using Microsoft.EntityFrameworkCore;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using TravelPlanner.Common.Enums;
using TravelPlanner.Common.DTOs.Destination;
using TravelPlanner.Common.DTOs.Notification;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.DTOs.Trip;
using TravelPlanner.Common.Interfaces;
using TripService.Data;
using TripService.Entities;
using TripService.Mappings;

namespace TripService
{
    internal sealed class TripService : StatelessService, ITripService
    {
        private readonly TripDbContextFactory _contextFactory;
        private readonly IConfiguration _configuration;

        public TripService(StatelessServiceContext context) : base(context)
        {
            _contextFactory = new TripDbContextFactory();
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        private async Task<bool> HasAccessAsync(Guid tripId, Guid userId, bool requiresEdit)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
            if (trip == null) return false;
            if (trip.UserId == userId) return true;

            var shareServiceUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
            var shareService = ServiceProxy.Create<IShareService>(new Uri(shareServiceUri), new ServicePartitionKey(0L));
            var access = await shareService.CheckAccessAsync(tripId, userId);

            if (!access.IsSuccess) return false;
            if (requiresEdit) return access.Data == "Edit";
            return access.Data == "Edit" || access.Data == "View";
        }

        public async Task<ResultDto<TripDto>> CreateTripAsync(CreateTripDto trip, Guid userId)
        {
            if (trip.EstimatedBudget < 0)
            {
                return ResultDto<TripDto>.Failure("Budget cannot be a negative value.");
            }
            if (trip.StartDate.Date > trip.EndDate.Date)
            {
                return ResultDto<TripDto>.Failure("End date cannot be scheduled before the start date.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var newTrip = new Trip
            {
                Id = Guid.NewGuid(),
                Title = trip.Title,
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                EstimatedBudget = trip.EstimatedBudget,
                GeneralNotes = trip.GeneralNotes,
                UserId = userId
            };
            dbContext.Trips.Add(newTrip);
            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.TripAdded,
                    Message = $"New trip '{newTrip.Title}' has been created.",
                    TripId = newTrip.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<TripDto>.Success(newTrip.MapToDto(), "Trip created successfully.");
        }

        public async Task<ResultDto<List<TripDto>>> GetUserTripsAsync(Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var ownedTrips = await dbContext.Trips.Where(t => t.UserId == userId).ToListAsync();
            var ownedDtos = ownedTrips.Select(t => t.MapToDto()).ToList();

            try
            {
                var shareServiceUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
                var shareService = ServiceProxy.Create<IShareService>(new Uri(shareServiceUri), new ServicePartitionKey(0L));
                var allTrips = await dbContext.Trips.ToListAsync();

                foreach (var trip in allTrips)
                {
                    if (trip.UserId == userId) continue;
                    var access = await shareService.CheckAccessAsync(trip.Id, userId);
                    if (access.IsSuccess && access.Data != "None")
                    {
                        ownedDtos.Add(trip.MapToDto());
                    }
                }
            }
            catch (Exception)
            {
            }

            return ResultDto<List<TripDto>>.Success(ownedDtos, "User trips retrieved successfully.");
        }

        public async Task<ResultDto<TripDto>> GetTripByIdAsync(Guid tripId, Guid userId)
        {
            if (userId != Guid.Empty && !await HasAccessAsync(tripId, userId, false))
            {
                return ResultDto<TripDto>.Failure("Access denied to this trip.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var t = await dbContext.Trips.FirstOrDefaultAsync(trip => trip.Id == tripId);
            if (t == null) return ResultDto<TripDto>.Failure("Trip not found.");
            return ResultDto<TripDto>.Success(t.MapToDto(), "Trip retrieved successfully.");
        }

        public async Task<ResultDto<Guid>> GetTripOwnerAsync(Guid tripId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
            if (trip == null) return ResultDto<Guid>.Failure("Trip not found.");
            return ResultDto<Guid>.Success(trip.UserId, "Trip owner retrieved successfully.");
        }

        public async Task<ResultDto<TripDto>> UpdateTripAsync(Guid tripId, CreateTripDto trip, Guid userId)
        {
            if (trip.EstimatedBudget < 0)
            {
                return ResultDto<TripDto>.Failure("Budget cannot be a negative value.");
            }
            if (trip.StartDate.Date > trip.EndDate.Date)
            {
                return ResultDto<TripDto>.Failure("End date cannot be scheduled before the start date.");
            }
            if (!await HasAccessAsync(tripId, userId, true))
            {
                return ResultDto<TripDto>.Failure("You do not have permission to modify this trip.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
            if (existing == null) return ResultDto<TripDto>.Failure("Trip not found.");

            var destinations = await dbContext.Destinations.Where(d => d.TripId == tripId).ToListAsync();
            foreach (var d in destinations)
            {
                if (d.ArrivalDate.Date < trip.StartDate.Date || d.DepartureDate.Date > trip.EndDate.Date)
                {
                    return ResultDto<TripDto>.Failure($"Cannot change dates. Destination '{d.Name}' falls outside the new trip range.");
                }
            }

            try
            {
                var activityServiceUri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
                var activityService = ServiceProxy.Create<IActivityService>(new Uri(activityServiceUri));
                var actResult = await activityService.GetTripActivitiesAsync(tripId, userId);
                if (actResult.IsSuccess && actResult.Data != null)
                {
                    foreach (var a in actResult.Data)
                    {
                        if (a.ScheduledAt.Date < trip.StartDate.Date || a.ScheduledAt.Date > trip.EndDate.Date)
                        {
                            return ResultDto<TripDto>.Failure($"Cannot change dates. Activity '{a.Name}' is scheduled outside the new trip range.");
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            try
            {
                var expenseServiceUri = _configuration["ServiceFabricSettings:ExpenseServiceUri"];
                var expenseService = ServiceProxy.Create<IExpenseService>(new Uri(expenseServiceUri));
                var expResult = await expenseService.GetTripExpensesAsync(tripId, userId);
                if (expResult.IsSuccess && expResult.Data != null)
                {
                    foreach (var e in expResult.Data)
                    {
                        if (e.IncurredAt.Date < trip.StartDate.Date || e.IncurredAt.Date > trip.EndDate.Date)
                        {
                            return ResultDto<TripDto>.Failure($"Cannot change dates. Expense '{e.Title}' is recorded outside the new trip range.");
                        }
                    }
                }
            }
            catch (Exception)
            {
            }

            existing.Title = trip.Title;
            existing.Description = trip.Description;
            existing.StartDate = trip.StartDate;
            existing.EndDate = trip.EndDate;
            existing.EstimatedBudget = trip.EstimatedBudget;
            existing.GeneralNotes = trip.GeneralNotes;

            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.TripChanged,
                    Message = $"Trip '{existing.Title}' core details have been updated.",
                    TripId = existing.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<TripDto>.Success(existing.MapToDto(), "Trip updated successfully.");
        }

        public async Task<ResultDto<bool>> DeleteTripAsync(Guid tripId, Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
            if (trip == null) return ResultDto<bool>.Failure("Trip not found.");
            if (trip.UserId != userId) return ResultDto<bool>.Failure("Only the owner can delete this trip.");

            try
            {
                var activityServiceUri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
                var activityService = ServiceProxy.Create<IActivityService>(new Uri(activityServiceUri));
                await activityService.RemoveAllActivitiesForTripAsync(tripId);

                var expenseServiceUri = _configuration["ServiceFabricSettings:ExpenseServiceUri"];
                var expenseService = ServiceProxy.Create<IExpenseService>(new Uri(expenseServiceUri));
                await expenseService.RemoveAllExpensesForTripAsync(tripId);

                var shareServiceUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
                var shareService = ServiceProxy.Create<IShareService>(new Uri(shareServiceUri), new ServicePartitionKey(0L));
                await shareService.ClearAllSharesForTripAsync(tripId);

                var checklistServiceUri = _configuration["ServiceFabricSettings:ChecklistServiceUri"];
                var checklistService = ServiceProxy.Create<IChecklistService>(new Uri(checklistServiceUri), new ServicePartitionKey(0L));
                await checklistService.ClearAllChecklistsForTripAsync(tripId);
            }
            catch (Exception)
            {
            }

            dbContext.Trips.Remove(trip);
            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.TripRemoved,
                    Message = $"Trip '{trip.Title}' has been deleted.",
                    TripId = trip.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<bool>.Success(true, "Trip deleted successfully.");
        }

        public async Task<ResultDto<DestinationDto>> AddDestinationAsync(CreateDestinationDto d, Guid userId)
        {
            if (!await HasAccessAsync(d.TripId, userId, true))
            {
                return ResultDto<DestinationDto>.Failure("No permission to modify destinations on this trip.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var dest = new Destination
            {
                Id = Guid.NewGuid(),
                Name = d.Name,
                Location = d.Location,
                ArrivalDate = d.ArrivalDate,
                DepartureDate = d.DepartureDate,
                Notes = d.Notes,
                TripId = d.TripId
            };
            dbContext.Destinations.Add(dest);
            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.DestinationAdded,
                    Message = $"Destination '{dest.Name}' has been added to the trip plan.",
                    TripId = dest.TripId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<DestinationDto>.Success(dest.MapToDto(), "Destination added successfully.");
        }

        public async Task<ResultDto<List<DestinationDto>>> GetTripDestinationsAsync(Guid tripId, Guid userId)
        {
            if (!await HasAccessAsync(tripId, userId, false))
            {
                return ResultDto<List<DestinationDto>>.Failure("Access denied.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var destinations = await dbContext.Destinations.Where(d => d.TripId == tripId).ToListAsync();
            var dtos = destinations.Select(d => d.MapToDto()).ToList();
            return ResultDto<List<DestinationDto>>.Success(dtos, "Destinations retrieved successfully.");
        }

        public async Task<ResultDto<DestinationDto>> UpdateDestinationAsync(Guid id, CreateDestinationDto d, Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Destinations.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return ResultDto<DestinationDto>.Failure("Destination not found.");

            if (!await HasAccessAsync(existing.TripId, userId, true))
            {
                return ResultDto<DestinationDto>.Failure("No permission to modify destinations on this trip.");
            }

            existing.Name = d.Name;
            existing.Location = d.Location;
            existing.ArrivalDate = d.ArrivalDate;
            existing.DepartureDate = d.DepartureDate;
            existing.Notes = d.Notes;

            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.DestinationChanged,
                    Message = $"Destination '{existing.Name}' details have been updated.",
                    TripId = existing.TripId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<DestinationDto>.Success(existing.MapToDto(), "Destination updated successfully.");
        }

        public async Task<ResultDto<bool>> DeleteDestinationAsync(Guid id, Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Destinations.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return ResultDto<bool>.Failure("Destination not found.");

            if (!await HasAccessAsync(existing.TripId, userId, true))
            {
                return ResultDto<bool>.Failure("No permission to modify destinations on this trip.");
            }

            dbContext.Destinations.Remove(existing);
            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.DestinationRemoved,
                    Message = $"Destination '{existing.Name}' has been removed from the plan.",
                    TripId = existing.TripId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<bool>.Success(true, "Destination deleted successfully.");
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
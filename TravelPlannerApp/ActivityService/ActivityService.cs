using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System.Fabric;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Activity;
using TravelPlanner.Common.DTOs.Notification;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.Enums;
using ActivityService.Data;
using ActivityService.Entities;
using ActivityService.Mappings;

namespace ActivityService
{
    internal sealed class ActivityService : StatelessService, IActivityService
    {
        private readonly ActivityDbContextFactory _contextFactory;
        private readonly IConfiguration _configuration;

        public ActivityService(StatelessServiceContext context) : base(context)
        {
            _contextFactory = new ActivityDbContextFactory();

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        private async Task<string> ValidateActivityAccessAndDatesAsync(Guid tripId, DateTime scheduledAt, Guid userId, bool requiresEdit)
        {
            var tripServiceUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripServiceUri));
            var tripResult = await tripService.GetTripByIdAsync(tripId, userId);

            if (!tripResult.IsSuccess || tripResult.Data == null)
            {
                return "Access denied or trip does not exist.";
            }

            if (requiresEdit)
            {
                if (tripResult.Data.UserId != userId)
                {
                    var shareServiceUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
                    var shareService = ServiceProxy.Create<IShareService>(new Uri(shareServiceUri), new ServicePartitionKey(0L));
                    var access = await shareService.CheckAccessAsync(tripId, userId);
                    if (!access.IsSuccess || access.Data != "Edit")
                    {
                        return "You do not have permission to modify data on this trip plan.";
                    }
                }
            }

            var trip = tripResult.Data;
            if (scheduledAt.Date < trip.StartDate.Date || scheduledAt.Date > trip.EndDate.Date)
            {
                return $"Activity date must be within the trip range ({trip.StartDate.ToShortDateString()} - {trip.EndDate.ToShortDateString()}).";
            }

            return null;
        }

        public async Task<ResultDto<ActivityDto>> AddActivityAsync(CreateActivityDto a, Guid userId)
        {
            var validationError = await ValidateActivityAccessAndDatesAsync(a.TripId, a.ScheduledAt, userId, true);
            if (validationError != null)
            {
                return ResultDto<ActivityDto>.Failure(validationError);
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                Name = a.Name,
                Location = a.Location,
                ScheduledAt = a.ScheduledAt,
                Price = a.Price,
                Description = a.Description,
                Status = a.Status,
                TripId = a.TripId
            };

            dbContext.Activities.Add(activity);
            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.ActivityAdded,
                    Message = $"Activity '{activity.Name}' has been added to the trip plan.",
                    TripId = activity.TripId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<ActivityDto>.Success(activity.MapToDto(), "Activity added successfully.");
        }

        public async Task<ResultDto<List<ActivityDto>>> GetTripActivitiesAsync(Guid tripId, Guid userId)
        {
            var validationError = await ValidateActivityAccessAndDatesAsync(tripId, DateTime.UtcNow, userId, false);
            if (validationError != null && validationError.Contains("Access denied"))
            {
                return ResultDto<List<ActivityDto>>.Failure("Access denied.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var activities = await dbContext.Activities.Where(a => a.TripId == tripId).ToListAsync();
            var dtos = activities.Select(a => a.MapToDto()).ToList();
            return ResultDto<List<ActivityDto>>.Success(dtos, "Activities retrieved successfully.");
        }

        public async Task<ResultDto<bool>> UpdateActivityAsync(Guid id, CreateActivityDto a, Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Activities.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return ResultDto<bool>.Failure("Activity not found.");

            var validationError = await ValidateActivityAccessAndDatesAsync(existing.TripId, a.ScheduledAt, userId, true);
            if (validationError != null)
            {
                return ResultDto<bool>.Failure(validationError);
            }

            existing.Name = a.Name;
            existing.Location = a.Location;
            existing.ScheduledAt = a.ScheduledAt;
            existing.Price = a.Price;
            existing.Description = a.Description;
            existing.Status = a.Status;

            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.ActivityChanged,
                    Message = $"Activity '{existing.Name}' details have been updated.",
                    TripId = existing.TripId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<bool>.Success(true, "Activity updated successfully.");
        }

        public async Task<ResultDto<bool>> RemoveActivityAsync(Guid id, Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Activities.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return ResultDto<bool>.Failure("Activity not found.");

            var validationError = await ValidateActivityAccessAndDatesAsync(existing.TripId, existing.ScheduledAt, userId, true);
            if (validationError != null && validationError.Contains("permission"))
            {
                return ResultDto<bool>.Failure(validationError);
            }

            dbContext.Activities.Remove(existing);
            await dbContext.SaveChangesAsync();

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.ActivityRemoved,
                    Message = $"Activity '{existing.Name}' has been removed from the plan.",
                    TripId = existing.TripId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
            }

            return ResultDto<bool>.Success(true, "Activity removed successfully.");
        }

        public async Task<ResultDto<bool>> RemoveAllActivitiesForTripAsync(Guid tripId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var activities = await dbContext.Activities.Where(a => a.TripId == tripId).ToListAsync();

            if (activities.Any())
            {
                dbContext.Activities.RemoveRange(activities);
                await dbContext.SaveChangesAsync();
            }

            return ResultDto<bool>.Success(true, "All trip activities removed successfully.");
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
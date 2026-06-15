using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.IO;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Activity;
using TravelPlanner.Common.DTOs.Expense;
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

            if (activity.Price > 0)
            {
                var expenseServiceUri = _configuration["ServiceFabricSettings:ExpenseServiceUri"];
                var expenseService = ServiceProxy.Create<IExpenseService>(new Uri(expenseServiceUri));

                var expenseResult = await expenseService.AddExpenseAsync(new CreateExpenseDto
                {
                    Title = $"Activity: {activity.Name}",
                    Category = ExpenseCategory.Activity,
                    Amount = activity.Price,
                    IncurredAt = activity.ScheduledAt,
                    Description = activity.Description,
                    TripId = activity.TripId
                }, userId);

                if (!expenseResult.IsSuccess)
                {
                    return ResultDto<ActivityDto>.Failure($"Activity saved, but failed to sync expense: {expenseResult.Message}");
                }
            }

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

            string oldName = existing.Name;
            double oldPrice = existing.Price;

            existing.Name = a.Name;
            existing.Location = a.Location;
            existing.ScheduledAt = a.ScheduledAt;
            existing.Price = a.Price;
            existing.Description = a.Description;
            existing.Status = a.Status;

            await dbContext.SaveChangesAsync();

            try
            {
                var expenseServiceUri = _configuration["ServiceFabricSettings:ExpenseServiceUri"];
                var expenseService = ServiceProxy.Create<IExpenseService>(new Uri(expenseServiceUri));

                if (oldPrice == 0 && a.Price > 0)
                {
                    await expenseService.AddExpenseAsync(new CreateExpenseDto
                    {
                        Title = $"Activity: {a.Name}",
                        Category = ExpenseCategory.Activity,
                        Amount = a.Price,
                        IncurredAt = a.ScheduledAt,
                        Description = a.Description,
                        TripId = existing.TripId
                    }, userId);
                }
                else if (oldPrice > 0 && a.Price == 0)
                {
                    await expenseService.SyncDeleteExpenseFromActivityAsync(existing.TripId, $"Activity: {oldName}", oldPrice);
                }
                else if (oldPrice > 0 && a.Price > 0)
                {
                    await expenseService.SyncUpdateExpenseFromActivityAsync(
                        existing.TripId,
                        $"Activity: {oldName}",
                        oldPrice,
                        $"Activity: {a.Name}",
                        a.Price
                    );
                }
            }
            catch (Exception)
            {
            }

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

            if (existing.Price > 0)
            {
                try
                {
                    var expenseServiceUri = _configuration["ServiceFabricSettings:ExpenseServiceUri"];
                    var expenseService = ServiceProxy.Create<IExpenseService>(new Uri(expenseServiceUri));
                    await expenseService.SyncDeleteExpenseFromActivityAsync(existing.TripId, $"Activity: {existing.Name}", existing.Price);
                }
                catch (Exception)
                {
                }
            }

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

        public async Task<ResultDto<bool>> SyncDeleteActivityFromExpenseAsync(Guid tripId, string name, double price)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var activity = await dbContext.Activities
                .FirstOrDefaultAsync(a => a.TripId == tripId && a.Name == name && Math.Abs(a.Price - price) < 0.01);

            if (activity != null)
            {
                dbContext.Activities.Remove(activity);
                await dbContext.SaveChangesAsync();
            }

            return ResultDto<bool>.Success(true);
        }

        public async Task<ResultDto<bool>> SyncUpdateActivityFromExpenseAsync(Guid tripId, string oldName, double oldPrice, string newName, double newPrice)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var activity = await dbContext.Activities
                .FirstOrDefaultAsync(a => a.TripId == tripId && a.Name == oldName && Math.Abs(a.Price - oldPrice) < 0.01);

            if (activity != null)
            {
                activity.Name = newName;
                activity.Price = newPrice;
                await dbContext.SaveChangesAsync();
            }

            return ResultDto<bool>.Success(true);
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
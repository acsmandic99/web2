using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Activity;
using TravelPlanner.Common.DTOs.Shared;
using ActivityService.Data;
using ActivityService.Entities;
using ActivityService.Mappings;

namespace ActivityService
{
    internal sealed class ActivityService : StatelessService, IActivityService
    {
        private readonly ActivityDbContextFactory _contextFactory;

        public ActivityService(StatelessServiceContext context) : base(context)
        {
            _contextFactory = new ActivityDbContextFactory();
        }

        private async Task<string> ValidateActivityDateAsync(Guid tripId, DateTime scheduledAt)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var tripResult = await tripService.GetTripByIdAsync(tripId);

            if (!tripResult.IsSuccess || tripResult.Data == null)
            {
                return "The connected trip does not exist.";
            }

            var trip = tripResult.Data;
            if (scheduledAt.Date < trip.StartDate.Date || scheduledAt.Date > trip.EndDate.Date)
            {
                return $"Activity date must be within the trip range ({trip.StartDate.ToShortDateString()} - {trip.EndDate.ToShortDateString()}).";
            }

            return null;
        }

        public async Task<ResultDto<ActivityDto>> AddActivityAsync(CreateActivityDto a)
        {
            var validationError = await ValidateActivityDateAsync(a.TripId, a.ScheduledAt);
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
            return ResultDto<ActivityDto>.Success(activity.MapToDto(), "Activity added successfully.");
        }

        public async Task<ResultDto<List<ActivityDto>>> GetTripActivitiesAsync(Guid tripId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var activities = await dbContext.Activities.Where(a => a.TripId == tripId).ToListAsync();
            var dtos = activities.Select(a => a.MapToDto()).ToList();
            return ResultDto<List<ActivityDto>>.Success(dtos, "Activities retrieved successfully.");
        }

        public async Task<ResultDto<bool>> UpdateActivityAsync(Guid id, CreateActivityDto a)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Activities.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return ResultDto<bool>.Failure("Activity not found.");

            var validationError = await ValidateActivityDateAsync(existing.TripId, a.ScheduledAt);
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
            return ResultDto<bool>.Success(true, "Activity updated successfully.");
        }

        public async Task<ResultDto<bool>> RemoveActivityAsync(Guid id)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Activities.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return ResultDto<bool>.Failure("Activity not found.");

            dbContext.Remove(existing);
            await dbContext.SaveChangesAsync();
            return ResultDto<bool>.Success(true, "Activity removed successfully.");
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
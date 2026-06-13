using TravelPlanner.Common.DTOs.Activity;
using ActivityService.Entities;

namespace ActivityService.Mappings
{
    public static class ActivityMappingExtensions
    {
        public static ActivityDto MapToDto(this Activity a)
        {
            return new ActivityDto
            {
                Id = a.Id,
                Name = a.Name,
                Location = a.Location,
                ScheduledAt = a.ScheduledAt,
                Price = a.Price,
                Description = a.Description,
                Status = a.Status,
                TripId = a.TripId
            };
        }
    }
}
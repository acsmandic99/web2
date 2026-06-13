using System;
using TravelPlanner.Common.Enums;

namespace TravelPlanner.Common.DTOs.Activity
{
    public class ActivityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime ScheduledAt { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public ActivityStatus Status { get; set; }
        public Guid TripId { get; set; }
    }
}
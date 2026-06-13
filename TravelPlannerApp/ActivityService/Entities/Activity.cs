using System;
using System.Diagnostics;
using TravelPlanner.Common.Enums;

namespace ActivityService.Entities
{
    public class Activity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public double Price { get; set; }
        public Guid TripId { get; set; }
        public string Description { get; set; } = string.Empty;
        public ActivityStatus Status { get; set; }      
    }
}
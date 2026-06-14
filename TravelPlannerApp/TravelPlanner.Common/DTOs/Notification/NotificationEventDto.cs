using System;
using TravelPlanner.Common.Enums;

namespace TravelPlanner.Common.DTOs.Notification
{
    public class NotificationEventDto
    {
        public NotificationEventType EventType { get; set; }
        public string Message { get; set; }
        public Guid TripId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
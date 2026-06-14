using System;

namespace TravelPlanner.Common.DTOs.Notification
{
    public class NotificationEventDto
    {
        public string EventType { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
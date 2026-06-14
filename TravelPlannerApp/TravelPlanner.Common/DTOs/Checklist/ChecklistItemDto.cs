using System;

namespace TravelPlanner.Common.DTOs.Checklist
{
    public class ChecklistItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
        public Guid TripId { get; set; }
    }
}
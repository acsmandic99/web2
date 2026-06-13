using System;

namespace TripService.Entities
{
    public class Trip
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double EstimatedBudget { get; set; }
        public Guid UserId { get; set; }
        public string GeneralNotes { get; set; }
    }
}
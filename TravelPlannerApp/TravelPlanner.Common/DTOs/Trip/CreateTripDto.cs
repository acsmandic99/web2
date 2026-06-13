using System;

namespace TravelPlanner.Common.DTOs.Trip
{
    public class CreateTripDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double EstimatedBudget { get; set; }
        public string GeneralNotes { get; set; }
    }
}
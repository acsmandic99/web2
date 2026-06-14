using System;
using TravelPlanner.Common.Enums;

namespace TravelPlanner.Common.DTOs.Expense
{
    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public ExpenseCategory Category { get; set; }
        public double Amount { get; set; }
        public DateTime IncurredAt { get; set; }
        public string Description { get; set; }
        public Guid TripId { get; set; }
    }
}
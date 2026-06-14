using ExpenseService.Entities;
using TravelPlanner.Common.DTOs.Expense;

namespace ExpenseService.Mappings
{
    public static class ExpenseMappingExtensions
    {
        public static ExpenseDto MapToDto(this Expense e)
        {
            return new ExpenseDto
            {
                Id = e.Id,
                Title = e.Title,
                Category = e.Category,
                Amount = e.Amount,
                IncurredAt = e.IncurredAt,
                Description = e.Description,
                TripId = e.TripId
            };
        }
    }
}
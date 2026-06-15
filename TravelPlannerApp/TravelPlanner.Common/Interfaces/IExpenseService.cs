using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Expense;
using TravelPlanner.Common.DTOs.Shared;

namespace TravelPlanner.Common.Interfaces
{
    public interface IExpenseService : IService
    {
        Task<ResultDto<ExpenseDto>> AddExpenseAsync(CreateExpenseDto expense, Guid userId);
        Task<ResultDto<List<ExpenseDto>>> GetTripExpensesAsync(Guid tripId, Guid userId);
        Task<ResultDto<BudgetSummaryDto>> GetBudgetSummaryAsync(Guid tripId, Guid userId);
        Task<ResultDto<ExpenseDto>> GetExpenseByIdAsync(Guid expenseId, Guid userId);
        Task<ResultDto<ExpenseDto>> UpdateExpenseAsync(Guid expenseId, CreateExpenseDto expense, Guid userId);
        Task<ResultDto<bool>> DeleteExpenseAsync(Guid expenseId, Guid userId);
        Task<ResultDto<bool>> SyncDeleteExpenseFromActivityAsync(Guid tripId, string title, double amount);
        Task<ResultDto<bool>> RemoveAllExpensesForTripAsync(Guid tripId);
        Task<ResultDto<bool>> SyncUpdateExpenseFromActivityAsync(Guid tripId, string oldTitle, double oldAmount, string newTitle, double newAmount);
    }
}
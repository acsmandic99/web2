using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Expense;
using TravelPlanner.Common.DTOs.Shared;
using ExpenseService.Data;
using ExpenseService.Entities;
using ExpenseService.Mappings;

namespace ExpenseService
{
    internal sealed class ExpenseService : StatelessService, IExpenseService
    {
        private readonly ExpenseDbContextFactory _contextFactory;

        public ExpenseService(StatelessServiceContext context) : base(context)
        {
            _contextFactory = new ExpenseDbContextFactory();
        }

        private async Task<bool> CheckAccessAsync(Guid tripId, Guid userId, bool requiresEdit)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var tripResult = await tripService.GetTripByIdAsync(tripId, userId);
            if (!tripResult.IsSuccess || tripResult.Data == null) return false;
            if (tripResult.Data.UserId == userId) return true;

            var shareService = ServiceProxy.Create<IShareService>(new Uri("fabric:/TravelPlannerApp/ShareService"));
            var access = await shareService.CheckAccessAsync(tripId, userId);
            if (!access.IsSuccess) return false;

            if (requiresEdit) return access.Data == "Edit";
            return access.Data == "Edit" || access.Data == "View";
        }

        public async Task<ResultDto<ExpenseDto>> AddExpenseAsync(CreateExpenseDto expense, Guid userId)
        {
            if (!await CheckAccessAsync(expense.TripId, userId, true))
            {
                return ResultDto<ExpenseDto>.Failure("No permission to modify financials on this trip.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var newExpense = new Expense
            {
                Id = Guid.NewGuid(),
                Title = expense.Title,
                Category = expense.Category,
                Amount = expense.Amount,
                IncurredAt = expense.IncurredAt,
                Description = expense.Description,
                TripId = expense.TripId
            };

            dbContext.Expenses.Add(newExpense);
            await dbContext.SaveChangesAsync();
            return ResultDto<ExpenseDto>.Success(newExpense.MapToDto(), "Expense recorded successfully.");
        }

        public async Task<ResultDto<List<ExpenseDto>>> GetTripExpensesAsync(Guid tripId, Guid userId)
        {
            if (!await CheckAccessAsync(tripId, userId, false))
            {
                return ResultDto<List<ExpenseDto>>.Failure("Access denied.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var expenses = await dbContext.Expenses.Where(e => e.TripId == tripId).ToListAsync();
            var dtos = expenses.Select(e => e.MapToDto()).ToList();
            return ResultDto<List<ExpenseDto>>.Success(dtos, "Expenses retrieved successfully.");
        }

        public async Task<ResultDto<BudgetSummaryDto>> GetBudgetSummaryAsync(Guid tripId, Guid userId)
        {
            if (!await CheckAccessAsync(tripId, userId, false))
            {
                return ResultDto<BudgetSummaryDto>.Failure("Access denied.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var tripResult = await tripService.GetTripByIdAsync(tripId, userId);

            var totalSpent = await dbContext.Expenses.Where(e => e.TripId == tripId).SumAsync(e => e.Amount);
            var estimatedBudget = tripResult.Data.EstimatedBudget;

            var summary = new BudgetSummaryDto
            {
                EstimatedBudget = estimatedBudget,
                TotalSpent = totalSpent,
                RemainingBudget = estimatedBudget - totalSpent
            };

            return ResultDto<BudgetSummaryDto>.Success(summary, "Budget summary calculated successfully.");
        }

        public async Task<ResultDto<ExpenseDto>> GetExpenseByIdAsync(Guid expenseId, Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var expense = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId);
            if (expense == null) return ResultDto<ExpenseDto>.Failure("Expense record not found.");

            if (!await CheckAccessAsync(expense.TripId, userId, false))
            {
                return ResultDto<ExpenseDto>.Failure("Access denied.");
            }

            return ResultDto<ExpenseDto>.Success(expense.MapToDto(), "Expense retrieved successfully.");
        }

        public async Task<ResultDto<ExpenseDto>> UpdateExpenseAsync(Guid expenseId, CreateExpenseDto expense, Guid userId)
        {
            if (!await CheckAccessAsync(expense.TripId, userId, true))
            {
                return ResultDto<ExpenseDto>.Failure("No permission to modify financials on this trip.");
            }

            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId);
            if (existing == null) return ResultDto<ExpenseDto>.Failure("Expense record not found.");

            existing.Title = expense.Title;
            existing.Category = expense.Category;
            existing.Amount = expense.Amount;
            existing.IncurredAt = expense.IncurredAt;
            existing.Description = expense.Description;

            await dbContext.SaveChangesAsync();
            return ResultDto<ExpenseDto>.Success(existing.MapToDto(), "Expense updated successfully.");
        }

        public async Task<ResultDto<bool>> DeleteExpenseAsync(Guid expenseId, Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId);
            if (existing == null) return ResultDto<bool>.Failure("Expense record not found.");

            if (!await CheckAccessAsync(existing.TripId, userId, true))
            {
                return ResultDto<bool>.Failure("No permission to modify financials on this trip.");
            }

            dbContext.Expenses.Remove(existing);
            await dbContext.SaveChangesAsync();
            return ResultDto<bool>.Success(true, "Expense deleted successfully.");
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
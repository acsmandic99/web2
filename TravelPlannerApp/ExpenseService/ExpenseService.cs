using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Expense;
using TravelPlanner.Common.DTOs.Notification;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.Enums;
using ExpenseService.Data;
using ExpenseService.Entities;
using ExpenseService.Mappings;

namespace ExpenseService
{
    internal sealed class ExpenseService : StatelessService, IExpenseService
    {
        private readonly ExpenseDbContextFactory _contextFactory;
        private readonly IConfiguration _configuration;

        public ExpenseService(StatelessServiceContext context) : base(context)
        {
            _contextFactory = new ExpenseDbContextFactory();
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        private async Task<bool> CheckAccessAsync(Guid tripId, Guid userId, bool requiresEdit)
        {
            var tripServiceUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripServiceUri));
            var tripResult = await tripService.GetTripByIdAsync(tripId, userId);
            if (!tripResult.IsSuccess || tripResult.Data == null) return false;
            if (tripResult.Data.UserId == userId) return true;

            var shareServiceUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
            var shareService = ServiceProxy.Create<IShareService>(new Uri(shareServiceUri), new ServicePartitionKey(0L));
            var access = await shareService.CheckAccessAsync(tripId, userId);
            if (!access.IsSuccess) return false;

            if (requiresEdit) return access.Data == "Editor";
            return access.Data == "Editor" || access.Data == "Viewer";
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

            try
            {
                var tripServiceUri = _configuration["ServiceFabricSettings:TripServiceUri"];
                var tripService = ServiceProxy.Create<ITripService>(new Uri(tripServiceUri));
                var tripResult = await tripService.GetTripByIdAsync(expense.TripId, userId);

                var totalSpent = await dbContext.Expenses.Where(e => e.TripId == expense.TripId).SumAsync(e => e.Amount);

                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));

                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.ExpenseAdded,
                    Message = $"New expense recorded: {newExpense.Title} ({newExpense.Amount} EUR) for Trip {newExpense.TripId}",
                    TripId = newExpense.TripId,
                    CreatedAt = DateTime.UtcNow
                });

                if (tripResult.IsSuccess && tripResult.Data != null && totalSpent > tripResult.Data.EstimatedBudget)
                {
                    await notificationService.PublishEventAsync(new NotificationEventDto
                    {
                        EventType = NotificationEventType.ExpenseAdded,
                        Message = $"CRITICAL: Budget exceeded for trip '{tripResult.Data.Title}'! Total spent: {totalSpent} EUR (Budget: {tripResult.Data.EstimatedBudget} EUR).",
                        TripId = newExpense.TripId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception)
            {
            }

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
            var tripServiceUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripServiceUri));
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
            using var dbContext = _contextFactory.CreateDbContext(null);
            var existing = await dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId);
            if (existing == null) return ResultDto<ExpenseDto>.Failure("Expense record not found.");

            if (!await CheckAccessAsync(existing.TripId, userId, true))
            {
                return ResultDto<ExpenseDto>.Failure("No permission to modify financials on this trip.");
            }

            string oldTitle = existing.Title;
            double oldAmount = existing.Amount;
            var oldCategory = existing.Category;

            existing.Title = expense.Title;
            existing.Category = expense.Category;
            existing.Amount = expense.Amount;
            existing.IncurredAt = expense.IncurredAt;
            existing.Description = expense.Description;

            await dbContext.SaveChangesAsync();

            if (oldCategory == ExpenseCategory.Activity)
            {
                try
                {
                    var activityServiceUri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
                    var activityService = ServiceProxy.Create<IActivityService>(new Uri(activityServiceUri));

                    string oldActivityName = oldTitle.StartsWith("Activity: ") ? oldTitle.Substring(10) : oldTitle;
                    string newActivityName = expense.Title.StartsWith("Activity: ") ? expense.Title.Substring(10) : expense.Title;

                    await activityService.SyncUpdateActivityFromExpenseAsync(
                        existing.TripId,
                        oldActivityName,
                        (double)oldAmount,
                        newActivityName,
                        (double)expense.Amount
                    );
                }
                catch (Exception)
                {
                }
            }

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

            if (existing.Category == ExpenseCategory.Activity)
            {
                try
                {
                    var activityServiceUri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
                    var activityService = ServiceProxy.Create<IActivityService>(new Uri(activityServiceUri));
                    var activityName = existing.Title.StartsWith("Activity: ") ? existing.Title.Substring(10) : existing.Title;
                    await activityService.SyncDeleteActivityFromExpenseAsync(existing.TripId, activityName, (double)existing.Amount);
                }
                catch (Exception)
                {
                }
            }

            return ResultDto<bool>.Success(true, "Expense deleted successfully.");
        }

        public async Task<ResultDto<bool>> SyncDeleteExpenseFromActivityAsync(Guid tripId, string title, double amount)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var expense = await dbContext.Expenses
                .FirstOrDefaultAsync(e => e.TripId == tripId && e.Title == title && Math.Abs((double)e.Amount - amount) < 0.01);

            if (expense != null)
            {
                dbContext.Expenses.Remove(expense);
                await dbContext.SaveChangesAsync();
            }

            return ResultDto<bool>.Success(true);
        }

        public async Task<ResultDto<bool>> SyncUpdateExpenseFromActivityAsync(Guid tripId, string oldTitle, double oldAmount, string newTitle, double newAmount)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var expense = await dbContext.Expenses
                .FirstOrDefaultAsync(e => e.TripId == tripId && e.Title == oldTitle && Math.Abs((double)e.Amount - oldAmount) < 0.01);

            if (expense != null)
            {
                expense.Title = newTitle;
                expense.Amount = (double)newAmount;
                await dbContext.SaveChangesAsync();
            }

            return ResultDto<bool>.Success(true);
        }

        public async Task<ResultDto<bool>> RemoveAllExpensesForTripAsync(Guid tripId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var expenses = await dbContext.Expenses.Where(e => e.TripId == tripId).ToListAsync();

            if (expenses.Any())
            {
                dbContext.Expenses.RemoveRange(expenses);
                await dbContext.SaveChangesAsync();
            }

            return ResultDto<bool>.Success(true, "All trip expenses removed successfully.");
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
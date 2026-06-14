using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Expense;
using TravelPlanner.Common.DTOs.Shared;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/expenses")]
    public class ExpensesController : ControllerBase
    {
        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetTripExpenses(Guid tripId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var expenseService = ServiceProxy.Create<IExpenseService>(new Uri("fabric:/TravelPlannerApp/ExpenseService"));
            var result = await expenseService.GetTripExpensesAsync(tripId, Guid.Parse(userIdClaim));
            return Ok(result);
        }

        [HttpGet("trip/{tripId}/summary")]
        public async Task<IActionResult> GetBudgetSummary(Guid tripId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var expenseService = ServiceProxy.Create<IExpenseService>(new Uri("fabric:/TravelPlannerApp/ExpenseService"));
            var result = await expenseService.GetBudgetSummaryAsync(tripId, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateExpenseDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var expenseService = ServiceProxy.Create<IExpenseService>(new Uri("fabric:/TravelPlannerApp/ExpenseService"));
            var result = await expenseService.AddExpenseAsync(request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var expenseService = ServiceProxy.Create<IExpenseService>(new Uri("fabric:/TravelPlannerApp/ExpenseService"));
            var result = await expenseService.GetExpenseByIdAsync(id, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateExpenseDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var expenseService = ServiceProxy.Create<IExpenseService>(new Uri("fabric:/TravelPlannerApp/ExpenseService"));
            var result = await expenseService.UpdateExpenseAsync(id, request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var expenseService = ServiceProxy.Create<IExpenseService>(new Uri("fabric:/TravelPlannerApp/ExpenseService"));
            var result = await expenseService.DeleteExpenseAsync(id, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
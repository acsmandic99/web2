using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Checklist;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/checklists")]
    public class ChecklistsController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] CreateChecklistItemDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var checklistService = ServiceProxy.Create<IChecklistService>(new Uri("fabric:/TravelPlannerApp/ChecklistService"), new ServicePartitionKey(0L));
            var result = await checklistService.AddItemAsync(request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetItems(Guid tripId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var checklistService = ServiceProxy.Create<IChecklistService>(new Uri("fabric:/TravelPlannerApp/ChecklistService"), new ServicePartitionKey(0L));
            var result = await checklistService.GetItemsAsync(tripId, Guid.Parse(userIdClaim));
            return Ok(result);
        }

        [HttpPut("trip/{tripId}/item/{itemId}/toggle")]
        public async Task<IActionResult> ToggleItem(Guid tripId, Guid itemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var checklistService = ServiceProxy.Create<IChecklistService>(new Uri("fabric:/TravelPlannerApp/ChecklistService"), new ServicePartitionKey(0L));
            var result = await checklistService.ToggleItemAsync(tripId, itemId, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("trip/{tripId}/item/{itemId}")]
        public async Task<IActionResult> DeleteItem(Guid tripId, Guid itemId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var checklistService = ServiceProxy.Create<IChecklistService>(new Uri("fabric:/TravelPlannerApp/ChecklistService"), new ServicePartitionKey(0L));
            var result = await checklistService.DeleteItemAsync(tripId, itemId, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
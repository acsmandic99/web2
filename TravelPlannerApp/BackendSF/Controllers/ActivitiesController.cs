using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Activity;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController : ControllerBase
    {
        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetTripActivities(Guid tripId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.GetTripActivitiesAsync(tripId, Guid.Parse(userIdClaim));
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateActivityDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.AddActivityAsync(request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateActivityDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.UpdateActivityAsync(id, request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.RemoveActivityAsync(id, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
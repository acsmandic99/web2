using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Activity;

namespace BackendSF.Controllers
{
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController : ControllerBase
    {
        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetTripActivities(Guid tripId)
        {
            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.GetTripActivitiesAsync(tripId);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateActivityDto request)
        {
            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.AddActivityAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateActivityDto request)
        {
            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.UpdateActivityAsync(id, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var activityService = ServiceProxy.Create<IActivityService>(new Uri("fabric:/TravelPlannerApp/ActivityService"));
            var result = await activityService.RemoveActivityAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
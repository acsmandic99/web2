using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Activity;
using BackendSF.Extensions;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ActivitiesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetTripActivities(Guid tripId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
            var activityService = ServiceProxy.Create<IActivityService>(new Uri(uri));
            var result = await activityService.GetTripActivitiesAsync(tripId, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateActivityDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
            var activityService = ServiceProxy.Create<IActivityService>(new Uri(uri));
            var result = await activityService.AddActivityAsync(request, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateActivityDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
            var activityService = ServiceProxy.Create<IActivityService>(new Uri(uri));
            var result = await activityService.UpdateActivityAsync(id, request, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:ActivityServiceUri"];
            var activityService = ServiceProxy.Create<IActivityService>(new Uri(uri));
            var result = await activityService.RemoveActivityAsync(id, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }
    }
}
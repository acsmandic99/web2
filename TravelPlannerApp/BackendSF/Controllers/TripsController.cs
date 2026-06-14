using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Trip;
using TravelPlanner.Common.Enums;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTrips(Guid userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || Guid.Parse(userIdClaim) != userId) return Unauthorized();

            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            return Ok(await tripService.GetUserTripsAsync(userId));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.GetTripByIdAsync(id, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.CreateTripAsync(request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateTripDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.UpdateTripAsync(id, request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.DeleteTripAsync(id, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id}/share")]
        public async Task<IActionResult> Share(Guid id, [FromQuery] ShareAccessLevel accessLevel)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var shareService = ServiceProxy.Create<IShareService>(new Uri("fabric:/TravelPlannerApp/ShareService"), new ServicePartitionKey(0L));
            var result = await shareService.GenerateShareTokenAsync(id, accessLevel, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("share/claim/{token}")]
        public async Task<IActionResult> ClaimShare(string token)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var shareService = ServiceProxy.Create<IShareService>(new Uri("fabric:/TravelPlannerApp/ShareService"), new ServicePartitionKey(0L));
            var result = await shareService.ClaimShareTokenAsync(token, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
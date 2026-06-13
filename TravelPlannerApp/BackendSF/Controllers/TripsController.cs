using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Trip;

namespace BackendSF.Controllers
{
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTrips(Guid userId)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.GetUserTripsAsync(userId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.GetTripByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.CreateTripAsync(request, Guid.Parse(userIdClaim));
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateTripDto request)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.UpdateTripAsync(id, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.DeleteTripAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
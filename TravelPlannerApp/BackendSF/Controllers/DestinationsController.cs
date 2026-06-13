using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Destination;

namespace BackendSF.Controllers
{
    [ApiController]
    [Route("api/destinations")]
    public class DestinationsController : ControllerBase
    {
        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetTripDestinations(Guid tripId)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.GetTripDestinationsAsync(tripId);
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateDestinationDto request)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.AddDestinationAsync(request);
            return Ok(result);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateDestinationDto request)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.UpdateDestinationAsync(id, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var result = await tripService.DeleteDestinationAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
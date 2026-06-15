using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Destination;
using BackendSF.Extensions;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/destinations")]
    public class DestinationsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public DestinationsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetTripDestinations(Guid tripId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.GetTripDestinationsAsync(tripId, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateDestinationDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.AddDestinationAsync(request, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateDestinationDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.UpdateDestinationAsync(id, request, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.DeleteDestinationAsync(id, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }
    }
}
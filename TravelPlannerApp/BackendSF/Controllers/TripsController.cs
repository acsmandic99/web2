using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Trip;

namespace BackendSF.Controllers
{
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripDto request)
        {
            try
            {
                var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
                var response = await tripService.CreateTripAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTrips(Guid userId)
        {
            try
            {
                var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
                var response = await tripService.GetUserTripsAsync(userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
                var response = await tripService.GetTripByIdAsync(id);
                if (response == null)
                {
                    return NotFound();
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
                var success = await tripService.DeleteTripAsync(id);
                if (!success)
                {
                    return NotFound();
                }
                return Ok(new { Message = "Trip deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
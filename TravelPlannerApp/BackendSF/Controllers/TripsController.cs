using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Trip;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.Enums;
using BackendSF.Extensions;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/trips")]
    public class TripsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TripsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserTrips(Guid userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || Guid.Parse(userIdClaim) != userId) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.GetUserTripsAsync(userId);
            return result.ToActionResult();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.GetTripByIdAsync(id, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTripDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.CreateTripAsync(request, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] CreateTripDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.UpdateTripAsync(id, request, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(uri));
            var result = await tripService.DeleteTripAsync(id, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPost("{id}/share")]
        public async Task<IActionResult> Share(Guid id, [FromQuery] ShareAccessLevel accessLevel)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:ShareServiceUri"];
            var shareService = ServiceProxy.Create<IShareService>(new Uri(uri), new ServicePartitionKey(0L));
            var result = await shareService.GenerateShareTokenAsync(id, accessLevel, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpPost("share/claim/{token}")]
        public async Task<IActionResult> ClaimShare(string token)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:ShareServiceUri"];
            var shareService = ServiceProxy.Create<IShareService>(new Uri(uri), new ServicePartitionKey(0L));
            var result = await shareService.ClaimShareTokenAsync(token, Guid.Parse(userIdClaim));
            return result.ToActionResult();
        }

        [HttpGet("{id}/collaborators")]
        public async Task<IActionResult> GetCollaborators(Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripUri));
            var ownerResult = await tripService.GetTripOwnerAsync(id);
            if (!ownerResult.IsSuccess || ownerResult.Data != Guid.Parse(userIdClaim)) return Unauthorized();

            var shareUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
            var shareService = ServiceProxy.Create<IShareService>(new Uri(shareUri), new ServicePartitionKey(0L));

            var sharedUsersResult = await shareService.GetSharedUsersAsync(id);
            if (!sharedUsersResult.IsSuccess) return sharedUsersResult.ToActionResult();

            var collaborators = new List<object>();
            var userUri = _configuration["ServiceFabricSettings:UserServiceUri"];
            var userService = ServiceProxy.Create<IUserService>(new Uri(userUri));

            foreach (var uId in sharedUsersResult.Data)
            {
                var userResult = await userService.GetUserByIdAsync(uId);
                var accessResult = await shareService.CheckAccessAsync(id, uId);

                if (userResult.IsSuccess && userResult.Data != null)
                {
                    collaborators.Add(new
                    {
                        UserId = uId,
                        Username = userResult.Data.Username,
                        Email = userResult.Data.Email,
                        AccessLevel = accessResult.Data
                    });
                }
            }

            var finalResult = ResultDto<List<object>>.Success(collaborators, "Collaborators retrieved successfully.");
            return finalResult.ToActionResult();
        }

        [HttpPut("{id}/collaborators/{userId}")]
        public async Task<IActionResult> UpdateCollaboratorPermission(Guid id, Guid userId, [FromQuery] string accessLevel)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripUri));
            var ownerResult = await tripService.GetTripOwnerAsync(id);
            if (!ownerResult.IsSuccess || ownerResult.Data != Guid.Parse(userIdClaim)) return Unauthorized();

            var shareUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
            var shareService = ServiceProxy.Create<IShareService>(new Uri(shareUri), new ServicePartitionKey(0L));
            var result = await shareService.UpdateUserPermissionAsync(id, userId, accessLevel);
            return result.ToActionResult();
        }

        [HttpDelete("{id}/collaborators/{userId}")]
        public async Task<IActionResult> RevokeCollaboratorPermission(Guid id, Guid userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var tripUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripUri));
            var ownerResult = await tripService.GetTripOwnerAsync(id);
            if (!ownerResult.IsSuccess || ownerResult.Data != Guid.Parse(userIdClaim)) return Unauthorized();

            var shareUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
            var shareService = ServiceProxy.Create<IShareService>(new Uri(shareUri), new ServicePartitionKey(0L));
            var result = await shareService.RevokeUserPermissionAsync(id, userId);
            return result.ToActionResult();
        }
    }
}
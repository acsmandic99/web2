using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;

namespace BackendSF.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var notificationService = ServiceProxy.Create<INotificationService>(new Uri("fabric:/TravelPlannerApp/NotificationService"), new ServicePartitionKey(0L));
            var result = await notificationService.GetUserNotificationsAsync(Guid.Parse(userIdClaim));

            return Ok(result);
        }
    }
}
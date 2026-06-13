using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Auth;

namespace BackendSF.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            var userService = ServiceProxy.Create<IUserService>(new Uri("fabric:/TravelPlannerApp/UserService"));
            var result = await userService.RegisterUserAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var userService = ServiceProxy.Create<IUserService>(new Uri("fabric:/TravelPlannerApp/UserService"));
            var result = await userService.LoginUserAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
using BackendSF.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Auth;
using TravelPlanner.Common.DTOs.User;
using TravelPlanner.Common.Interfaces;

namespace BackendSF.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto request)
        {
            var uri = _configuration["ServiceFabricSettings:UserServiceUri"];
            var userService = ServiceProxy.Create<IUserService>(new Uri(uri));
            var result = await userService.RegisterUserAsync(request);
            return result.ToActionResult();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto request)
        {
            var uri = _configuration["ServiceFabricSettings:UserServiceUri"];
            var userService = ServiceProxy.Create<IUserService>(new Uri(uri));
            var result = await userService.LoginUserAsync(request);
            return result.ToActionResult();
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var uri = _configuration["ServiceFabricSettings:UserServiceUri"];
            var userService = ServiceProxy.Create<IUserService>(new Uri(uri));
            var result = await userService.UpdateProfileAsync(Guid.Parse(userIdClaim), request);
            return result.ToActionResult();
        }
    }
}
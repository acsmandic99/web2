using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TravelPlanner.Common;
using UserService.Data;
using UserService.Entities;

namespace UserService
{
    internal sealed class UserService : StatelessService, IUserService
    {
        private readonly UserDbContextFactory _contextFactory;
        private readonly IConfigurationRoot _configuration;
        public UserService(StatelessServiceContext context)
            : base(context)
        {
            _contextFactory = new UserDbContextFactory();
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public async Task<AuthResponseDto> RegisterUserAsync(UserRegisterDto request)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var userExists = await dbContext.Users.AnyAsync(u => u.Name == request.Username || u.Email == request.Email);
            if (userExists)
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Username or email is already taken."
                };
            }

            var newUser = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(newUser);
            await dbContext.SaveChangesAsync();

            return new AuthResponseDto
            {
                IsSuccess = true,
                Message = "User registered successfully."
            };
        }

        public async Task<AuthResponseDto> LoginUserAsync(UserLoginDto request)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Name == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid username or password."
                };
            }

            string token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Token = token,
                Message = "Login successful."
            };
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var secretKey = _configuration["JwtSettings:Secret"];
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
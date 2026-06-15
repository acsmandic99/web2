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
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Auth;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.DTOs.User;
using TravelPlanner.Common.Interfaces;
using UserService.Data;
using UserService.Entities;

namespace UserService
{
    internal sealed class UserService : StatelessService, IUserService
    {
        private readonly UserDbContextFactory _contextFactory;
        private readonly IConfigurationRoot _configuration;

        public UserService(StatelessServiceContext context) : base(context)
        {
            _contextFactory = new UserDbContextFactory();
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public async Task<ResultDto<bool>> RegisterUserAsync(UserRegisterDto request)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var userExists = await dbContext.Users.AnyAsync(u => u.Name == request.Username || u.Email == request.Email);
            if (userExists)
            {
                return ResultDto<bool>.Failure("Username or email is already taken.");
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

            return ResultDto<bool>.Success(true, "User registered successfully.");
        }

        public async Task<ResultDto<string>> LoginUserAsync(UserLoginDto request)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Name == request.Username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return ResultDto<string>.Failure("Invalid username or password.");
            }

            string token = GenerateJwtToken(user);
            return ResultDto<string>.Success(token, "Login successful.");
        }

        public async Task<ResultDto<UserDto>> GetUserByIdAsync(Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return ResultDto<UserDto>.Failure("User not found.");

            var dto = new UserDto
            {
                Id = user.Id,
                Username = user.Name,
                Email = user.Email
            };
            return ResultDto<UserDto>.Success(dto, "User retrieved successfully.");
        }

        public async Task<ResultDto<bool>> UpdateProfileAsync(Guid userId, UpdateProfileDto request)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return ResultDto<bool>.Failure("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            {
                return ResultDto<bool>.Failure("Incorrect current password.");
            }

            if (!string.IsNullOrEmpty(request.Email) && user.Email != request.Email)
            {
                var emailExists = await dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId);
                if (emailExists)
                {
                    return ResultDto<bool>.Failure("Email address is already in use by another account.");
                }
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            await dbContext.SaveChangesAsync();
            return ResultDto<bool>.Success(true, "Profile updated successfully.");
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
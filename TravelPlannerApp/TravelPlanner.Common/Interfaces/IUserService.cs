using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Auth;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.DTOs.User;

namespace TravelPlanner.Common.Interfaces
{
    public interface IUserService : IService
    {
        Task<ResultDto<bool>> RegisterUserAsync(UserRegisterDto request);
        Task<ResultDto<string>> LoginUserAsync(UserLoginDto request);
        Task<ResultDto<UserDto>> GetUserByIdAsync(Guid userId);
        Task<ResultDto<bool>> UpdateProfileAsync(Guid userId, UpdateProfileDto request);
    }
}
using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Auth;
using TravelPlanner.Common.DTOs.Shared;

namespace TravelPlanner.Common.Interfaces
{
    public interface IUserService : IService
    {
        Task<ResultDto<bool>> RegisterUserAsync(UserRegisterDto request);
        Task<ResultDto<string>> LoginUserAsync(UserLoginDto request);
    }
}
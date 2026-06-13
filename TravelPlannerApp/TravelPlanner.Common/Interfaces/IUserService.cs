using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Auth;

namespace TravelPlanner.Common.Interfaces
{
    public interface IUserService : IService
    {
        Task<AuthResponseDto> RegisterUserAsync(UserRegisterDto request);
        Task<AuthResponseDto> LoginUserAsync(UserLoginDto request);
    }
}
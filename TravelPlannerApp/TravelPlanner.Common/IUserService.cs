using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace TravelPlanner.Common
{
    public interface IUserService : IService
    {
        Task<AuthResponseDto> RegisterUserAsync(UserRegisterDto request);
        Task<AuthResponseDto> LoginUserAsync(UserLoginDto request);
    }
}
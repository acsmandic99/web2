using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Trip;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.Enums;

namespace TravelPlanner.Common.Interfaces
{
    public interface IShareService : IService
    {
        Task<ResultDto<TripShareDto>> GenerateShareTokenAsync(Guid tripId, ShareAccessLevel accessLevel, Guid userId);
        Task<ResultDto<bool>> ClaimShareTokenAsync(string token, Guid userId);
        Task<ResultDto<string>> CheckAccessAsync(Guid tripId, Guid userId);
        Task<ResultDto<List<Guid>>> GetSharedUsersAsync(Guid tripId);
        Task<ResultDto<bool>> ClearAllSharesForTripAsync(Guid tripId);
    }
}
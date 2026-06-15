using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Activity;
using TravelPlanner.Common.DTOs.Shared;

namespace TravelPlanner.Common.Interfaces
{
    public interface IActivityService : IService
    {
        Task<ResultDto<ActivityDto>> AddActivityAsync(CreateActivityDto a, Guid userId);
        Task<ResultDto<List<ActivityDto>>> GetTripActivitiesAsync(Guid tripId, Guid userId);
        Task<ResultDto<bool>> UpdateActivityAsync(Guid id, CreateActivityDto a, Guid userId);
        Task<ResultDto<bool>> RemoveActivityAsync(Guid id, Guid userId);
        Task<ResultDto<bool>> SyncDeleteActivityFromExpenseAsync(Guid tripId, string name, double price);
        Task<ResultDto<bool>> RemoveAllActivitiesForTripAsync(Guid tripId);
        Task<ResultDto<bool>> SyncUpdateActivityFromExpenseAsync(Guid tripId, string oldName, double oldPrice, string newName, double newPrice);
    }
}
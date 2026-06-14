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
        Task<ResultDto<ActivityDto>> AddActivityAsync(CreateActivityDto activity, Guid userId);
        Task<ResultDto<List<ActivityDto>>> GetTripActivitiesAsync(Guid tripId, Guid userId);
        Task<ResultDto<bool>> UpdateActivityAsync(Guid activityId, CreateActivityDto activity, Guid userId);
        Task<ResultDto<bool>> RemoveActivityAsync(Guid activityId, Guid userId);
    }
}
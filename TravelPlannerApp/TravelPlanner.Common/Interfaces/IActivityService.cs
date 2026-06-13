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
        Task<ResultDto<ActivityDto>> AddActivityAsync(CreateActivityDto activity);
        Task<ResultDto<List<ActivityDto>>> GetTripActivitiesAsync(Guid tripId);
        Task<ResultDto<bool>> UpdateActivityAsync(Guid activityId, CreateActivityDto activity);
        Task<ResultDto<bool>> RemoveActivityAsync(Guid activityId);
    }
}
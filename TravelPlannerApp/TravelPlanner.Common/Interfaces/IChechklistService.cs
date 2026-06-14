using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Checklist;
using TravelPlanner.Common.DTOs.Shared;

namespace TravelPlanner.Common.Interfaces
{
    public interface IChecklistService : IService
    {
        Task<ResultDto<ChecklistItemDto>> AddItemAsync(CreateChecklistItemDto item, Guid userId);
        Task<ResultDto<List<ChecklistItemDto>>> GetItemsAsync(Guid tripId, Guid userId);
        Task<ResultDto<bool>> ToggleItemAsync(Guid tripId, Guid itemId, Guid userId);
        Task<ResultDto<bool>> DeleteItemAsync(Guid tripId, Guid itemId, Guid userId);
    }
}
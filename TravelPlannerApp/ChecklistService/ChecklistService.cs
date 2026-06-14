using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Checklist;
using TravelPlanner.Common.DTOs.Shared;

namespace ChecklistService
{
    internal sealed class ChecklistService : StatefulService, IChecklistService
    {
        public ChecklistService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<ResultDto<ChecklistItemDto>> AddItemAsync(CreateChecklistItemDto item, Guid userId)
        {
            var checklistDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, List<ChecklistItemDto>>>("checklists");
            string key = $"{item.TripId}_{userId}";

            var dto = new ChecklistItemDto
            {
                Id = Guid.NewGuid(),
                Title = item.Title,
                IsCompleted = false,
                TripId = item.TripId
            };

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await checklistDict.TryGetValueAsync(tx, key);
                var list = result.HasValue ? new List<ChecklistItemDto>(result.Value) : new List<ChecklistItemDto>();
                list.Add(dto);
                await checklistDict.SetAsync(tx, key, list);
                await tx.CommitAsync();
            }

            return ResultDto<ChecklistItemDto>.Success(dto, "Checklist item added successfully.");
        }

        public async Task<ResultDto<List<ChecklistItemDto>>> GetItemsAsync(Guid tripId, Guid userId)
        {
            var checklistDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, List<ChecklistItemDto>>>("checklists");
            string key = $"{tripId}_{userId}";

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await checklistDict.TryGetValueAsync(tx, key);
                if (result.HasValue)
                {
                    return ResultDto<List<ChecklistItemDto>>.Success(result.Value, "Checklist items retrieved successfully.");
                }
                return ResultDto<List<ChecklistItemDto>>.Success(new List<ChecklistItemDto>(), "No checklist items found.");
            }
        }

        public async Task<ResultDto<bool>> ToggleItemAsync(Guid tripId, Guid itemId, Guid userId)
        {
            var checklistDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, List<ChecklistItemDto>>>("checklists");
            string key = $"{tripId}_{userId}";

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await checklistDict.TryGetValueAsync(tx, key);
                if (!result.HasValue)
                {
                    return ResultDto<bool>.Failure("Checklist not found.");
                }

                var list = new List<ChecklistItemDto>(result.Value);
                var item = list.Find(x => x.Id == itemId);
                if (item == null)
                {
                    return ResultDto<bool>.Failure("Checklist item not found.");
                }

                item.IsCompleted = !item.IsCompleted;
                await checklistDict.SetAsync(tx, key, list);
                await tx.CommitAsync();
                return ResultDto<bool>.Success(true, "Checklist item status toggled successfully.");
            }
        }

        public async Task<ResultDto<bool>> DeleteItemAsync(Guid tripId, Guid itemId, Guid userId)
        {
            var checklistDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, List<ChecklistItemDto>>>("checklists");
            string key = $"{tripId}_{userId}";

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await checklistDict.TryGetValueAsync(tx, key);
                if (!result.HasValue)
                {
                    return ResultDto<bool>.Failure("Checklist not found.");
                }

                var list = new List<ChecklistItemDto>(result.Value);
                var item = list.Find(x => x.Id == itemId);
                if (item == null)
                {
                    return ResultDto<bool>.Failure("Checklist item not found.");
                }

                list.Remove(item);
                await checklistDict.SetAsync(tx, key, list);
                await tx.CommitAsync();
                return ResultDto<bool>.Success(true, "Checklist item deleted successfully.");
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }
    }
}
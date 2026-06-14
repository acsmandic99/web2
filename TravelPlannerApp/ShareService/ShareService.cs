using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Trip;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.DTOs.Notification;
using TravelPlanner.Common.Enums;

namespace ShareService
{
    internal sealed class ShareService : StatefulService, IShareService
    {
        public ShareService(StatefulServiceContext context)
            : base(context)
        { }

        public async Task<ResultDto<TripShareDto>> GenerateShareTokenAsync(Guid tripId, ShareAccessLevel accessLevel, Guid userId)
        {
            var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
            var tripResult = await tripService.GetTripByIdAsync(tripId, userId);

            if (!tripResult.IsSuccess || tripResult.Data == null)
            {
                return ResultDto<TripShareDto>.Failure("Trip not found.");
            }

            if (tripResult.Data.UserId != userId)
            {
                return ResultDto<TripShareDto>.Failure("Only the owner can share this trip.");
            }

            var tokensDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, TripShareDto>>("tokens");

            var dto = new TripShareDto
            {
                Id = Guid.NewGuid(),
                TripId = tripId,
                Token = Guid.NewGuid().ToString("N"),
                AccessLevel = accessLevel,
                ClaimedByUserId = null
            };

            using (var tx = this.StateManager.CreateTransaction())
            {
                await tokensDict.AddAsync(tx, dto.Token, dto);
                await tx.CommitAsync();
            }

            return ResultDto<TripShareDto>.Success(dto, "Share token generated successfully.");
        }

        public async Task<ResultDto<bool>> ClaimShareTokenAsync(string token, Guid userId)
        {
            var tokensDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, TripShareDto>>("tokens");
            var permsDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("permissions");
            TripShareDto shareData = null;

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await tokensDict.TryGetValueAsync(tx, token);
                if (!result.HasValue)
                {
                    return ResultDto<bool>.Failure("Invalid share token.");
                }

                var share = result.Value;
                if (share.ClaimedByUserId != null)
                {
                    return ResultDto<bool>.Failure("Token already claimed.");
                }

                var tripService = ServiceProxy.Create<ITripService>(new Uri("fabric:/TravelPlannerApp/TripService"));
                var tripResult = await tripService.GetTripByIdAsync(share.TripId, userId);
                if (tripResult.IsSuccess && tripResult.Data != null && tripResult.Data.UserId == userId)
                {
                    return ResultDto<bool>.Failure("Owner cannot claim their own share token.");
                }

                share.ClaimedByUserId = userId;
                await tokensDict.SetAsync(tx, token, share);

                string permKey = $"{share.TripId}_{userId}";
                await permsDict.SetAsync(tx, permKey, share.AccessLevel.ToString());

                shareData = share;

                await tx.CommitAsync();
            }

            if (shareData != null)
            {
                try
                {
                    var notificationService = ServiceProxy.Create<INotificationService>(new Uri("fabric:/TravelPlannerApp/NotificationService"), new ServicePartitionKey(0L));
                    await notificationService.PublishEventAsync(new NotificationEventDto
                    {
                        EventType = NotificationEventType.TripShareAccepted,
                        Message = $"User {userId} accepted share for Trip {shareData.TripId} with access level {shareData.AccessLevel}",
                        TripId = shareData.TripId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                catch (Exception)
                {
                }
            }

            return ResultDto<bool>.Success(true, "Share token claimed successfully.");
        }

        public async Task<ResultDto<string>> CheckAccessAsync(Guid tripId, Guid userId)
        {
            var permsDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("permissions");

            using (var tx = this.StateManager.CreateTransaction())
            {
                string permKey = $"{tripId}_{userId}";
                var result = await permsDict.TryGetValueAsync(tx, permKey);

                if (result.HasValue)
                {
                    return ResultDto<string>.Success(result.Value, "Access level retrieved.");
                }

                return ResultDto<string>.Success("None", "No access granted.");
            }
        }

        public async Task<ResultDto<List<Guid>>> GetSharedUsersAsync(Guid tripId)
        {
            var permsDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("permissions");
            var sharedUsers = new List<Guid>();

            using (var tx = this.StateManager.CreateTransaction())
            {
                var enumerable = await permsDict.CreateEnumerableAsync(tx);
                using (var enumerator = enumerable.GetAsyncEnumerator())
                {
                    while (await enumerator.MoveNextAsync(System.Threading.CancellationToken.None))
                    {
                        var current = enumerator.Current;
                        if (current.Key.StartsWith($"{tripId}_"))
                        {
                            var parts = current.Key.Split('_');
                            if (parts.Length == 2 && Guid.TryParse(parts[1], out Guid uId))
                            {
                                sharedUsers.Add(uId);
                            }
                        }
                    }
                }
            }

            return ResultDto<List<Guid>>.Success(sharedUsers, "Shared users retrieved successfully.");
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }
    }
}
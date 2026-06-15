using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
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
        private readonly IConfiguration _configuration;

        public ShareService(StatefulServiceContext context) : base(context)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public async Task<ResultDto<TripShareDto>> GenerateShareTokenAsync(Guid tripId, ShareAccessLevel accessLevel, Guid userId)
        {
            var tripServiceUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripServiceUri));
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

            TripShareDto share = null;
            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await tokensDict.TryGetValueAsync(tx, token);
                if (result.HasValue)
                {
                    share = result.Value;
                }
            }

            if (share == null)
            {
                return ResultDto<bool>.Failure("Invalid share token.");
            }

            if (share.ClaimedByUserId != null)
            {
                return ResultDto<bool>.Failure("Token already claimed.");
            }

            var tripServiceUri = _configuration["ServiceFabricSettings:TripServiceUri"];
            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripServiceUri));
            var ownerResult = await tripService.GetTripOwnerAsync(share.TripId);

            if (!ownerResult.IsSuccess)
            {
                return ResultDto<bool>.Failure("The trip associated with this token was not found.");
            }

            if (ownerResult.Data == userId)
            {
                return ResultDto<bool>.Failure("Owner cannot claim their own share token.");
            }

            using (var tx = this.StateManager.CreateTransaction())
            {
                var currentResult = await tokensDict.TryGetValueAsync(tx, token, LockMode.Update);
                if (!currentResult.HasValue) return ResultDto<bool>.Failure("Invalid share token.");

                var currentShare = currentResult.Value;
                if (currentShare.ClaimedByUserId != null)
                {
                    return ResultDto<bool>.Failure("Token already claimed.");
                }

                currentShare.ClaimedByUserId = userId;
                await tokensDict.SetAsync(tx, token, currentShare);

                string permKey = $"{currentShare.TripId}_{userId}";
                await permsDict.SetAsync(tx, permKey, currentShare.AccessLevel.ToString());

                await tx.CommitAsync();
            }

            try
            {
                var notificationServiceUri = _configuration["ServiceFabricSettings:NotificationServiceUri"];
                var notificationService = ServiceProxy.Create<INotificationService>(new Uri(notificationServiceUri), new ServicePartitionKey(0L));
                await notificationService.PublishEventAsync(new NotificationEventDto
                {
                    EventType = NotificationEventType.TripShareAccepted,
                    Message = $"User {userId} accepted share for Trip {share.TripId} with access level {share.AccessLevel}",
                    TripId = share.TripId,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
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
                    while (await enumerator.MoveNextAsync(CancellationToken.None))
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

        public async Task<ResultDto<bool>> ClearAllSharesForTripAsync(Guid tripId)
        {
            var tokensDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, TripShareDto>>("tokens");
            var permsDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("permissions");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var tokensEnum = await tokensDict.CreateEnumerableAsync(tx);
                using (var tokensEnumerator = tokensEnum.GetAsyncEnumerator())
                {
                    var tokensToDelete = new List<string>();
                    while (await tokensEnumerator.MoveNextAsync(CancellationToken.None))
                    {
                        if (tokensEnumerator.Current.Value.TripId == tripId)
                        {
                            tokensToDelete.Add(tokensEnumerator.Current.Key);
                        }
                    }
                    foreach (var tokenKey in tokensToDelete)
                    {
                        await tokensDict.TryRemoveAsync(tx, tokenKey);
                    }
                }

                var permsEnum = await permsDict.CreateEnumerableAsync(tx);
                using (var permsEnumerator = permsEnum.GetAsyncEnumerator())
                {
                    var permsToDelete = new List<string>();
                    while (await permsEnumerator.MoveNextAsync(CancellationToken.None))
                    {
                        if (permsEnumerator.Current.Key.StartsWith($"{tripId}_"))
                        {
                            permsToDelete.Add(permsEnumerator.Current.Key);
                        }
                    }
                    foreach (var permKey in permsToDelete)
                    {
                        await permsDict.TryRemoveAsync(tx, permKey);
                    }
                }

                await tx.CommitAsync();
            }

            return ResultDto<bool>.Success(true, "All trip share records cleared successfully.");
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }
    }
}
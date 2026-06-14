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
using TravelPlanner.Common.DTOs.Notification;
using TravelPlanner.Common.DTOs.Shared;

namespace NotificationService
{
    internal sealed class NotificationService : StatefulService, INotificationService
    {
        private readonly IConfiguration _configuration;

        public NotificationService(StatefulServiceContext context)
            : base(context)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public async Task<ResultDto<bool>> PublishEventAsync(NotificationEventDto notificationEvent)
        {
            var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<NotificationEventDto>>("notificationQueue");

            using (var tx = this.StateManager.CreateTransaction())
            {
                await queue.EnqueueAsync(tx, notificationEvent);
                await tx.CommitAsync();
            }

            return ResultDto<bool>.Success(true, "Event enqueued successfully.");
        }

        public async Task<ResultDto<List<NotificationEventDto>>> GetUserNotificationsAsync(Guid userId)
        {
            var historyDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, List<NotificationEventDto>>>("userNotifications");

            using (var tx = this.StateManager.CreateTransaction())
            {
                var result = await historyDict.TryGetValueAsync(tx, userId);
                if (result.HasValue)
                {
                    return ResultDto<List<NotificationEventDto>>.Success(result.Value, "Notifications retrieved successfully.");
                }
                return ResultDto<List<NotificationEventDto>>.Success(new List<NotificationEventDto>(), "No notifications found.");
            }
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<NotificationEventDto>>("notificationQueue");
            var historyDict = await this.StateManager.GetOrAddAsync<IReliableDictionary<Guid, List<NotificationEventDto>>>("userNotifications");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await queue.TryDequeueAsync(tx);

                    if (result.HasValue)
                    {
                        var evt = result.Value;
                        var targets = new HashSet<Guid>();

                        try
                        {
                            var tripServiceUri = _configuration["ServiceFabricSettings:TripServiceUri"];
                            var tripService = ServiceProxy.Create<ITripService>(new Uri(tripServiceUri));
                            var ownerResult = await tripService.GetTripOwnerAsync(evt.TripId);
                            if (ownerResult.IsSuccess)
                            {
                                targets.Add(ownerResult.Data);
                            }

                            var shareServiceUri = _configuration["ServiceFabricSettings:ShareServiceUri"];
                            var shareService = ServiceProxy.Create<IShareService>(new Uri(shareServiceUri), new ServicePartitionKey(0L));
                            var sharedUsersResult = await shareService.GetSharedUsersAsync(evt.TripId);
                            if (sharedUsersResult.IsSuccess && sharedUsersResult.Data != null)
                            {
                                foreach (var userGuid in sharedUsersResult.Data)
                                {
                                    targets.Add(userGuid);
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }

                        foreach (var userId in targets)
                        {
                            var userHistory = await historyDict.TryGetValueAsync(tx, userId);
                            var list = userHistory.HasValue ? new List<NotificationEventDto>(userHistory.Value) : new List<NotificationEventDto>();
                            list.Add(evt);
                            await historyDict.SetAsync(tx, userId, list);
                        }

                        await tx.CommitAsync();
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }
    }
}
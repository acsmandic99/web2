using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using System;
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
        public NotificationService(StatefulServiceContext context)
            : base(context)
        { }

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

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var queue = await this.StateManager.GetOrAddAsync<IReliableQueue<NotificationEventDto>>("notificationQueue");

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var tx = this.StateManager.CreateTransaction())
                {
                    var result = await queue.TryDequeueAsync(tx);

                    if (result.HasValue)
                    {
                        var evt = result.Value;
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"[EVENT CONSUMED] Type: {evt.EventType} | Message: {evt.Message}");
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
using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Notification;
using TravelPlanner.Common.DTOs.Shared;

namespace TravelPlanner.Common.Interfaces
{
    public interface INotificationService : IService
    {
        Task<ResultDto<bool>> PublishEventAsync(NotificationEventDto notificationEvent);
    }
}
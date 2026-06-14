using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Trip;
using TravelPlanner.Common.DTOs.Shared;
using TravelPlanner.Common.DTOs.Destination;

namespace TravelPlanner.Common.Interfaces
{
    public interface ITripService : IService
    {
        Task<ResultDto<TripDto>> CreateTripAsync(CreateTripDto trip, Guid userId);
        Task<ResultDto<List<TripDto>>> GetUserTripsAsync(Guid userId);
        Task<ResultDto<TripDto>> GetTripByIdAsync(Guid tripId, Guid userId);
        Task<ResultDto<TripDto>> UpdateTripAsync(Guid tripId, CreateTripDto trip, Guid userId);
        Task<ResultDto<bool>> DeleteTripAsync(Guid tripId, Guid userId);

        Task<ResultDto<DestinationDto>> AddDestinationAsync(CreateDestinationDto destination, Guid userId);
        Task<ResultDto<List<DestinationDto>>> GetTripDestinationsAsync(Guid tripId, Guid userId);
        Task<ResultDto<DestinationDto>> UpdateDestinationAsync(Guid destinationId, CreateDestinationDto destination, Guid userId);
        Task<ResultDto<bool>> DeleteDestinationAsync(Guid destinationId, Guid userId);
        Task<ResultDto<Guid>> GetTripOwnerAsync(Guid tripId);
    }
}
using Microsoft.ServiceFabric.Services.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TravelPlanner.Common.DTOs.Trip;

namespace TravelPlanner.Common.Interfaces
{
    public interface ITripService : IService
    {
        Task<TripDto> CreateTripAsync(CreateTripDto trip);
        Task<List<TripDto>> GetUserTripsAsync(Guid userId);
        Task<TripDto> GetTripByIdAsync(Guid tripId);
        Task<bool> DeleteTripAsync(Guid tripId);
    }
}
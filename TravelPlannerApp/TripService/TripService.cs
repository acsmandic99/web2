using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;
using TravelPlanner.Common.Interfaces;
using TravelPlanner.Common.DTOs.Trip;
using TripService.Data;
using TripService.Entities;

namespace TripService
{
    internal sealed class TripService : StatelessService, ITripService
    {
        private readonly TripDbContextFactory _contextFactory;

        public TripService(StatelessServiceContext context)
            : base(context)
        {
            _contextFactory = new TripDbContextFactory();
        }

        public async Task<TripDto> CreateTripAsync(CreateTripDto trip)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var newTrip = new Trip
            {
                Id = Guid.NewGuid(),
                Title = trip.Title,
                Description = trip.Description,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                EstimatedBudget = trip.EstimatedBudget,
                UserId = trip.UserId
            };

            dbContext.Trips.Add(newTrip);
            await dbContext.SaveChangesAsync();

            return new TripDto
            {
                Id = newTrip.Id,
                Title = newTrip.Title,
                Description = newTrip.Description,
                StartDate = newTrip.StartDate,
                EndDate = newTrip.EndDate,
                EstimatedBudget = newTrip.EstimatedBudget,
                UserId = newTrip.UserId
            };
        }

        public async Task<List<TripDto>> GetUserTripsAsync(Guid userId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var trips = await dbContext.Trips
                .Where(t => t.UserId == userId)
                .Select(t => new TripDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    EstimatedBudget = t.EstimatedBudget,
                    UserId = t.UserId
                })
                .ToListAsync();

            return trips;
        }

        public async Task<TripDto> GetTripByIdAsync(Guid tripId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var t = await dbContext.Trips.FirstOrDefaultAsync(trip => trip.Id == tripId);
            if (t == null)
            {
                return null;
            }

            return new TripDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                EstimatedBudget = t.EstimatedBudget,
                UserId = t.UserId
            };
        }

        public async Task<bool> DeleteTripAsync(Guid tripId)
        {
            using var dbContext = _contextFactory.CreateDbContext(null);

            var trip = await dbContext.Trips.FirstOrDefaultAsync(t => t.Id == tripId);
            if (trip == null)
            {
                return false;
            }

            dbContext.Trips.Remove(trip);
            await dbContext.SaveChangesAsync();
            return true;
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}
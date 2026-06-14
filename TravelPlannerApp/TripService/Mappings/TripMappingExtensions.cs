using TravelPlanner.Common.DTOs.Trip;
using TravelPlanner.Common.DTOs.Destination;
using TripService.Entities;

namespace TripService.Mappings
{
    public static class TripMappingExtensions
    {
        public static TripDto MapToDto(this Trip t)
        {
            return new TripDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                StartDate = t.StartDate,
                EndDate = t.EndDate,
                EstimatedBudget = t.EstimatedBudget,
                GeneralNotes = t.GeneralNotes,
                UserId = t.UserId
            };
        }

        public static DestinationDto MapToDto(this Destination d)
        {
            return new DestinationDto
            {
                Id = d.Id,
                Name = d.Name,
                Location = d.Location,
                ArrivalDate = d.ArrivalDate,
                DepartureDate = d.DepartureDate,
                Notes = d.Notes,
                TripId = d.TripId
            };
        }

        public static TripShareDto MapToDto(this TripShare s)
        {
            return new TripShareDto
            {
                Id = s.Id,
                TripId = s.TripId,
                Token = s.Token,
                AccessLevel = s.AccessLevel,
                ClaimedByUserId = s.ClaimedByUserId
            };
        }
    }
}
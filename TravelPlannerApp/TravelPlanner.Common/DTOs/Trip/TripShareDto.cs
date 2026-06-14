using System;
using TravelPlanner.Common.Enums;

namespace TravelPlanner.Common.DTOs.Trip
{
    public class TripShareDto
    {
        public Guid Id { get; set; }
        public Guid TripId { get; set; }
        public string Token { get; set; }
        public ShareAccessLevel AccessLevel { get; set; }
        public Guid? ClaimedByUserId { get; set; }
    }
}
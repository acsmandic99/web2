using System;

namespace TripService.Entities
{
    public class Destination
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public DateTime ArrivalDate { get; set; }
        public DateTime DepartureDate { get; set; }
        public string Notes { get; set; }
        public Guid TripId { get; set; }
    }
}
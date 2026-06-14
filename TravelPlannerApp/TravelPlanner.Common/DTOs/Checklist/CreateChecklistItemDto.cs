

namespace TravelPlanner.Common.DTOs.Checklist
{
    public class CreateChecklistItemDto
    {
        public string Title { get; set; }
        public Guid TripId { get; set; }
    }
}

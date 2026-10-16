// =============================================================================
// Event.cs – A race / walk / cycle event created by an Organiser
// =============================================================================

namespace Part2_Raceday_API.Models;

public class Event
{
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }

    // Allowed values: "Run", "Walk", "Cycle"
    public string EventType { get; set; } = string.Empty;

    // Who created this event
    public int OrganiserId { get; set; }

    // ----- Navigation properties -----
    public AppUser Organiser { get; set; } = null!;
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

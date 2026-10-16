// =============================================================================
// Category.cs – Age / distance groups that belong to an Event
// =============================================================================
// Examples: "Under 20", "Senior", "10 km", "21 km", "Open"
// A participant enrols into a specific category of an event.
// =============================================================================

namespace Part2_Raceday_API.Models;

public class Category
{
    public int CategoryId { get; set; }

    // The event this category belongs to
    public int EventId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // ----- Navigation properties -----
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

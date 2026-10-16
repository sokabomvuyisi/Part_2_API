// =============================================================================
// Enrolment.cs – A Participant signing up for a specific Event + Category
// =============================================================================
// Status flow:
//   Pending   → just enrolled (default)
//   Confirmed → Organiser accepted the enrolment
//   Cancelled → Organiser or system cancelled it
// =============================================================================

namespace Part2_Raceday_API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }

    // "Pending", "Confirmed" or "Cancelled"
    public string Status { get; set; } = "Pending";

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    // ----- Navigation properties -----
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public AppUser Participant { get; set; } = null!;

    // Optional – a result may or may not exist yet
    public Result? Result { get; set; }
}

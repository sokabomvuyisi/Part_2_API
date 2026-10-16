// =============================================================================
// Result.cs – Finish time and position recorded by an Organiser
// =============================================================================
// There is a one-to-one relationship with Enrolment:
//   - An enrolment can have zero or one result
//   - A result always belongs to exactly one enrolment
// =============================================================================

namespace Part2_Raceday_API.Models;

public class Result
{
    public int ResultId { get; set; }

    // Links back to the enrolment
    public int EnrolmentId { get; set; }

    // How long the participant took to finish
    public TimeSpan FinishTime { get; set; }

    // Finishing position (1 = first place, 2 = second, …)
    public int Position { get; set; }

    // ----- Navigation property -----
    public Enrolment Enrolment { get; set; } = null!;
}

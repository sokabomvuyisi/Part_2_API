namespace Part2_Raceday_API.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    // Navigation
    public Event Event { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public AppUser Participant { get; set; } = null!;
    public Result? Result { get; set; }
}
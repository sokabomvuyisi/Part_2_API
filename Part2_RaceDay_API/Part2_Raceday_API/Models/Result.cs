namespace Part2_Raceday_API.Models;

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }

    // Navigation
    public Enrolment Enrolment { get; set; } = null!;
}
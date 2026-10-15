namespace Part2_Raceday_API.Models;

public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty; // Under 20, Senior, 10km, 21km etc.
    public string? Description { get; set; }

    // Navigation
    public Event Event { get; set; } = null!;
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
// =============================================================================
// AppUser.cs – Represents a registered user (Organiser or Participant)
// =============================================================================
// Passwords are NEVER stored in plain text.
// We store only the hashed version produced by ASP.NET Core's PasswordHasher.
// =============================================================================

namespace Part2_Raceday_API.Models;

public class AppUser
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Hashed password (never the original plain-text password)
    public string PasswordHash { get; set; } = string.Empty;

    // Foreign key to the Role table
    public int RoleId { get; set; }

    // ----- Navigation properties (used by EF Core for relationships) -----
    public Role Role { get; set; } = null!;

    // Events that this user has organised (only relevant when Role = Organiser)
    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();

    // Enrolments this user has made (only relevant when Role = Participant)
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}

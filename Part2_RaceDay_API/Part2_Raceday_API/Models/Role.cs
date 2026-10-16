// =============================================================================
// Role.cs – Simple lookup table for user roles
// =============================================================================
// The system only has two roles:
//   1 = Organiser   → can create events, categories and record results
//   2 = Participant → can enrol in events and view their own results
// =============================================================================

namespace Part2_Raceday_API.Models;

public class Role
{
    public int RoleId { get; set; }

    // "Organiser" or "Participant"
    public string Name { get; set; } = string.Empty;

    // Navigation property – all users that have this role
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}

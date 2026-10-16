// =============================================================================
// ProfileController.cs – View and update the currently logged-in user's profile
// =============================================================================
// Both Organisers and Participants can use these endpoints.
// A user can only ever see or change THEIR OWN profile.
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Part2_Raceday_API.Data;
using Part2_Raceday_API.Filters;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Part2_Raceday_API.Controllers;

[ApiController]
[Route("api/[controller]")]                 // → /api/profile/...
[SwaggerTag("User Profile - View and update own profile")]
public class ProfileController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ProfileController(RaceDayDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------------------
    // GET /api/profile/me
    // -------------------------------------------------------------------------
    /// <summary>Get the currently logged-in user's profile.</summary>
    [HttpGet("me")]
    [AuthorizeRole]                         // Any authenticated user
    [SwaggerOperation(Summary = "Get my profile")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyProfile()
    {
        // UserId comes from the session that was created at login
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new ProfileDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.Name
        });
    }

    // -------------------------------------------------------------------------
    // PUT /api/profile/me
    // -------------------------------------------------------------------------
    /// <summary>Update the currently logged-in user's own profile information.</summary>
    [HttpPut("me")]
    [AuthorizeRole]
    [SwaggerOperation(Summary = "Update my profile")]
    [ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First name and last name are required." });

        // If the user is changing their email, make sure the new one is not already taken
        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower() && u.UserId != userId);

            if (emailTaken)
                return BadRequest(new { message = "Email is already in use by another account." });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email.Trim().ToLower();

        await _context.SaveChangesAsync();

        return Ok(new ProfileDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.Name
        });
    }
}

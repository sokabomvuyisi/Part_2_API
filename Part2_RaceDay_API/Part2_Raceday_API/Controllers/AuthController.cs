// =============================================================================
// AuthController.cs – Registration, Login and Logout
// =============================================================================
// This controller is PUBLIC (no [AuthorizeRole] attribute).
// Anyone can register or log in.
// After a successful login we store UserId + Role in the server-side session.
// That session is what the AuthorizeRoleAttribute later checks.
// =============================================================================

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Part2_Raceday_API.Data;
using Part2_Raceday_API.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Part2_Raceday_API.Controllers;

[ApiController]
[Route("api/[controller]")]                 // → /api/auth/...
[SwaggerTag("Authentication - Register, Login and Logout")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    // ASP.NET Core's built-in password hasher (PBKDF2).
    // We never store the plain-text password.
    private readonly PasswordHasher<AppUser> _passwordHasher = new();

    public AuthController(RaceDayDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/register
    // -------------------------------------------------------------------------
    /// <summary>
    /// Register a new Organiser or Participant account.
    /// Password is hashed before storage. Role must be "Organiser" or "Participant".
    /// </summary>
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register a new user",
                      Description = "Creates a new account. Password is never stored in plain text.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // ----- Basic validation -----
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email and password are required." });

        if (request.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        // Only two roles are allowed in this system
        var roleName = request.Role?.Trim();
        if (roleName != "Organiser" && roleName != "Participant")
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });

        // Prevent duplicate e-mail addresses
        var existing = await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existing)
            return BadRequest(new { message = "An account with this email already exists." });

        // Look up the Role row that was seeded in the database
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
            return BadRequest(new { message = "Invalid role." });

        // Create the user entity
        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim().ToLower(),
            RoleId = role.RoleId
        };

        // Hash the password BEFORE saving (never store plain text!)
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Build a clean response DTO (no password hash)
        var response = new AuthResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = role.Name,
            Message = "Registration successful. You can now log in."
        };

        // 201 Created – the resource has been created
        return CreatedAtAction(nameof(Login), response);
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/login
    // -------------------------------------------------------------------------
    /// <summary>
    /// Log in with email and password.
    /// Creates a server-side session containing UserId and Role.
    /// </summary>
    [HttpPost("login")]
    [SwaggerOperation(Summary = "Login",
                      Description = "Authenticates the user and creates a session that stores UserId and Role.")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new { message = "Email and password are required." });

        // Find the user and also load the related Role (needed for the session)
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower());

        if (user == null)
            return Unauthorized(new { message = "Invalid email or password." });

        // Verify the password against the stored hash
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        // ----- Create the server-side session -----
        // These two values are what AuthorizeRoleAttribute will check later
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role.Name);

        var response = new AuthResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role.Name,
            Message = "Login successful."
        };

        return Ok(response);
    }

    // -------------------------------------------------------------------------
    // POST /api/auth/logout
    // -------------------------------------------------------------------------
    /// <summary>
    /// Clear the current session (logout).
    /// </summary>
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "Logout", Description = "Clears the server-side session.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out successfully." });
    }
}

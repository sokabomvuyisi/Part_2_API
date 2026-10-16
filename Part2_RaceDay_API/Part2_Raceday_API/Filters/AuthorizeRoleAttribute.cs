// =============================================================================
// AuthorizeRoleAttribute.cs – Custom authorization filter
// =============================================================================
// This is the heart of our security model.
// Instead of using JWT tokens or ASP.NET Identity, we use a simple server-side
// session that stores:
//   - UserId  (integer)
//   - Role    ("Organiser" or "Participant")
//
// How to use it on a controller action:
//   [AuthorizeRole]                     → any logged-in user
//   [AuthorizeRole("Organiser")]        → only Organisers
//   [AuthorizeRole("Participant")]      → only Participants
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Part2_Raceday_API.Filters;

/// <summary>
/// Custom attribute that enforces authentication + optional role check using server-side session.
/// Usage: [AuthorizeRole] or [AuthorizeRole("Organiser")] or [AuthorizeRole("Participant")]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
{
    // The roles that are allowed to access the decorated endpoint.
    // Empty array means "any authenticated user is allowed".
    private readonly string[] _allowedRoles;

    public AuthorizeRoleAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles ?? Array.Empty<string>();
    }

    // This method is called automatically by ASP.NET Core before the action executes
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        // Retrieve the values that were stored during Login
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        // ----- Step 1: Is the user logged in at all? -----
        if (userId == null || string.IsNullOrEmpty(role))
        {
            // 401 Unauthorized – client should redirect to login
            context.Result = new UnauthorizedObjectResult(new
            {
                message = "You must be logged in to access this resource."
            });
            return;
        }

        // ----- Step 2: Does the user have one of the required roles? -----
        if (_allowedRoles.Length > 0 &&
            !_allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            // 403 Forbidden – user is logged in but not allowed to perform this action
            context.Result = new ObjectResult(new
            {
                message = $"Access denied. This action requires one of the following roles: {string.Join(", ", _allowedRoles)}"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        // If we reach here, the request is allowed to continue to the controller action
    }
}

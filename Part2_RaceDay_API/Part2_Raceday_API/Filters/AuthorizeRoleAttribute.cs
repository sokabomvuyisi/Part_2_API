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
    private readonly string[] _allowedRoles;

    public AuthorizeRoleAttribute(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles ?? Array.Empty<string>();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        // Not logged in
        if (userId == null || string.IsNullOrEmpty(role))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "You must be logged in to access this resource." });
            return;
        }

        // Role restriction applied
        if (_allowedRoles.Length > 0 && !_allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new { message = $"Access denied. This action requires one of the following roles: {string.Join(", ", _allowedRoles)}" })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
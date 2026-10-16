// =============================================================================
// CategoriesController.cs – Age / distance categories that belong to an Event
// =============================================================================
// Security rules:
//   - Viewing categories  → any authenticated user
//   - Creating / updating / deleting → only the Organiser who owns the event
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Part2_Raceday_API.Data;
using Part2_Raceday_API.Filters;
using Part2_Raceday_API.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Part2_Raceday_API.Controllers;

[ApiController]
[Route("api")]                              // Routes are nested under /api/events/...
[SwaggerTag("Categories - Age/distance categories belonging to an event")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------------------
    // GET /api/events/{eventId}/categories
    // -------------------------------------------------------------------------
    /// <summary>Get all categories for a specific event (both roles can view).</summary>
    [HttpGet("events/{eventId:int}/categories")]
    [AuthorizeRole]
    [SwaggerOperation(Summary = "Get categories for an event")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var exists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!exists)
            return NotFound(new { message = "Event not found." });

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                EventId = c.EventId,
                Name = c.Name,
                Description = c.Description
            })
            .ToListAsync();

        return Ok(categories);
    }

    // -------------------------------------------------------------------------
    // POST /api/events/{eventId}/categories
    // -------------------------------------------------------------------------
    /// <summary>Create a category for an event (only the Organiser who owns the event).</summary>
    [HttpPost("events/{eventId:int}/categories")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Create a category for an event")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CreateCategoryRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        // Ownership check
        if (ev.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only add categories to events that you organised." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Category name is required." });

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var dto = new CategoryDto
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            Description = category.Description
        };

        return CreatedAtAction(nameof(GetByEvent), new { eventId }, dto);
    }

    // -------------------------------------------------------------------------
    // PUT /api/categories/{id}
    // -------------------------------------------------------------------------
    /// <summary>Update a category (only the owning Organiser).</summary>
    [HttpPut("categories/{id:int}")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Update a category")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CreateCategoryRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        // Include the parent Event so we can check ownership
        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only update categories of events that you organised." });

        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();

        await _context.SaveChangesAsync();

        return Ok(new CategoryDto
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            Description = category.Description
        });
    }

    // -------------------------------------------------------------------------
    // DELETE /api/categories/{id}
    // -------------------------------------------------------------------------
    /// <summary>Delete a category (only the owning Organiser).</summary>
    [HttpDelete("categories/{id:int}")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Delete a category")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
            return NotFound(new { message = "Category not found." });

        if (category.Event.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete categories of events that you organised." });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

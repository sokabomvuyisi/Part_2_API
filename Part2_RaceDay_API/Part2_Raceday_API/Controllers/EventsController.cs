// =============================================================================
// EventsController.cs – CRUD operations for race events
// =============================================================================
// Security rules:
//   - Viewing events      → any authenticated user
//   - Creating / updating / deleting → only Organisers
//   - An Organiser may only modify events that THEY created
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
[Route("api/[controller]")]                 // → /api/events/...
[SwaggerTag("Events - Create, view, update and delete race events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------------------
    // GET /api/events
    // -------------------------------------------------------------------------
    /// <summary>Get all events (both roles can view).</summary>
    [HttpGet]
    [AuthorizeRole]                         // Any logged-in user
    [SwaggerOperation(Summary = "Get all events")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var events = await _context.Events
            .Include(e => e.Organiser)      // Needed to build OrganiserName
            .OrderBy(e => e.EventDate)
            .Select(e => new EventDto
            {
                EventId = e.EventId,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                OrganiserId = e.OrganiserId,
                OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName
            })
            .ToListAsync();

        return Ok(events);
    }

    // -------------------------------------------------------------------------
    // GET /api/events/{id}
    // -------------------------------------------------------------------------
    /// <summary>Get a single event by ID.</summary>
    [HttpGet("{id:int}")]
    [AuthorizeRole]
    [SwaggerOperation(Summary = "Get event by ID")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _context.Events
            .Include(ev => ev.Organiser)
            .FirstOrDefaultAsync(ev => ev.EventId == id);

        if (e == null)
            return NotFound(new { message = "Event not found." });

        return Ok(new EventDto
        {
            EventId = e.EventId,
            Name = e.Name,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            OrganiserId = e.OrganiserId,
            OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName
        });
    }

    // -------------------------------------------------------------------------
    // GET /api/events/mine
    // -------------------------------------------------------------------------
    /// <summary>Get events organised by the currently logged-in Organiser.</summary>
    [HttpGet("mine")]
    [AuthorizeRole("Organiser")]            // Only Organisers
    [SwaggerOperation(Summary = "Get my organised events")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        // UserId was stored in the session during login
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var events = await _context.Events
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .Select(e => new EventDto
            {
                EventId = e.EventId,
                Name = e.Name,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                OrganiserId = e.OrganiserId
            })
            .ToListAsync();

        return Ok(events);
    }

    // -------------------------------------------------------------------------
    // POST /api/events
    // -------------------------------------------------------------------------
    /// <summary>Create a new event (Organiser only).</summary>
    [HttpPost]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Create a new event")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Location))
            return BadRequest(new { message = "Name and Location are required." });

        // Restrict EventType to a known set of values
        var validTypes = new[] { "Run", "Walk", "Cycle" };
        if (!validTypes.Contains(request.EventType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "EventType must be Run, Walk or Cycle." });

        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var entity = new Event
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            OrganiserId = userId          // The logged-in user becomes the owner
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync();

        var dto = new EventDto
        {
            EventId = entity.EventId,
            Name = entity.Name,
            Description = entity.Description,
            EventDate = entity.EventDate,
            Location = entity.Location,
            DistanceKm = entity.DistanceKm,
            EventType = entity.EventType,
            OrganiserId = entity.OrganiserId
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.EventId }, dto);
    }

    // -------------------------------------------------------------------------
    // PUT /api/events/{id}
    // -------------------------------------------------------------------------
    /// <summary>Update an existing event (only the owning Organiser).</summary>
    [HttpPut("{id:int}")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Update an event")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEventRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var entity = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (entity == null)
            return NotFound(new { message = "Event not found." });

        // Ownership check – an Organiser may only edit their own events
        if (entity.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only update events that you organised." });

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim() ?? string.Empty;
        entity.EventDate = request.EventDate;
        entity.Location = request.Location.Trim();
        entity.DistanceKm = request.DistanceKm;
        entity.EventType = request.EventType;

        await _context.SaveChangesAsync();

        return Ok(new EventDto
        {
            EventId = entity.EventId,
            Name = entity.Name,
            Description = entity.Description,
            EventDate = entity.EventDate,
            Location = entity.Location,
            DistanceKm = entity.DistanceKm,
            EventType = entity.EventType,
            OrganiserId = entity.OrganiserId
        });
    }

    // -------------------------------------------------------------------------
    // DELETE /api/events/{id}
    // -------------------------------------------------------------------------
    /// <summary>Delete an event (only the owning Organiser).</summary>
    [HttpDelete("{id:int}")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Delete an event")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var entity = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (entity == null)
            return NotFound(new { message = "Event not found." });

        if (entity.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "You can only delete events that you organised." });

        _context.Events.Remove(entity);
        await _context.SaveChangesAsync();

        return NoContent();     // 204 – successfully deleted, no body
    }
}

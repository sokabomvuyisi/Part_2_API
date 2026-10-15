using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Event Enrolments - Participants enter events, Organisers view enrolments")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Participant enrols in an event by selecting a category.
    /// </summary>
    [HttpPost]
    [AuthorizeRole("Participant")]
    [SwaggerOperation(Summary = "Enrol in an event")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Enrol([FromBody] CreateEnrolmentRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == request.EventId);
        if (ev == null)
            return BadRequest(new { message = "Event not found." });

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);

        if (category == null)
            return BadRequest(new { message = "Category not found for this event." });

        // Prevent duplicate enrolment
        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(en => en.EventId == request.EventId && en.ParticipantId == userId);

        if (alreadyEnrolled)
            return BadRequest(new { message = "You are already enrolled in this event." });

        var enrolment = new Enrolment
        {
            EventId = request.EventId,
            CategoryId = request.CategoryId,
            ParticipantId = userId,
            Status = "Pending",
            EnrolmentDate = DateTime.UtcNow
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        // Load related data for response
        await _context.Entry(enrolment).Reference(e => e.Event).LoadAsync();
        await _context.Entry(enrolment).Reference(e => e.Category).LoadAsync();
        await _context.Entry(enrolment).Reference(e => e.Participant).LoadAsync();

        var dto = new EnrolmentDto
        {
            EnrolmentId = enrolment.EnrolmentId,
            EventId = enrolment.EventId,
            EventName = enrolment.Event.Name,
            CategoryId = enrolment.CategoryId,
            CategoryName = enrolment.Category.Name,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = enrolment.Participant.FirstName + " " + enrolment.Participant.LastName,
            Status = enrolment.Status,
            EnrolmentDate = enrolment.EnrolmentDate
        };

        return CreatedAtAction(nameof(GetMine), dto);
    }

    /// <summary>
    /// Get enrolments of the currently logged-in Participant.
    /// </summary>
    [HttpGet("mine")]
    [AuthorizeRole("Participant")]
    [SwaggerOperation(Summary = "Get my enrolments")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var list = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Where(e => e.ParticipantId == userId)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                EventId = e.EventId,
                EventName = e.Event.Name,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                ParticipantId = e.ParticipantId,
                Status = e.Status,
                EnrolmentDate = e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Organiser views all enrolments for one of their events.
    /// </summary>
    [HttpGet("event/{eventId:int}")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Get enrolments for an event (Organiser)")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view enrolments for events that you organised." });

        var list = await _context.Enrolments
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .Where(e => e.EventId == eventId)
            .Select(e => new EnrolmentDto
            {
                EnrolmentId = e.EnrolmentId,
                EventId = e.EventId,
                EventName = ev.Name,
                CategoryId = e.CategoryId,
                CategoryName = e.Category.Name,
                ParticipantId = e.ParticipantId,
                ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
                Status = e.Status,
                EnrolmentDate = e.EnrolmentDate
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Organiser updates the status of an enrolment (Pending / Confirmed / Cancelled).
    /// </summary>
    [HttpPut("{id:int}/status")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Update enrolment status")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateEnrolmentStatusRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment == null)
            return NotFound(new { message = "Enrolment not found." });

        if (enrolment.Event.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only manage enrolments for your own events." });

        var valid = new[] { "Pending", "Confirmed", "Cancelled" };
        if (!valid.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "Status must be Pending, Confirmed or Cancelled." });

        enrolment.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new EnrolmentDto
        {
            EnrolmentId = enrolment.EnrolmentId,
            EventId = enrolment.EventId,
            EventName = enrolment.Event.Name,
            CategoryId = enrolment.CategoryId,
            CategoryName = enrolment.Category.Name,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = enrolment.Participant.FirstName + " " + enrolment.Participant.LastName,
            Status = enrolment.Status,
            EnrolmentDate = enrolment.EnrolmentDate
        });
    }
}
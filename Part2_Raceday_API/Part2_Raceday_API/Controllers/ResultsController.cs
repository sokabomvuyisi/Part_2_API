using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Part2_Raceday_API.Data;
using Part2_Raceday_API.Filters;
using Part2_Raceday_API.Models;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace Part2_Raceday_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[SwaggerTag("Results - Organisers record finish times, Participants view their results")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Organiser records a result (finish time + position) for an enrolment.
    /// </summary>
    [HttpPost]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Record a result")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateResultRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Participant)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

        if (enrolment == null)
            return BadRequest(new { message = "Enrolment not found." });

        if (enrolment.Event.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only record results for events that you organised." });

        if (enrolment.Result != null)
            return BadRequest(new { message = "A result already exists for this enrolment." });

        if (request.Position < 1)
            return BadRequest(new { message = "Position must be 1 or greater." });

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        var dto = new ResultDto
        {
            ResultId = result.ResultId,
            EnrolmentId = result.EnrolmentId,
            EventId = enrolment.EventId,
            EventName = enrolment.Event.Name,
            ParticipantId = enrolment.ParticipantId,
            ParticipantName = enrolment.Participant.FirstName + " " + enrolment.Participant.LastName,
            FinishTime = result.FinishTime,
            Position = result.Position
        };

        return CreatedAtAction(nameof(GetMine), dto);
    }

    /// <summary>
    /// Participant views their own results.
    /// </summary>
    [HttpGet("mine")]
    [AuthorizeRole("Participant")]
    [SwaggerOperation(Summary = "Get my results")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var list = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Event)
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Where(r => r.Enrolment.ParticipantId == userId)
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                EventId = r.Enrolment.EventId,
                EventName = r.Enrolment.Event.Name,
                ParticipantId = r.Enrolment.ParticipantId,
                ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
                FinishTime = r.FinishTime,
                Position = r.Position
            })
            .ToListAsync();

        return Ok(list);
    }

    /// <summary>
    /// Organiser views all results for one of their events.
    /// </summary>
    [HttpGet("event/{eventId:int}")]
    [AuthorizeRole("Organiser")]
    [SwaggerOperation(Summary = "Get results for an event")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByEvent(int eventId)
    {
        var userId = HttpContext.Session.GetInt32("UserId")!.Value;

        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null)
            return NotFound(new { message = "Event not found." });

        if (ev.OrganiserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view results for events that you organised." });

        var list = await _context.Results
            .Include(r => r.Enrolment)
                .ThenInclude(e => e.Participant)
            .Where(r => r.Enrolment.EventId == eventId)
            .Select(r => new ResultDto
            {
                ResultId = r.ResultId,
                EnrolmentId = r.EnrolmentId,
                EventId = eventId,
                EventName = ev.Name,
                ParticipantId = r.Enrolment.ParticipantId,
                ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
                FinishTime = r.FinishTime,
                Position = r.Position
            })
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(list);
    }
}
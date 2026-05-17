using System.Security.Claims;
using System.Text.Json;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/complaints")]
public class ComplaintsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ComplaintsController(AppDbContext db) { _db = db; }

    // GET /api/v1/complaints?downJourney=true&status=InProgress
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ComplaintListItemDto>>> List(
        [FromQuery] bool? downJourney,
        [FromQuery] ComplaintStatus? status,
        CancellationToken ct)
    {
        var q = _db.Complaints.AsNoTracking().AsQueryable();
        if (downJourney is not null) q = q.Where(c => c.DownJourney == downJourney);
        if (status is not null) q = q.Where(c => c.Status == status);
        var rows = await q.OrderByDescending(c => c.OpenedAt).Select(c => c.ToListItem()).ToListAsync(ct);
        return Ok(rows);
    }

    // GET /api/v1/complaints/by-category — used by the dashboard chart.
    [HttpGet("by-category")]
    public async Task<ActionResult<IReadOnlyList<ComplaintsByCategoryDto>>> ByCategory(CancellationToken ct)
    {
        var rows = await _db.Complaints.AsNoTracking()
            .GroupBy(c => c.Category)
            .Select(g => new ComplaintsByCategoryDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ComplaintDto>> Get(long id, CancellationToken ct)
    {
        var c = await _db.Complaints.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        return Ok(c.ToDto());
    }

    // PATCH /api/v1/complaints/{id}/status — also stamps ClosedAt when moving
    // to Resolved/Closed and clears it on reopen.
    [HttpPatch("{id:long}/status")]
    public async Task<ActionResult<ComplaintDto>> UpdateStatus(long id, [FromBody] UpdateComplaintStatusRequest req, CancellationToken ct)
    {
        var c = await _db.Complaints.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        var prev = c.Status;
        c.Status = req.Status;
        var closing = req.Status is ComplaintStatus.Resolved or ComplaintStatus.Closed;
        c.ClosedAt = closing ? DateTime.UtcNow : null;
        c.UpdatedAt = DateTime.UtcNow;
        _db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = c.Id,
            Kind = "status",
            PayloadJson = JsonSerializer.Serialize(new { from = prev.ToString(), to = req.Status.ToString() }),
            ByUserId = CurrentUserId(),
        });
        await _db.SaveChangesAsync(ct);
        return Ok(c.ToDto());
    }

    [HttpPost("{id:long}/notes")]
    public async Task<IActionResult> AddNote(long id, [FromBody] AddComplaintNoteRequest req, CancellationToken ct)
    {
        var c = await _db.Complaints.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Note)) return BadRequest();
        _db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = c.Id,
            Kind = "note",
            PayloadJson = JsonSerializer.Serialize(new { text = req.Note.Trim() }),
            ByUserId = CurrentUserId(),
        });
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:long}/assign")]
    public async Task<ActionResult<ComplaintDto>> Assign(long id, [FromBody] AssignComplaintRequest req, CancellationToken ct)
    {
        var c = await _db.Complaints.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (c is null) return NotFound();
        c.AssignedTo = req.UserId;
        c.UpdatedAt = DateTime.UtcNow;
        _db.ComplaintEvents.Add(new ComplaintEvent
        {
            ComplaintId = c.Id,
            Kind = "assign",
            PayloadJson = JsonSerializer.Serialize(new { userId = req.UserId }),
            ByUserId = CurrentUserId(),
        });
        await _db.SaveChangesAsync(ct);
        return Ok(c.ToDto());
    }

    private long? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var v) ? v : null;
    }
}

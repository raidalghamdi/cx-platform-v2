using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Channels;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/inbox")]
public class InboxController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IChannelAdapterRegistry _adapters;

    public InboxController(AppDbContext db, IChannelAdapterRegistry adapters)
    {
        _db = db; _adapters = adapters;
    }

    // GET /api/v1/inbox/threads?channel=Email&status=New
    [HttpGet("threads")]
    public async Task<ActionResult<IReadOnlyList<InboxThreadDto>>> List(
        [FromQuery] InboxChannel? channel,
        [FromQuery] InboxStatus? status,
        CancellationToken ct)
    {
        var q = _db.InboxThreads.AsNoTracking().AsQueryable();
        if (channel is not null) q = q.Where(t => t.Channel == channel);
        if (status is not null) q = q.Where(t => t.Status == status);
        var rows = await q.OrderByDescending(t => t.ReceivedAt).Select(t => t.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("threads/{id:long}")]
    public async Task<ActionResult<InboxThreadDto>> Get(long id, CancellationToken ct)
    {
        var t = await _db.InboxThreads.FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? NotFound() : Ok(t.ToDto());
    }

    // POST /api/v1/inbox/threads/{id}/reply — invokes the channel adapter,
    // and on success stamps RepliedAt + Status=Replied.
    [HttpPost("threads/{id:long}/reply")]
    public async Task<ActionResult<InboxThreadDto>> Reply(long id, [FromBody] ReplyToThreadRequest req, CancellationToken ct)
    {
        var t = await _db.InboxThreads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Body)) return BadRequest(new { error = "body is required" });

        var adapter = _adapters.Get(t.Channel);
        var result = await adapter.SendAsync(t.Id, new SendPayload(req.Body, req.Subject), ct);
        if (!result.Ok)
            return StatusCode(502, new { error = result.Error ?? "adapter failed" });

        t.Status = InboxStatus.Replied;
        t.RepliedAt = DateTime.UtcNow;
        t.ReplyBody = req.Body;
        t.ReplySubject = req.Subject;
        t.ExternalId = result.ExternalId;
        t.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(t.ToDto());
    }

    [HttpPatch("threads/{id:long}/status")]
    public async Task<ActionResult<InboxThreadDto>> SetStatus(long id, [FromBody] UpdateThreadStatusRequest req, CancellationToken ct)
    {
        var t = await _db.InboxThreads.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return NotFound();
        t.Status = req.Status;
        t.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(t.ToDto());
    }
}

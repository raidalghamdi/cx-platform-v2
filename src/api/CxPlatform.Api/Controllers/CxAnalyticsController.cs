using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Application.Services;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// Gap 4 — CX KPI tracking & analysis. Snapshots are written daily by the
// aggregator service (stubbed in Subagent 1); root-cause links are
// hand-curated by quality + supervisors.
[ApiController]
[Authorize]
[Route("api/v1/cx-analytics")]
public class CxAnalyticsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICxAnalyticsAggregatorService _aggregator;
    public CxAnalyticsController(AppDbContext db, ICxAnalyticsAggregatorService aggregator)
    {
        _db = db; _aggregator = aggregator;
    }

    [HttpGet("snapshots")]
    public async Task<ActionResult<IReadOnlyList<CxAnalyticsSnapshotDto>>> Snapshots(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? segment,
        CancellationToken ct)
    {
        var q = _db.CxAnalyticsSnapshots.AsNoTracking().AsQueryable();
        if (from is not null) q = q.Where(s => s.SnapshotDate >= from);
        if (to is not null) q = q.Where(s => s.SnapshotDate <= to);
        if (!string.IsNullOrWhiteSpace(segment)) q = q.Where(s => s.Segment == segment);
        var rows = await q.OrderBy(s => s.SnapshotDate).Take(1000).Select(s => s.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("trend")]
    public async Task<ActionResult<CxAnalyticsTrendDto>> Trend(
        [FromQuery] string segment = "All",
        [FromQuery] int days = 90,
        CancellationToken ct = default)
    {
        days = Math.Clamp(days, 7, 365);
        var since = DateTime.UtcNow.Date.AddDays(-days);
        var rows = await _db.CxAnalyticsSnapshots.AsNoTracking()
            .Where(s => s.Segment == segment && s.SnapshotDate >= since)
            .OrderBy(s => s.SnapshotDate)
            .Select(s => s.ToDto())
            .ToListAsync(ct);
        return Ok(new CxAnalyticsTrendDto(segment, rows));
    }

    [HttpPost("snapshots/build")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<object>> BuildSnapshot([FromQuery] DateTime? date, CancellationToken ct)
    {
        var d = (date ?? DateTime.UtcNow).Date;
        var built = await _aggregator.BuildDailySnapshotsAsync(d, ct);
        return Ok(new { date = d, built });
    }

    [HttpGet("root-cause-links")]
    public async Task<ActionResult<IReadOnlyList<RootCauseLinkDto>>> Links(CancellationToken ct)
    {
        var rows = await _db.RootCauseLinks.AsNoTracking()
            .OrderByDescending(l => l.LinkStrength)
            .Select(l => l.ToDto())
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("root-cause-links")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<RootCauseLinkDto>> CreateLink([FromBody] CreateRootCauseLinkRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest();
        var row = new RootCauseLink
        {
            FromType = req.FromType ?? "", FromRefId = req.FromRefId,
            ToType = req.ToType ?? "", ToRefId = req.ToRefId,
            LinkStrength = Math.Clamp(req.LinkStrength, 0m, 1m),
            Notes = req.Notes ?? "",
        };
        _db.RootCauseLinks.Add(row);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Links), null, row.ToDto());
    }
}

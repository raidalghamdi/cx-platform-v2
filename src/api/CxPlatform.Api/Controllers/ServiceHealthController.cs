using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Application.Services;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// Gap 2 — Service performance & stability. Reads come from
// service_health_metrics (filled by SyntheticCheckRunner — stubbed in
// Subagent 1, real in Subagent 2); incidents are user-authored.
[ApiController]
[Authorize]
[Route("api/v1/service-health")]
public class ServiceHealthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISyntheticCheckRunner _checks;
    public ServiceHealthController(AppDbContext db, ISyntheticCheckRunner checks)
    {
        _db = db; _checks = checks;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<IReadOnlyList<ServiceHealthMetricDto>>> Metrics(
        [FromQuery] string? service,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var q = _db.ServiceHealthMetrics.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(service)) q = q.Where(m => m.ServiceName == service);
        if (from is not null) q = q.Where(m => m.MeasuredAt >= from);
        if (to is not null) q = q.Where(m => m.MeasuredAt <= to);
        var rows = await q.OrderByDescending(m => m.MeasuredAt).Take(2000)
            .Select(m => m.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("incidents")]
    public async Task<ActionResult<IReadOnlyList<ServiceIncidentDto>>> Incidents(CancellationToken ct)
    {
        var rows = await _db.ServiceIncidents.AsNoTracking()
            .OrderByDescending(i => i.OpenedAt)
            .Select(i => i.ToDto())
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("incidents")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<ServiceIncidentDto>> CreateIncident([FromBody] CreateServiceIncidentRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.ServiceName) || string.IsNullOrWhiteSpace(req.TitleEn))
            return BadRequest(new { error = "service and title are required" });
        var sev = Enum.TryParse<IncidentSeverity>(req.Severity, true, out var s) ? s : IncidentSeverity.Sev3;
        var row = new ServiceIncident
        {
            ServiceName = req.ServiceName,
            Severity = sev,
            TitleEn = req.TitleEn, TitleAr = req.TitleAr ?? "",
            RootCauseEn = req.RootCauseEn ?? "", RootCauseAr = req.RootCauseAr ?? "",
            Status = IncidentStatus.Open,
        };
        _db.ServiceIncidents.Add(row);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Incidents), null, row.ToDto());
    }

    [HttpPut("incidents/{id:long}")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<ServiceIncidentDto>> UpdateIncident(long id, [FromBody] UpdateServiceIncidentRequest req, CancellationToken ct)
    {
        var row = await _db.ServiceIncidents.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (row is null) return NotFound();
        if (Enum.TryParse<IncidentStatus>(req.Status, true, out var st)) row.Status = st;
        if (req.RemediationEn is not null) row.RemediationEn = req.RemediationEn;
        if (req.RemediationAr is not null) row.RemediationAr = req.RemediationAr;
        if (row.Status == IncidentStatus.Resolved && row.ResolvedAt is null)
            row.ResolvedAt = DateTime.UtcNow;
        if (row.Status != IncidentStatus.Resolved) row.ResolvedAt = null;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    [HttpGet("synthetic-checks")]
    public async Task<ActionResult<IReadOnlyList<SyntheticCheckDto>>> Checks(CancellationToken ct)
    {
        var rows = await _db.SyntheticChecks.AsNoTracking()
            .OrderBy(c => c.Name).Select(c => c.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPut("synthetic-checks/{id:long}/enabled")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<SyntheticCheckDto>> ToggleCheck(long id, [FromBody] ToggleSyntheticCheckRequest req, CancellationToken ct)
    {
        var row = await _db.SyntheticChecks.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return NotFound();
        row.Enabled = req.Enabled;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    // Manual one-shot trigger so the UI can ask the runner to refresh.
    // Stub returns 0; Subagent 2's real runner reports how many checks fired.
    [HttpPost("synthetic-checks/run")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<object>> RunNow(CancellationToken ct)
        => Ok(new { ran = await _checks.RunOnceAsync(ct) });
}

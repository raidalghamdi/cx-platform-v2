using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/automation")]
public class AutomationController : ControllerBase
{
    private static readonly Random Rng = new();
    private readonly AppDbContext _db;
    public AutomationController(AppDbContext db) { _db = db; }

    [HttpGet("rules")]
    public async Task<ActionResult<IReadOnlyList<AutomationRuleDto>>> List(CancellationToken ct)
    {
        var rows = await _db.AutomationRules.AsNoTracking()
            .OrderByDescending(r => r.Enabled).ThenBy(r => r.NameEn)
            .Select(r => r.ToDto())
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPut("rules/{id:long}/enabled")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<AutomationRuleDto>> Toggle(long id, [FromBody] ToggleAutomationRequest req, CancellationToken ct)
    {
        var row = await _db.AutomationRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row is null) return NotFound();
        row.Enabled = req.Enabled;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    // POST /api/v1/automation/rules/{id}/run — mocked manual run with the
    // same 800-1500ms / 95% pattern as the channel adapters.
    [HttpPost("rules/{id:long}/run")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<AutomationRunResultDto>> Run(long id, CancellationToken ct)
    {
        var row = await _db.AutomationRules.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row is null) return NotFound();
        if (!row.Enabled) return BadRequest(new { error = "rule is disabled" });

        int delay; bool ok;
        lock (Rng) { delay = Rng.Next(800, 1501); ok = Rng.NextDouble() < 0.95; }
        await Task.Delay(delay, ct);

        row.LastRunAt = DateTime.UtcNow;
        row.LastRunStatus = ok ? "success" : "failure";
        row.RunCount += 1;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Ok(new AutomationRunResultDto(ok, row.LastRunStatus, delay,
            ok ? "Action dispatched (mock)." : "Mock action failed — try again."));
    }
}

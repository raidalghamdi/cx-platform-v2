using System.Security.Claims;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Application.Services;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kpis")]
public class KpisController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IThresholdEvaluationService _thresholds;
    public KpisController(AppDbContext db, IThresholdEvaluationService thresholds)
    {
        _db = db; _thresholds = thresholds;
    }

    // RoleScope = "all" or pipe-joined roles (e.g. "executive|admin"). Phase 0
    // seeds everything as "all", but the filter is in place for later phases.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KpiDto>>> List(CancellationToken ct)
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        var rows = await _db.Kpis.AsNoTracking()
            .Where(k => k.RoleScope == "all" || k.RoleScope.Contains(role))
            .OrderBy(k => k.Key)
            .ToListAsync(ct);
        return Ok(rows.Select(k => k.ToDto()).ToList());
    }

    // Round 5 hook: writing a new KPI value triggers the threshold evaluator
    // so any breaching threshold spins up a PDCA improvement item and/or
    // a notification. Returns the new KPI plus any items that were created.
    public sealed record UpdateKpiValueRequest(decimal Value, decimal? Delta);

    [HttpPut("{id:long}/value")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<object>> UpdateValue(
        long id,
        [FromBody] UpdateKpiValueRequest req,
        CancellationToken ct)
    {
        var k = await _db.Kpis.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (k is null) return NotFound();

        k.Value = req.Value;
        if (req.Delta is not null) k.Delta = req.Delta.Value;
        k.LastSyncAt = DateTime.UtcNow;
        k.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var actorId = long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub"), out var uid) ? uid : (long?)null;
        var triggered = await _thresholds.EvaluateAsync(id, req.Value, actorId, ct);

        return Ok(new
        {
            kpi = k.ToDto(),
            triggered,
        });
    }
}

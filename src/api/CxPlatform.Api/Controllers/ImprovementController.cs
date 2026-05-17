using System.Security.Claims;
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

// Gap 3 — KPI-driven continuous-improvement governance. PDCA transitions
// route through IPdcaTransitionService so Subagent 2 can layer in the
// legal-transition rules and notifications without touching the controller.
[ApiController]
[Authorize]
[Route("api/v1/improvement")]
public class ImprovementController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPdcaTransitionService _pdca;
    public ImprovementController(AppDbContext db, IPdcaTransitionService pdca)
    {
        _db = db; _pdca = pdca;
    }

    [HttpGet("items")]
    public async Task<ActionResult<IReadOnlyList<ImprovementItemDto>>> List(
        [FromQuery] string? stage,
        [FromQuery] string? source,
        [FromQuery] string? priority,
        CancellationToken ct)
    {
        var q = _db.ImprovementItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(stage) && Enum.TryParse<PdcaStage>(stage, true, out var st))
            q = q.Where(i => i.PdcaStage == st);
        if (!string.IsNullOrWhiteSpace(source) && Enum.TryParse<ImprovementSource>(source, true, out var sr))
            q = q.Where(i => i.SourceType == sr);
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<ImprovementPriority>(priority, true, out var pr))
            q = q.Where(i => i.Priority == pr);
        var rows = await q.OrderByDescending(i => i.CreatedAt).Select(i => i.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("items/{id:long}")]
    public async Task<ActionResult<ImprovementItemDetailDto>> Get(long id, CancellationToken ct)
    {
        var item = await _db.ImprovementItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null) return NotFound();
        var log = await _db.PdcaCycleLogs.AsNoTracking()
            .Where(l => l.ImprovementItemId == id)
            .OrderBy(l => l.ChangedAt)
            .Select(l => l.ToDto())
            .ToListAsync(ct);
        return Ok(new ImprovementItemDetailDto(item.ToDto(), log));
    }

    [HttpPost("items")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<ImprovementItemDto>> Create([FromBody] CreateImprovementItemRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.TitleEn) || string.IsNullOrWhiteSpace(req.TitleAr))
            return BadRequest(new { error = "title_en and title_ar are required" });
        var src = Enum.TryParse<ImprovementSource>(req.SourceType, true, out var s) ? s : ImprovementSource.Manual;
        var pr  = Enum.TryParse<ImprovementPriority>(req.Priority, true, out var p) ? p : ImprovementPriority.Medium;
        var row = new ImprovementItem
        {
            SourceType = src, SourceRefId = req.SourceRefId,
            TitleEn = req.TitleEn, TitleAr = req.TitleAr,
            DescriptionEn = req.DescriptionEn ?? "", DescriptionAr = req.DescriptionAr ?? "",
            Owner = req.Owner ?? "", Priority = pr,
            PdcaStage = PdcaStage.Plan, TargetDate = req.TargetDate,
        };
        _db.ImprovementItems.Add(row);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row.ToDto());
    }

    // PATCH /api/v1/improvement/items/{id}/transition — uses the service so
    // legal-transition rules (Subagent 2) apply uniformly.
    [HttpPost("items/{id:long}/transition")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<ImprovementItemDetailDto>> Transition(long id, [FromBody] TransitionPdcaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<PdcaStage>(req.ToStage, true, out var to))
            return BadRequest(new { error = "unknown stage" });
        var uid = CurrentUserId();
        var res = await _pdca.TransitionAsync(id, to, uid, req.NotesEn ?? "", req.NotesAr ?? "", ct);
        if (!res.Ok) return BadRequest(new { error = res.Error });
        return await Get(id, ct);
    }

    // ── KPI thresholds (used by Subagent 2's threshold evaluator) ──────────
    [HttpGet("kpi-thresholds")]
    public async Task<ActionResult<IReadOnlyList<KpiThresholdDto>>> Thresholds(CancellationToken ct)
    {
        var rows = await (from t in _db.KpiThresholds.AsNoTracking()
                          join k in _db.Kpis.AsNoTracking() on t.KpiId equals k.Id
                          orderby k.Key
                          select new { t, k.Key }).ToListAsync(ct);
        return Ok(rows.Select(r => r.t.ToDto(r.Key)).ToList());
    }

    [HttpPut("kpi-thresholds/{id:long}")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<KpiThresholdDto>> UpdateThreshold(long id, [FromBody] UpdateKpiThresholdRequest req, CancellationToken ct)
    {
        var row = await _db.KpiThresholds.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (row is null) return NotFound();
        row.ThresholdValue = req.ThresholdValue;
        if (Enum.TryParse<ThresholdComparison>(req.ComparisonOp, true, out var op)) row.ComparisonOp = op;
        if (Enum.TryParse<ThresholdBreachAction>(req.BreachAction, true, out var ba)) row.BreachAction = ba;
        row.Enabled = req.Enabled;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        var key = await _db.Kpis.Where(k => k.Id == row.KpiId).Select(k => k.Key).FirstOrDefaultAsync(ct) ?? "";
        return Ok(row.ToDto(key));
    }

    private long? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var v) ? v : null;
    }
}

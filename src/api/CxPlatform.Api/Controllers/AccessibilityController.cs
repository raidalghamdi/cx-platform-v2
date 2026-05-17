using System.Security.Claims;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// Gap 1 — Digital accessibility & universal access. Reviewers see audits +
// remediation items; admin/quality/supervisor can record new audits and
// update remediation items.
[ApiController]
[Authorize]
[Route("api/v1/accessibility")]
public class AccessibilityController : ControllerBase
{
    private readonly AppDbContext _db;
    public AccessibilityController(AppDbContext db) { _db = db; }

    [HttpGet("audits")]
    public async Task<ActionResult<IReadOnlyList<AccessibilityAuditDto>>> List(CancellationToken ct)
    {
        var rows = await _db.AccessibilityAudits.AsNoTracking()
            .OrderByDescending(a => a.AuditDate)
            .ToListAsync(ct);
        return Ok(rows.Select(a => a.ToDto()).ToList());
    }

    [HttpGet("audits/{id:long}")]
    public async Task<ActionResult<AccessibilityAuditDetailDto>> Get(long id, CancellationToken ct)
    {
        var audit = await _db.AccessibilityAudits.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (audit is null) return NotFound();
        var items = await _db.AccessibilityRemediations.AsNoTracking()
            .Where(r => r.AuditId == id)
            .OrderBy(r => r.Severity).ThenBy(r => r.WcagCriterion)
            .Select(r => r.ToDto())
            .ToListAsync(ct);
        return Ok(new AccessibilityAuditDetailDto(audit.ToDto(), items));
    }

    [HttpPost("audits")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<AccessibilityAuditDto>> Create([FromBody] CreateAccessibilityAuditRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Auditor))
            return BadRequest(new { error = "auditor is required" });
        var level = Enum.TryParse<WcagLevel>(req.WcagLevel, true, out var w) ? w : WcagLevel.AA;
        var audit = new AccessibilityAuditEntry
        {
            Auditor = req.Auditor,
            ScopePagesJson = System.Text.Json.JsonSerializer.Serialize(req.ScopePages ?? Array.Empty<string>()),
            WcagLevel = level,
            ReportUrl = req.ReportUrl,
            Notes = req.Notes ?? "",
        };
        _db.AccessibilityAudits.Add(audit);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = audit.Id }, audit.ToDto());
    }

    [HttpPut("remediations/{id:long}")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<AccessibilityRemediationDto>> UpdateRemediation(long id, [FromBody] UpdateAccessibilityRemediationRequest req, CancellationToken ct)
    {
        var row = await _db.AccessibilityRemediations.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row is null) return NotFound();
        if (Enum.TryParse<AccessibilityItemStatus>(req.Status, true, out var s)) row.Status = s;
        row.TargetDate = req.TargetDate;
        if (!string.IsNullOrWhiteSpace(req.Owner)) row.Owner = req.Owner;
        if (row.Status == AccessibilityItemStatus.Resolved && row.ResolvedDate is null)
            row.ResolvedDate = DateTime.UtcNow;
        if (row.Status != AccessibilityItemStatus.Resolved)
            row.ResolvedDate = null;
        row.UpdatedAt = DateTime.UtcNow;
        // Recalculate OpenIssues on the parent audit so the table tile stays consistent.
        var audit = await _db.AccessibilityAudits.FirstOrDefaultAsync(a => a.Id == row.AuditId, ct);
        if (audit is not null)
        {
            audit.OpenIssues = await _db.AccessibilityRemediations
                .CountAsync(r => r.AuditId == audit.Id && r.Status != AccessibilityItemStatus.Resolved, ct);
            audit.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    // Static WCAG 2.2 reference subset (success criteria most relevant to
    // public-sector portals). Lives in code so the SPA can render a help
    // pane without an extra round-trip and without a DB lookup.
    private static readonly WcagCriterionDto[] Criteria = new[]
    {
        new WcagCriterionDto("1.1.1", "Non-text Content", "A",  "Perceivable"),
        new WcagCriterionDto("1.3.1", "Info and Relationships", "A",  "Perceivable"),
        new WcagCriterionDto("1.4.3", "Contrast (Minimum)", "AA", "Perceivable"),
        new WcagCriterionDto("1.4.11","Non-text Contrast", "AA", "Perceivable"),
        new WcagCriterionDto("2.1.1", "Keyboard", "A", "Operable"),
        new WcagCriterionDto("2.4.7", "Focus Visible", "AA", "Operable"),
        new WcagCriterionDto("2.5.8", "Target Size (Minimum)", "AA", "Operable"),
        new WcagCriterionDto("3.1.2", "Language of Parts", "AA", "Understandable"),
        new WcagCriterionDto("3.3.7", "Redundant Entry", "A", "Understandable"),
        new WcagCriterionDto("4.1.2", "Name, Role, Value", "A", "Robust"),
        new WcagCriterionDto("4.1.3", "Status Messages", "AA", "Robust"),
    };

    [HttpGet("wcag-criteria")]
    public ActionResult<IReadOnlyList<WcagCriterionDto>> WcagCriteria() => Ok(Criteria);
}

using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/governance")]
public class GovernanceController : ControllerBase
{
    private readonly AppDbContext _db;
    public GovernanceController(AppDbContext db) { _db = db; }

    [HttpGet("bodies")]
    public async Task<ActionResult<IReadOnlyList<GovernanceBodyDto>>> ListBodies(CancellationToken ct)
    {
        var rows = await _db.GovernanceBodies.AsNoTracking().OrderBy(b => b.NameEn).ToListAsync(ct);
        return Ok(rows.Select(b => b.ToDto()).ToList());
    }

    [HttpGet("bodies/{id:long}")]
    public async Task<ActionResult<GovernanceBodyDetailDto>> GetBody(long id, CancellationToken ct)
    {
        var body = await _db.GovernanceBodies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (body is null) return NotFound();
        var decisions = await _db.GovernanceDecisions.AsNoTracking()
            .Where(d => d.BodyId == id)
            .OrderByDescending(d => d.DecidedAt)
            .Select(d => d.ToDto())
            .ToListAsync(ct);
        return Ok(new GovernanceBodyDetailDto(body.ToDto(), decisions));
    }

    [HttpPost("bodies/{bodyId:long}/decisions")]
    [Authorize(Roles = "admin,supervisor,quality,executive")]
    public async Task<ActionResult<GovernanceDecisionDto>> CreateDecision(long bodyId, [FromBody] CreateGovernanceDecisionRequest req, CancellationToken ct)
    {
        var body = await _db.GovernanceBodies.FirstOrDefaultAsync(x => x.Id == bodyId, ct);
        if (body is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.TitleEn) || string.IsNullOrWhiteSpace(req.TitleAr))
            return BadRequest(new { error = "title_en and title_ar are required" });
        var row = new GovernanceDecision {
            BodyId = bodyId,
            TitleEn = req.TitleEn, TitleAr = req.TitleAr,
            Decision = req.Decision ?? "",
            OwnerEn = req.OwnerEn ?? "", OwnerAr = req.OwnerAr ?? "",
            DueDate = req.DueDate,
            DecidedAt = DateTime.UtcNow,
        };
        _db.GovernanceDecisions.Add(row);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetBody), new { id = bodyId }, row.ToDto());
    }
}

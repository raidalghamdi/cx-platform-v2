using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// Customer journeys + stages. Reads are open to any authenticated user with
// /journeys access — page-level filtering happens in the route-guard. Writes
// are gated to admin + supervisor here so we never depend on the SPA guard
// for security.

[ApiController]
[Authorize]
[Route("api/v1/journeys")]
public class JourneysController : ControllerBase
{
    private readonly AppDbContext _db;
    public JourneysController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<JourneyDto>>> List(CancellationToken ct)
    {
        var rows = await _db.Journeys.AsNoTracking()
            .OrderByDescending(j => j.CreatedAt)
            .Select(j => j.ToDto())
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<JourneyDetailDto>> Get(long id, CancellationToken ct)
    {
        var j = await _db.Journeys.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (j is null) return NotFound();
        var stages = await _db.JourneyStages.AsNoTracking()
            .Where(s => s.JourneyId == id)
            .OrderBy(s => s.Sequence)
            .Select(s => s.ToDto())
            .ToListAsync(ct);
        return Ok(new JourneyDetailDto(j.ToDto(), stages));
    }

    [HttpPost]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<JourneyDetailDto>> Create([FromBody] UpsertJourneyRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.NameEn) || string.IsNullOrWhiteSpace(req.NameAr))
            return BadRequest(new { error = "name_en and name_ar are required" });
        var journey = new Journey {
            NameEn = req.NameEn, NameAr = req.NameAr,
            Persona = req.Persona ?? "", Status = req.Status ?? "active",
            StageCount = req.Stages?.Count ?? 0,
        };
        _db.Journeys.Add(journey);
        await _db.SaveChangesAsync(ct);
        if (req.Stages is { Count: > 0 })
        {
            foreach (var s in req.Stages)
                _db.JourneyStages.Add(MapStage(s, journey.Id));
            await _db.SaveChangesAsync(ct);
        }
        return await Get(journey.Id, ct);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<ActionResult<JourneyDetailDto>> Update(long id, [FromBody] UpsertJourneyRequest req, CancellationToken ct)
    {
        var j = await _db.Journeys.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (j is null) return NotFound();
        j.NameEn = req.NameEn; j.NameAr = req.NameAr;
        j.Persona = req.Persona ?? ""; j.Status = req.Status ?? j.Status;
        j.StageCount = req.Stages?.Count ?? j.StageCount;
        j.UpdatedAt = DateTime.UtcNow;
        // Replace stage set wholesale — simpler than diffing for Phase 1.
        var existing = await _db.JourneyStages.Where(s => s.JourneyId == id).ToListAsync(ct);
        _db.JourneyStages.RemoveRange(existing);
        if (req.Stages is { Count: > 0 })
            foreach (var s in req.Stages)
                _db.JourneyStages.Add(MapStage(s, id));
        await _db.SaveChangesAsync(ct);
        return await Get(id, ct);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var j = await _db.Journeys.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (j is null) return NotFound();
        var stages = await _db.JourneyStages.Where(s => s.JourneyId == id).ToListAsync(ct);
        _db.JourneyStages.RemoveRange(stages);
        _db.Journeys.Remove(j);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static JourneyStage MapStage(UpsertJourneyStage s, long journeyId) => new()
    {
        JourneyId = journeyId,
        Sequence = s.Sequence,
        NameEn = s.NameEn ?? "", NameAr = s.NameAr ?? "",
        TouchpointEn = s.TouchpointEn ?? "", TouchpointAr = s.TouchpointAr ?? "",
        PainPointEn = s.PainPointEn ?? "", PainPointAr = s.PainPointAr ?? "",
        EmotionScore = Math.Clamp(s.EmotionScore, -2, 2),
    };
}

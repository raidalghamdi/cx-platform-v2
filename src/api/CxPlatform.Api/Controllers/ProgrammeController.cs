using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/programme")]
public class ProgrammeController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProgrammeController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProgrammeInitiativeDto>>> List(CancellationToken ct)
    {
        var rows = await _db.ProgrammeInitiatives.AsNoTracking()
            .OrderBy(p => p.TargetDate)
            .Select(p => p.ToDto())
            .ToListAsync(ct);
        return Ok(rows);
    }

    // PUT /api/v1/programme/{id}/status — updates RAG colour + % progress + notes.
    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "admin,supervisor,executive")]
    public async Task<ActionResult<ProgrammeInitiativeDto>> UpdateStatus(long id, [FromBody] UpdateProgrammeStatusRequest req, CancellationToken ct)
    {
        var row = await _db.ProgrammeInitiatives.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return NotFound();
        row.RagStatus = NormaliseRag(req.RagStatus);
        row.ProgressPct = Math.Clamp(req.ProgressPct, 0, 100);
        if (req.Notes is not null) row.Notes = req.Notes;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    private static string NormaliseRag(string s) =>
        s?.ToLowerInvariant() switch
        {
            "red"   => "red",
            "green" => "green",
            _       => "amber",
        };
}

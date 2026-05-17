using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// About-page sections. Reads are open to any authenticated user; only admin
// can edit body / order / titles.
[ApiController]
[Authorize]
[Route("api/v1/about")]
public class AboutController : ControllerBase
{
    private readonly AppDbContext _db;
    public AboutController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AboutSectionDto>>> List(CancellationToken ct)
    {
        var rows = await _db.AboutSections.AsNoTracking()
            .OrderBy(s => s.OrderIndex)
            .Select(s => s.ToDto())
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<AboutSectionDto>> Update(long id, [FromBody] UpdateAboutSectionRequest req, CancellationToken ct)
    {
        var row = await _db.AboutSections.FirstOrDefaultAsync(s => s.Id == id, ct);
        if (row is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.KeyEn) || string.IsNullOrWhiteSpace(req.KeyAr))
            return BadRequest(new { error = "key_en and key_ar are required" });
        row.KeyEn = req.KeyEn;
        row.KeyAr = req.KeyAr;
        row.BodyEn = req.BodyEn ?? "";
        row.BodyAr = req.BodyAr ?? "";
        row.OrderIndex = req.OrderIndex;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }
}

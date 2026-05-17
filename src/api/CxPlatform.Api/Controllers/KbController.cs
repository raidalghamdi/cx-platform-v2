using System.Security.Claims;
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
[Route("api/v1/kb")]
public class KbController : ControllerBase
{
    private readonly AppDbContext _db;
    public KbController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KbArticleDto>>> List(
        [FromQuery] string? category,
        [FromQuery] string? q,
        CancellationToken ct)
    {
        var query = _db.KbArticles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(a => a.Category == category);
        if (!string.IsNullOrWhiteSpace(q))
        {
            // Single LIKE across both languages keeps the SPA search simple.
            var like = $"%{q}%";
            query = query.Where(a => EF.Functions.Like(a.TitleEn, like) || EF.Functions.Like(a.TitleAr, like)
                                  || EF.Functions.Like(a.BodyEn, like)  || EF.Functions.Like(a.BodyAr, like));
        }
        var rows = await query.OrderByDescending(a => a.UpdatedAt).Select(a => a.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<KbArticleDto>> Get(long id, CancellationToken ct)
    {
        var a = await _db.KbArticles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return a is null ? NotFound() : Ok(a.ToDto());
    }

    // KB editors are admin + supervisor + agent — matches the brief's
    // ✓-write rows. Quality/customer/executive are read-only via [Authorize].
    [HttpPost]
    [Authorize(Roles = "admin,supervisor,agent")]
    public async Task<ActionResult<KbArticleDto>> Create([FromBody] UpsertKbArticleRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.TitleEn) || string.IsNullOrWhiteSpace(req.TitleAr))
            return BadRequest();
        var a = new KbArticle {
            TitleEn = req.TitleEn, TitleAr = req.TitleAr,
            Category = req.Category ?? "",
            BodyEn = req.BodyEn ?? "", BodyAr = req.BodyAr ?? "",
            Status = string.IsNullOrWhiteSpace(req.Status) ? "draft" : req.Status,
            AuthorId = CurrentUserId(),
            UpdatedAt = DateTime.UtcNow,
        };
        _db.KbArticles.Add(a);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = a.Id }, a.ToDto());
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin,supervisor,agent")]
    public async Task<ActionResult<KbArticleDto>> Update(long id, [FromBody] UpsertKbArticleRequest req, CancellationToken ct)
    {
        var a = await _db.KbArticles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        a.TitleEn = req.TitleEn ?? a.TitleEn;
        a.TitleAr = req.TitleAr ?? a.TitleAr;
        a.Category = req.Category ?? a.Category;
        a.BodyEn = req.BodyEn ?? a.BodyEn;
        a.BodyAr = req.BodyAr ?? a.BodyAr;
        if (!string.IsNullOrWhiteSpace(req.Status)) a.Status = req.Status;
        a.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(a.ToDto());
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin,supervisor")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var a = await _db.KbArticles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return NotFound();
        _db.KbArticles.Remove(a);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private long? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var v) ? v : null;
    }
}

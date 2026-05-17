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

// Gap 5 — Content, knowledge & digital-channels management. Review cycles
// live in their own table referencing kb_articles; freshness scores are
// recomputed by ContentFreshnessService (stubbed in Subagent 1).
[ApiController]
[Authorize]
[Route("api/v1/content-governance")]
public class ContentGovernanceController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IContentFreshnessService _freshness;
    public ContentGovernanceController(AppDbContext db, IContentFreshnessService freshness)
    {
        _db = db; _freshness = freshness;
    }

    [HttpGet("review-cycles")]
    public async Task<ActionResult<IReadOnlyList<ContentReviewCycleDto>>> ReviewCycles(CancellationToken ct)
    {
        var rows = await (from c in _db.ContentReviewCycles.AsNoTracking()
                          join a in _db.KbArticles.AsNoTracking() on c.KbArticleId equals a.Id into joined
                          from a in joined.DefaultIfEmpty()
                          orderby c.DueDate
                          select new { c, en = a != null ? a.TitleEn : "", ar = a != null ? a.TitleAr : "" }).ToListAsync(ct);
        return Ok(rows.Select(r => r.c.ToDto(r.en, r.ar)).ToList());
    }

    [HttpPost("review-cycles")]
    [Authorize(Roles = "admin,supervisor,quality,agent")]
    public async Task<ActionResult<ContentReviewCycleDto>> Create([FromBody] CreateContentReviewCycleRequest req, CancellationToken ct)
    {
        if (req is null || req.KbArticleId <= 0) return BadRequest();
        var article = await _db.KbArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == req.KbArticleId, ct);
        if (article is null) return NotFound(new { error = "article not found" });
        var row = new ContentReviewCycle
        {
            KbArticleId = req.KbArticleId,
            DueDate = req.DueDate.Date,
            AssignedReviewer = req.AssignedReviewer ?? "",
            Status = ContentReviewStatus.Pending,
            FreshnessScore = _freshness.Score(article.UpdatedAt, true),
        };
        _db.ContentReviewCycles.Add(row);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(ReviewCycles), null, row.ToDto(article.TitleEn, article.TitleAr));
    }

    [HttpPut("review-cycles/{id:long}")]
    [Authorize(Roles = "admin,supervisor,quality,agent")]
    public async Task<ActionResult<ContentReviewCycleDto>> Update(long id, [FromBody] UpdateContentReviewCycleRequest req, CancellationToken ct)
    {
        var row = await _db.ContentReviewCycles.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (row is null) return NotFound();
        if (Enum.TryParse<ContentReviewStatus>(req.Status, true, out var s)) row.Status = s;
        if (!string.IsNullOrWhiteSpace(req.AssignedReviewer)) row.AssignedReviewer = req.AssignedReviewer;
        if (req.Notes is not null) row.Notes = req.Notes;
        if (row.Status is ContentReviewStatus.Approved or ContentReviewStatus.Rejected && row.CompletedAt is null)
            row.CompletedAt = DateTime.UtcNow;
        if (row.Status is ContentReviewStatus.Pending or ContentReviewStatus.InReview)
            row.CompletedAt = null;
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        var article = await _db.KbArticles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == row.KbArticleId, ct);
        return Ok(row.ToDto(article?.TitleEn ?? "", article?.TitleAr ?? ""));
    }

    [HttpGet("channel-performance")]
    public async Task<ActionResult<IReadOnlyList<ChannelPerformanceMetricDto>>> ChannelPerformance(
        [FromQuery] string? channel,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var q = _db.ChannelPerformanceMetrics.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(channel)) q = q.Where(m => m.Channel == channel);
        if (from is not null) q = q.Where(m => m.MeasuredAt >= from);
        if (to is not null) q = q.Where(m => m.MeasuredAt <= to);
        var rows = await q.OrderByDescending(m => m.MeasuredAt).Take(2000)
            .Select(m => m.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    // Stale articles = either a low freshness score on an existing review
    // cycle, or no review cycle at all + UpdatedAt > 180 days. Reviewers
    // use the list to schedule the next batch.
    [HttpGet("stale-articles")]
    public async Task<ActionResult<IReadOnlyList<StaleArticleDto>>> Stale(CancellationToken ct)
    {
        var threshold = DateTime.UtcNow.AddDays(-180);
        // Latest-cycle freshness per article (left join keeps articles with no cycle yet).
        var latest = await (from a in _db.KbArticles.AsNoTracking()
                            join c in _db.ContentReviewCycles.AsNoTracking() on a.Id equals c.KbArticleId into cj
                            from c in cj.OrderByDescending(x => x.UpdatedAt).Take(1).DefaultIfEmpty()
                            select new {
                                a.Id, a.TitleEn, a.TitleAr, a.Category, a.UpdatedAt,
                                Score = c == null ? 0 : c.FreshnessScore,
                                Parity = c == null ? true : c.EnArParityFlag,
                            }).ToListAsync(ct);
        var stale = latest
            .Where(r => r.Score < 60 || !r.Parity || r.UpdatedAt < threshold)
            .OrderBy(r => r.Score).ThenBy(r => r.UpdatedAt)
            .Select(r => new StaleArticleDto(r.Id, r.TitleEn, r.TitleAr, r.Category, r.UpdatedAt, r.Score, r.Parity))
            .Take(200).ToList();
        return Ok(stale);
    }
}

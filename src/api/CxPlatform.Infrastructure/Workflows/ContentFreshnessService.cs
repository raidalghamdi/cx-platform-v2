using CxPlatform.Application.Services;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Infrastructure.Workflows;

// Round 5 Subagent 2 — freshness scoring + nightly recalc.
//
// Decay curve (per brief):
//   start at 100, subtract 1 point per 30 days since UpdatedAt, floor at 0.
//   If EN/AR parity is broken, subtract a flat 20-point penalty.
//   Result clamped to [0, 100].
public class ContentFreshnessService : IContentFreshnessService
{
    private readonly AppDbContext _db;
    public ContentFreshnessService(AppDbContext db) { _db = db; }

    public int Score(DateTime updatedAt, bool enArParityFlag)
    {
        var ageDays = (DateTime.UtcNow - updatedAt).TotalDays;
        if (ageDays < 0) ageDays = 0;            // article dated in the future — treat as fresh
        var decay = (int)Math.Floor(ageDays / 30.0);
        var raw = 100 - decay - (enArParityFlag ? 0 : 20);
        if (raw < 0)   raw = 0;
        if (raw > 100) raw = 100;
        return raw;
    }

    // RecalculateAllAsync — walk every ContentReviewCycle, look up the
    // linked article's UpdatedAt, and refresh FreshnessScore. Cheap at the
    // pilot row count; Subagent 1's seed is only single digits.
    public async Task RecalculateAllAsync(CancellationToken ct = default)
    {
        var cycles = await _db.ContentReviewCycles.ToListAsync(ct);
        if (cycles.Count == 0) return;

        var articleIds = cycles.Select(c => c.KbArticleId).Distinct().ToList();
        var articles = await _db.KbArticles
            .Where(a => articleIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.UpdatedAt, ct);

        var touched = 0;
        foreach (var c in cycles)
        {
            if (!articles.TryGetValue(c.KbArticleId, out var updatedAt)) continue;
            var fresh = Score(updatedAt, c.EnArParityFlag);
            if (c.FreshnessScore != fresh)
            {
                c.FreshnessScore = fresh;
                c.UpdatedAt = DateTime.UtcNow;
                touched++;
            }
        }
        if (touched > 0) await _db.SaveChangesAsync(ct);
    }
}

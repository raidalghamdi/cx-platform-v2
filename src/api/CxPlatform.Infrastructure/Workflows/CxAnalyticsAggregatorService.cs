using CxPlatform.Application.Services;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Infrastructure.Workflows;

// Round 5 Subagent 2 — daily CxAnalyticsSnapshot builder.
//
// Reads existing voc_responses + complaints tables (no schema changes to
// either) and writes one CxAnalyticsSnapshot per (date, segment).
// Idempotent — re-running for the same date updates the existing row.
public class CxAnalyticsAggregatorService : ICxAnalyticsAggregatorService
{
    private static readonly string[] Segments = { "All", "NewCustomer", "Returning", "VIP" };

    private readonly AppDbContext _db;
    public CxAnalyticsAggregatorService(AppDbContext db) { _db = db; }

    public async Task<int> BuildDailySnapshotsAsync(DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        var dayEnd = day.AddDays(1);

        // Pre-load the VoC + complaint windows once so we don't hit MySQL
        // per-segment. The pilot row counts are small enough that it's a
        // net win even if every snapshot ends up reading the same rows.
        var vocOfDay = await _db.VocResponses.AsNoTracking()
            .Where(v => v.RespondedAt >= day && v.RespondedAt < dayEnd)
            .Select(v => new { v.NpsScore, v.CustomerName })
            .ToListAsync(ct);
        var complaintsOpened = await _db.Complaints.AsNoTracking()
            .Where(c => c.OpenedAt >= day && c.OpenedAt < dayEnd)
            .Select(c => new { c.Id })
            .ToListAsync(ct);
        var complaintsClosed = await _db.Complaints.AsNoTracking()
            .Where(c => c.ClosedAt != null && c.ClosedAt >= day && c.ClosedAt < dayEnd)
            .Select(c => new { c.OpenedAt, c.ClosedAt })
            .ToListAsync(ct);

        var existing = await _db.CxAnalyticsSnapshots
            .Where(s => s.SnapshotDate == day && Segments.Contains(s.Segment))
            .ToDictionaryAsync(s => s.Segment, ct);

        int written = 0;
        foreach (var segment in Segments)
        {
            var voc = segment switch
            {
                // "All" — no filter. The other segments use the customer name
                // as a deterministic proxy until a proper segment column lands.
                "All"         => vocOfDay,
                "VIP"         => vocOfDay.Where(v => v.CustomerName?.Contains("VIP", StringComparison.OrdinalIgnoreCase) == true).ToList(),
                "NewCustomer" => vocOfDay.Where(v => HashSegment(v.CustomerName) == 0).ToList(),
                "Returning"   => vocOfDay.Where(v => HashSegment(v.CustomerName) == 1).ToList(),
                _             => vocOfDay,
            };

            var csat = ComputeCsat(voc.Select(v => v.NpsScore));
            var nps  = ComputeNps(voc.Select(v => v.NpsScore));
            var ces  = Math.Round(csat / 100m * 7m, 1);  // placeholder mapping 0-100 → 0-7
            var volume = complaintsOpened.Count;          // segment-level volume is the same — no per-segment field yet
            var resolutionP95Hours = ComputeP95Hours(
                complaintsClosed.Where(c => c.ClosedAt is not null)
                    .Select(c => (c.ClosedAt!.Value - c.OpenedAt).TotalHours));

            if (existing.TryGetValue(segment, out var row))
            {
                row.Csat = csat;
                row.Nps  = nps;
                row.Ces  = ces;
                row.ComplaintVolume = volume;
                row.ResolutionRateP95Hours = (decimal)resolutionP95Hours;
                row.JourneyId = null;
                row.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CxAnalyticsSnapshots.Add(new CxAnalyticsSnapshot
                {
                    SnapshotDate = day,
                    Segment = segment,
                    Csat = csat,
                    Nps  = nps,
                    Ces  = ces,
                    ComplaintVolume = volume,
                    ResolutionRateP95Hours = (decimal)resolutionP95Hours,
                    JourneyId = null,
                });
            }
            written++;
        }

        await _db.SaveChangesAsync(ct);
        return written;
    }

    // CSAT proxy from NPS 0-10:
    //   9-10 → 100, 7-8 → 80, 5-6 → 60, 0-4 → 40. Average across the window.
    private static decimal ComputeCsat(IEnumerable<int> npsScores)
    {
        var arr = npsScores.ToArray();
        if (arr.Length == 0) return 0m;
        var total = 0m;
        foreach (var n in arr)
            total += n >= 9 ? 100m : n >= 7 ? 80m : n >= 5 ? 60m : 40m;
        return Math.Round(total / arr.Length, 3);
    }

    // Standard NPS = (promoters - detractors) / total * 100. 0-10 scale.
    private static decimal ComputeNps(IEnumerable<int> npsScores)
    {
        var arr = npsScores.ToArray();
        if (arr.Length == 0) return 0m;
        var promoters  = arr.Count(n => n >= 9);
        var detractors = arr.Count(n => n <= 6);
        var nps = (decimal)(promoters - detractors) / arr.Length * 100m;
        return Math.Round(nps, 3);
    }

    // P95 over a small sample — sort + index.
    private static double ComputeP95Hours(IEnumerable<double> hours)
    {
        var arr = hours.Where(h => h > 0).OrderBy(h => h).ToArray();
        if (arr.Length == 0) return 0.0;
        var idx = (int)Math.Floor(0.95 * (arr.Length - 1));
        return Math.Round(arr[idx], 2);
    }

    // Deterministic segment proxy by name — 0/1 buckets.
    private static int HashSegment(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        var sum = 0;
        foreach (var c in name) sum = (sum + c) % 2;
        return sum;
    }
}

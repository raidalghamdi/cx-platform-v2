using CxPlatform.Application.Dtos;
using CxPlatform.Application.Services;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Infrastructure.Workflows;

// Round 5 Subagent 2 — KPI-driven improvement-item factory.
// Given a KPI's freshly-updated value, find every enabled KpiThreshold that
// is breaching and (per its BreachAction) either create an ImprovementItem
// in PdcaStage.Plan, send a Notification, or both. Idempotency for the demo
// is loose: we don't dedupe against an existing open item — a real release
// would, but Subagent 1's seed creates duplicates anyway so this matches.
public class ThresholdEvaluationService : IThresholdEvaluationService
{
    private readonly AppDbContext _db;
    public ThresholdEvaluationService(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ImprovementItemDto>> EvaluateAsync(
        long kpiId,
        decimal currentValue,
        long? actorUserId,
        CancellationToken ct = default)
    {
        var kpi = await _db.Kpis.FirstOrDefaultAsync(k => k.Id == kpiId, ct);
        if (kpi is null) return Array.Empty<ImprovementItemDto>();

        var thresholds = await _db.KpiThresholds
            .Where(t => t.KpiId == kpiId && t.Enabled)
            .ToListAsync(ct);
        if (thresholds.Count == 0) return Array.Empty<ImprovementItemDto>();

        // Resolve the default owner once — first supervisor, fall back to admin.
        var owner = await _db.Users
            .Where(u => u.Role == "supervisor")
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.NameEn, u.NameAr })
            .FirstOrDefaultAsync(ct);
        if (owner is null)
        {
            owner = await _db.Users
                .Where(u => u.Role == "admin")
                .OrderBy(u => u.Id)
                .Select(u => new { u.Id, u.NameEn, u.NameAr })
                .FirstOrDefaultAsync(ct);
        }
        long? ownerId = owner?.Id;
        string ownerLabel = owner?.NameEn ?? "Unassigned";

        var created = new List<ImprovementItem>();
        foreach (var t in thresholds)
        {
            if (!IsBreaching(t.ComparisonOp, currentValue, t.ThresholdValue)) continue;

            ImprovementItem? newItem = null;
            if (t.BreachAction is ThresholdBreachAction.CreateImprovementItem or ThresholdBreachAction.Both)
            {
                var priority = DerivePriority(currentValue, t.ThresholdValue);
                var opEn = t.ComparisonOp == ThresholdComparison.LessThan ? "below" : "above";
                var opAr = t.ComparisonOp == ThresholdComparison.LessThan ? "تحت" : "فوق";
                newItem = new ImprovementItem
                {
                    SourceType = ImprovementSource.KpiBreach,
                    SourceRefId = kpi.Id,
                    TitleEn = $"{kpi.NameEn} {opEn} threshold ({currentValue} vs {t.ThresholdValue})",
                    TitleAr = $"{kpi.NameAr} {opAr} العتبة ({currentValue} مقابل {t.ThresholdValue})",
                    DescriptionEn = $"Auto-created because {kpi.Key} hit {currentValue} (threshold {t.ComparisonOp} {t.ThresholdValue}). Investigate root cause and propose remediation.",
                    DescriptionAr = $"تم الإنشاء تلقائياً بعد بلوغ {kpi.Key} القيمة {currentValue} (العتبة {ArOp(t.ComparisonOp)} {t.ThresholdValue}). يلزم تحليل السبب الجذري واقتراح المعالجة.",
                    Owner = ownerLabel,
                    Priority = priority,
                    PdcaStage = PdcaStage.Plan,
                    TargetDate = DateTime.UtcNow.AddDays(priority == ImprovementPriority.Critical ? 7 :
                                                        priority == ImprovementPriority.High     ? 14 :
                                                        priority == ImprovementPriority.Medium   ? 30 : 60),
                };
                _db.ImprovementItems.Add(newItem);
                created.Add(newItem);
            }

            if (t.BreachAction is ThresholdBreachAction.NotifyOnly or ThresholdBreachAction.Both)
            {
                if (ownerId is not null)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = ownerId.Value,
                        TitleEn = $"KPI breach: {kpi.NameEn}",
                        TitleAr = $"تجاوز مؤشر: {kpi.NameAr}",
                        BodyEn = $"Current value {currentValue} {(t.ComparisonOp == ThresholdComparison.LessThan ? "<" : ">")} threshold {t.ThresholdValue}.",
                        BodyAr = $"القيمة الحالية {currentValue} {(t.ComparisonOp == ThresholdComparison.LessThan ? "أقل من" : "أكبر من")} العتبة {t.ThresholdValue}.",
                        Kind = "warning",
                    });
                }
            }
        }

        if (created.Count == 0 && !_db.ChangeTracker.HasChanges())
            return Array.Empty<ImprovementItemDto>();

        await _db.SaveChangesAsync(ct);
        return created.Select(ToDto).ToList();
    }

    // Local DTO projection — Infrastructure does not reference the Api
    // project, so the Mappers extensions aren't visible here. Keep this in
    // sync with CxPlatform.Api.Mappers.ToDto(ImprovementItem).
    private static ImprovementItemDto ToDto(ImprovementItem i) =>
        new(i.Id, i.SourceType.ToString(), i.SourceRefId, i.TitleEn, i.TitleAr,
            i.DescriptionEn, i.DescriptionAr, i.Owner, i.Priority.ToString(),
            i.PdcaStage.ToString(), i.CreatedAt, i.TargetDate, i.ClosedAt);

    private static bool IsBreaching(ThresholdComparison op, decimal current, decimal threshold) => op switch
    {
        ThresholdComparison.LessThan    => current < threshold,
        ThresholdComparison.GreaterThan => current > threshold,
        _ => false,
    };

    // Magnitude relative to the threshold drives priority. Symmetric for
    // either direction (less-than vs greater-than).
    private static ImprovementPriority DerivePriority(decimal current, decimal threshold)
    {
        if (threshold == 0m) return ImprovementPriority.Medium;
        var deltaPct = Math.Abs(current - threshold) / Math.Abs(threshold) * 100m;
        return deltaPct switch
        {
            > 20m => ImprovementPriority.Critical,
            > 10m => ImprovementPriority.High,
            > 5m  => ImprovementPriority.Medium,
            _     => ImprovementPriority.Low,
        };
    }

    private static string ArOp(ThresholdComparison op) =>
        op == ThresholdComparison.LessThan ? "أقل من" : "أكبر من";
}

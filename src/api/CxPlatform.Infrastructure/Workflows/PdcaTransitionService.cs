using CxPlatform.Application.Services;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Infrastructure.Workflows;

// Round 5 Subagent 2 — real PDCA enforcement.
// Legal transitions: Plan → Do → Check → Act → Closed, plus the two
// escape hatches the team agreed on at the kanban review:
//   • Plan → Closed   (cancel during planning)
//   • any → Plan      (re-open / restart the cycle)
// Everything else is rejected with a bilingual PdcaTransitionException
// that the controller turns into a 400 with ProblemDetails.
public class PdcaTransitionService : IPdcaTransitionService
{
    private readonly AppDbContext _db;
    public PdcaTransitionService(AppDbContext db) { _db = db; }

    // forward[from] = stages we allow as a direct next step.
    private static readonly Dictionary<PdcaStage, HashSet<PdcaStage>> Forward = new()
    {
        [PdcaStage.Plan]   = new() { PdcaStage.Do, PdcaStage.Closed },
        [PdcaStage.Do]     = new() { PdcaStage.Check },
        [PdcaStage.Check]  = new() { PdcaStage.Act },
        [PdcaStage.Act]    = new() { PdcaStage.Closed },
        [PdcaStage.Closed] = new(),
    };

    public async Task<PdcaTransitionResult> TransitionAsync(
        long improvementItemId,
        PdcaStage toStage,
        long? actorUserId,
        string notesEn,
        string notesAr,
        CancellationToken ct = default)
    {
        var item = await _db.ImprovementItems.FirstOrDefaultAsync(x => x.Id == improvementItemId, ct);
        if (item is null)
        {
            throw new PdcaTransitionException(
                $"Improvement item #{improvementItemId} not found.",
                $"بند التحسين رقم {improvementItemId} غير موجود.");
        }

        var from = item.PdcaStage;

        // No-op transition — silently accept so the SPA can be lazy about
        // disabling buttons. We still log it for traceability.
        if (from == toStage)
        {
            await WriteLogAsync(item.Id, from, toStage, actorUserId, notesEn, notesAr, ct);
            return new PdcaTransitionResult(true, null, from, toStage);
        }

        // Re-open: any → Plan. Clears ClosedAt if previously closed.
        if (toStage == PdcaStage.Plan)
        {
            ApplyTransition(item, toStage);
            await WriteLogAsync(item.Id, from, toStage, actorUserId, notesEn, notesAr, ct);
            return new PdcaTransitionResult(true, null, from, toStage);
        }

        // Forward path through the cycle (plus Plan→Closed cancellation).
        if (!Forward.TryGetValue(from, out var allowed) || !allowed.Contains(toStage))
        {
            throw new PdcaTransitionException(
                $"Illegal PDCA transition {from} → {toStage}. Allowed next steps: {string.Join(", ", allowed ?? new HashSet<PdcaStage>())} (or back to Plan to re-open).",
                $"انتقال غير مسموح في دورة PDCA من {ArStage(from)} إلى {ArStage(toStage)}. الخطوات المتاحة: {string.Join("، ", (allowed ?? new HashSet<PdcaStage>()).Select(ArStage))} (أو الرجوع إلى «تخطيط» لإعادة الفتح).");
        }

        ApplyTransition(item, toStage);
        await WriteLogAsync(item.Id, from, toStage, actorUserId, notesEn, notesAr, ct);
        return new PdcaTransitionResult(true, null, from, toStage);
    }

    private void ApplyTransition(ImprovementItem item, PdcaStage toStage)
    {
        var wasClosed = item.PdcaStage == PdcaStage.Closed;
        item.PdcaStage = toStage;
        item.UpdatedAt = DateTime.UtcNow;
        // Stamp/clear ClosedAt so report queries don't have to inspect the log.
        if (toStage == PdcaStage.Closed) item.ClosedAt = DateTime.UtcNow;
        else if (wasClosed)              item.ClosedAt = null;
    }

    private async Task WriteLogAsync(
        long itemId, PdcaStage from, PdcaStage to, long? actorUserId,
        string notesEn, string notesAr, CancellationToken ct)
    {
        _db.PdcaCycleLogs.Add(new PdcaCycleLog
        {
            ImprovementItemId = itemId,
            FromStage = from, ToStage = to,
            ActorUserId = actorUserId,
            NotesEn = notesEn ?? "",
            NotesAr = notesAr ?? "",
        });
        await _db.SaveChangesAsync(ct);
    }

    // Stage names in Arabic for the rejection message. Mirrors the
    // 'imp.pdca.*' i18n keys the SPA renders.
    private static string ArStage(PdcaStage s) => s switch
    {
        PdcaStage.Plan   => "تخطيط",
        PdcaStage.Do     => "تنفيذ",
        PdcaStage.Check  => "تدقيق",
        PdcaStage.Act    => "تعميم",
        PdcaStage.Closed => "مغلقة",
        _ => s.ToString(),
    };
}

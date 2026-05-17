// Subagent 1 ships no-op fallbacks for every Round-5 workflow contract so
// the API compiles and runs end-to-end before Subagent 2 lands. Subagent 2
// replaces these registrations with real implementations.

using CxPlatform.Application.Dtos;
using CxPlatform.Application.Services;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Infrastructure.WorkflowStubs;

// PDCA stub — writes a log row + flips the stage, but does NOT enforce
// legal-transition rules. Subagent 2's real service must reject illegal
// jumps (e.g. Plan → Closed without going through Do/Check/Act).
public class PdcaTransitionServiceStub : IPdcaTransitionService
{
    private readonly AppDbContext _db;
    public PdcaTransitionServiceStub(AppDbContext db) { _db = db; }

    public async Task<PdcaTransitionResult> TransitionAsync(
        long improvementItemId, PdcaStage toStage, long? actorUserId,
        string notesEn, string notesAr, CancellationToken ct = default)
    {
        var item = await _db.ImprovementItems.FirstOrDefaultAsync(x => x.Id == improvementItemId, ct);
        if (item is null) return new PdcaTransitionResult(false, "not found", null, null);
        var from = item.PdcaStage;
        item.PdcaStage = toStage;
        item.UpdatedAt = DateTime.UtcNow;
        if (toStage == PdcaStage.Closed) item.ClosedAt = DateTime.UtcNow;
        else if (from == PdcaStage.Closed) item.ClosedAt = null;
        _db.PdcaCycleLogs.Add(new PdcaCycleLog
        {
            ImprovementItemId = item.Id,
            FromStage = from, ToStage = toStage,
            ActorUserId = actorUserId,
            NotesEn = notesEn ?? "", NotesAr = notesAr ?? "",
        });
        await _db.SaveChangesAsync(ct);
        return new PdcaTransitionResult(true, null, from, toStage);
    }
}

// No-op threshold evaluator — returns an empty list so callers don't crash.
// Subagent 2 implements the real find-breaches → create-items logic.
public class ThresholdEvaluationServiceStub : IThresholdEvaluationService
{
    public Task<IReadOnlyList<ImprovementItemDto>> EvaluateAsync(
        long kpiId, decimal currentValue, long? actorUserId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ImprovementItemDto>>(Array.Empty<ImprovementItemDto>());
}

// Constant-zero freshness so callers see "due for review". Subagent 2
// implements the real decay curve.
public class ContentFreshnessServiceStub : IContentFreshnessService
{
    public int Score(DateTime updatedAt, bool enArParityFlag) => 0;
    public Task RecalculateAllAsync(CancellationToken ct = default) => Task.CompletedTask;
}

// Aggregator stub — no-op. Subagent 2 reads from voc + complaints + journeys.
public class CxAnalyticsAggregatorServiceStub : ICxAnalyticsAggregatorService
{
    public Task<int> BuildDailySnapshotsAsync(DateTime date, CancellationToken ct = default)
        => Task.FromResult(0);
}

// Synthetic-check runner stub — Subagent 2 wires this into a hosted
// BackgroundService and loops every 60 s.
public class SyntheticCheckRunnerStub : ISyntheticCheckRunner
{
    public Task<int> RunOnceAsync(CancellationToken ct = default) => Task.FromResult(0);
}

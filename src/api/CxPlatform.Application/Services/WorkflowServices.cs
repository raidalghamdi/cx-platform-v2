// Round 5 workflow-service contracts. These interfaces are the seam between
// the additive Subagent 1 build (controllers + entities + 5 pages) and
// Subagent 2's implementations (PDCA transitions, threshold evaluation,
// freshness scoring, analytics aggregation, synthetic-check background
// service). Subagent 1 wires the interfaces into DI with no-op defaults so
// the build stays green; Subagent 2 swaps in the real implementations.

using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Enums;

namespace CxPlatform.Application.Services;

// ── PDCA transitions ──────────────────────────────────────────────────────
// Enforces Plan→Do→Check→Act→Closed transitions and writes a PdcaCycleLog
// row on every change. Implementation must reject illegal jumps and stamp
// ClosedAt when ToStage == Closed.
public interface IPdcaTransitionService
{
    Task<PdcaTransitionResult> TransitionAsync(
        long improvementItemId,
        PdcaStage toStage,
        long? actorUserId,
        string notesEn,
        string notesAr,
        CancellationToken ct = default);
}

public record PdcaTransitionResult(
    bool Ok,
    string? Error,
    PdcaStage? FromStage,
    PdcaStage? ToStage);

// ── KPI threshold evaluation ─────────────────────────────────────────────
// Given a KPI's current value, finds breaching thresholds and (per
// BreachAction) creates an ImprovementItem in PdcaStage.Plan and/or
// emits a Notification.
public interface IThresholdEvaluationService
{
    Task<IReadOnlyList<ImprovementItemDto>> EvaluateAsync(
        long kpiId,
        decimal currentValue,
        long? actorUserId,
        CancellationToken ct = default);
}

// ── Content freshness ─────────────────────────────────────────────────────
// Decays with article age; under 60 OR EN/AR parity broken flags the
// article for review. Returns the score that callers can persist on the
// matching ContentReviewCycle.
public interface IContentFreshnessService
{
    int Score(DateTime updatedAt, bool enArParityFlag);
    Task RecalculateAllAsync(CancellationToken ct = default);
}

// ── CX analytics daily aggregator ─────────────────────────────────────────
// Reads voc_responses + complaints + journey data and produces a daily
// CxAnalyticsSnapshot per segment. Idempotent for a given (date, segment).
public interface ICxAnalyticsAggregatorService
{
    Task<int> BuildDailySnapshotsAsync(DateTime date, CancellationToken ct = default);
}

// ── Synthetic-check runner ────────────────────────────────────────────────
// Subagent 2 hosts this as a BackgroundService in CxPlatform.Api that
// loops every 60 s and writes ServiceHealthMetric rows. Subagent 1 ships
// the contract + a no-op fallback so the API still compiles & runs.
public interface ISyntheticCheckRunner
{
    Task<int> RunOnceAsync(CancellationToken ct = default);
}

// Phase 0 DTOs — flat records for JSON shape stability. Every bilingual field
// is exposed as *_en / *_ar so the client picks based on the active locale
// rather than the server guessing.

using CxPlatform.Domain.Enums;

namespace CxPlatform.Application.Dtos;

// ── Auth ────────────────────────────────────────────────────────────────────

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User,
    IReadOnlyList<RolePermissionDto> Permissions);

public record RefreshRequest(string RefreshToken);

// ── User ────────────────────────────────────────────────────────────────────

public record UserDto(
    long Id,
    string Email,
    string Role,
    string NameEn,
    string NameAr,
    string TitleEn,
    string TitleAr,
    string Landing);

// ── KPI ─────────────────────────────────────────────────────────────────────

public record KpiDto(
    string Key,
    string NameEn,
    string NameAr,
    decimal Value,
    string Unit,
    decimal Delta,
    decimal? Target,
    string Source,
    DateTime LastSyncAt);

// ── Complaint ──────────────────────────────────────────────────────────────

public record ComplaintListItemDto(
    long Id,
    string Code,
    string Category,
    string SubjectEn,
    string SubjectAr,
    ComplaintStatus Status,
    Priority Priority,
    string Channel,
    bool DownJourney,
    string? JourneyStageEn,
    string? JourneyStageAr,
    DateTime OpenedAt,
    DateTime? ClosedAt);

public record ComplaintDto(
    long Id,
    string Code,
    string Category,
    string SubjectEn,
    string SubjectAr,
    string BodyEn,
    string BodyAr,
    ComplaintStatus Status,
    Priority Priority,
    string Channel,
    bool DownJourney,
    string? JourneyStageEn,
    string? JourneyStageAr,
    long? CustomerId,
    long? AssignedTo,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    string? MonafasahRef);

public record UpdateComplaintStatusRequest(ComplaintStatus Status);
public record AddComplaintNoteRequest(string Note);
public record AssignComplaintRequest(long? UserId);

public record ComplaintsByCategoryDto(string Category, int Count);

// ── Inbox ──────────────────────────────────────────────────────────────────

public record InboxThreadDto(
    long Id,
    InboxChannel Channel,
    string FromAddress,
    string FromName,
    string? Subject,
    string Body,
    InboxStatus Status,
    Priority Priority,
    DateTime ReceivedAt,
    DateTime? RepliedAt,
    string? ReplySubject,
    string? ReplyBody);

public record ReplyToThreadRequest(string Body, string? Subject = null);

public record UpdateThreadStatusRequest(InboxStatus Status);

// ── Admin ──────────────────────────────────────────────────────────────────

public record RolePermissionDto(string Role, string PageKey, bool Allowed);

public record UpdateRolePermissionsRequest(IReadOnlyList<RolePermissionDto> Items);

public record ContactChannelDto(string Key, string Value);

public record UpdateContactChannelRequest(string Value);

// ── Notifications ───────────────────────────────────────────────────────────

public record NotificationDto(
    long Id,
    string TitleEn,
    string TitleAr,
    string BodyEn,
    string BodyAr,
    string Kind,
    DateTime CreatedAt,
    DateTime? ReadAt);

// ── Health ─────────────────────────────────────────────────────────────────

public record HealthDto(bool Ok, string Version, DateTime Timestamp);

// ── Phase 1: Journeys ──────────────────────────────────────────────────────

public record JourneyDto(
    long Id,
    string NameEn,
    string NameAr,
    string Persona,
    int StageCount,
    string Status,
    DateTime CreatedAt);

public record JourneyStageDto(
    long Id,
    long JourneyId,
    int Sequence,
    string NameEn,
    string NameAr,
    string TouchpointEn,
    string TouchpointAr,
    string PainPointEn,
    string PainPointAr,
    int EmotionScore);

public record JourneyDetailDto(JourneyDto Journey, IReadOnlyList<JourneyStageDto> Stages);

public record UpsertJourneyRequest(
    string NameEn,
    string NameAr,
    string Persona,
    string Status,
    IReadOnlyList<UpsertJourneyStage> Stages);

public record UpsertJourneyStage(
    int Sequence,
    string NameEn,
    string NameAr,
    string TouchpointEn,
    string TouchpointAr,
    string PainPointEn,
    string PainPointAr,
    int EmotionScore);

// ── Phase 1: VoC ───────────────────────────────────────────────────────────

public record VocResponseDto(
    long Id,
    string SurveyEn,
    string SurveyAr,
    string Channel,
    int NpsScore,
    string Sentiment,
    string CommentEn,
    string CommentAr,
    DateTime RespondedAt,
    string CustomerName);

public record CreateVocResponseRequest(
    string SurveyEn,
    string SurveyAr,
    string Channel,
    int NpsScore,
    string Sentiment,
    string CommentEn,
    string CommentAr,
    string CustomerName);

public record UpdateVocCommentRequest(string CommentEn, string CommentAr);

// ── Phase 1: KB ────────────────────────────────────────────────────────────

public record KbArticleDto(
    long Id,
    string TitleEn,
    string TitleAr,
    string Category,
    string BodyEn,
    string BodyAr,
    long? AuthorId,
    string Status,
    DateTime UpdatedAt);

public record UpsertKbArticleRequest(
    string TitleEn,
    string TitleAr,
    string Category,
    string BodyEn,
    string BodyAr,
    string Status);

// ── Phase 1: Programme ─────────────────────────────────────────────────────

public record ProgrammeInitiativeDto(
    long Id,
    string NameEn,
    string NameAr,
    string Owner,
    string RagStatus,
    int ProgressPct,
    DateTime StartDate,
    DateTime TargetDate,
    string Notes);

public record UpdateProgrammeStatusRequest(string RagStatus, int ProgressPct, string? Notes);

// ── Phase 1: Governance ────────────────────────────────────────────────────

public record GovernanceBodyDto(
    long Id,
    string NameEn,
    string NameAr,
    string Cadence,
    string Chair,
    IReadOnlyList<string> Members,
    string? CharterUrl);

public record GovernanceDecisionDto(
    long Id,
    long BodyId,
    DateTime DecidedAt,
    string TitleEn,
    string TitleAr,
    string Decision,
    string OwnerEn,
    string OwnerAr,
    DateTime? DueDate);

public record GovernanceBodyDetailDto(GovernanceBodyDto Body, IReadOnlyList<GovernanceDecisionDto> Decisions);

public record CreateGovernanceDecisionRequest(
    string TitleEn,
    string TitleAr,
    string Decision,
    string OwnerEn,
    string OwnerAr,
    DateTime? DueDate);

// ── Phase 2: About ─────────────────────────────────────────────────────────

public record AboutSectionDto(
    long Id,
    string KeyEn,
    string KeyAr,
    string BodyEn,
    string BodyAr,
    int OrderIndex,
    DateTime UpdatedAt);

public record UpdateAboutSectionRequest(
    string KeyEn,
    string KeyAr,
    string BodyEn,
    string BodyAr,
    int OrderIndex);

// ── Phase 2: Architecture (static reference data) ──────────────────────────

public record ArchitectureDomainDto(
    string Id,
    string NameEn,
    string NameAr,
    string DescriptionEn,
    string DescriptionAr);

public record ArchitecturePatternDto(
    string Id,
    string NameEn,
    string NameAr,
    string Style,                              // synchronous / async / batch
    string UsageEn,
    string UsageAr);

public record ArchitectureReferenceDto(
    IReadOnlyList<ArchitectureDomainDto> Domains,
    IReadOnlyList<ArchitecturePatternDto> Patterns);

// ── Phase 2: Portal ────────────────────────────────────────────────────────

public record PortalRequestDto(
    long Id,
    string Type,
    string SubjectEn,
    string SubjectAr,
    string BodyEn,
    string BodyAr,
    string Status,
    DateTime CreatedAt);

public record CreatePortalRequestRequest(
    string Type,
    string SubjectEn,
    string SubjectAr,
    string BodyEn,
    string BodyAr);

// ── Phase 2: Copilot ───────────────────────────────────────────────────────

public record AskCopilotRequest(
    string Intent,
    string PromptEn,
    string PromptAr);

public record CopilotInteractionDto(
    long Id,
    string Intent,
    string PromptEn,
    string PromptAr,
    string ResponseEn,
    string ResponseAr,
    int LatencyMs,
    bool Success,
    DateTime CreatedAt);

// ── Phase 2: Audit ─────────────────────────────────────────────────────────

public record AuditEventDto(
    long Id,
    string Kind,
    long? ActorUserId,
    string TargetKind,
    long? TargetId,
    string PrevHash,
    string EntryHash,
    string PayloadJson,
    DateTime At);

public record AuditPageDto(
    IReadOnlyList<AuditEventDto> Items,
    int Total,
    int Page,
    int PageSize);

public record AuditVerifyResultDto(
    bool Ok,
    int Total,
    int? FirstBrokenIndex,
    long? FirstBrokenId);

// ── Phase 2: Automation ────────────────────────────────────────────────────

public record AutomationRuleDto(
    long Id,
    string NameEn,
    string NameAr,
    string TriggerType,
    string ConditionJson,
    string ActionType,
    bool Enabled,
    DateTime? LastRunAt,
    string LastRunStatus,
    int RunCount);

public record ToggleAutomationRequest(bool Enabled);

public record AutomationRunResultDto(bool Ok, string Status, int LatencyMs, string? Note);

// ──────────────────────────────────────────────────────────────────────────
// Round 5: Maturity-model DTOs
// ──────────────────────────────────────────────────────────────────────────

// Gap 1 — Accessibility
public record AccessibilityAuditDto(
    long Id,
    DateTime AuditDate,
    string Auditor,
    IReadOnlyList<string> ScopePages,
    string WcagLevel,
    int TotalIssues,
    int OpenIssues,
    string? ReportUrl,
    string Notes);

public record AccessibilityRemediationDto(
    long Id,
    long AuditId,
    string WcagCriterion,
    string Severity,
    string DescriptionEn,
    string DescriptionAr,
    string Owner,
    string Status,
    DateTime? TargetDate,
    DateTime? ResolvedDate);

public record AccessibilityAuditDetailDto(
    AccessibilityAuditDto Audit,
    IReadOnlyList<AccessibilityRemediationDto> Items);

public record CreateAccessibilityAuditRequest(
    string Auditor,
    IReadOnlyList<string> ScopePages,
    string WcagLevel,
    string? ReportUrl,
    string Notes);

public record UpdateAccessibilityRemediationRequest(
    string Status,
    DateTime? TargetDate,
    string Owner);

public record WcagCriterionDto(string Id, string Name, string Level, string Principle);

// Gap 2 — Service health
public record ServiceHealthMetricDto(
    long Id,
    string ServiceName,
    DateTime MeasuredAt,
    decimal UptimePct,
    int P95LatencyMs,
    decimal ErrorRatePct,
    int MttrMinutes,
    int RequestCount);

public record ServiceIncidentDto(
    long Id,
    string ServiceName,
    DateTime OpenedAt,
    DateTime? ResolvedAt,
    string Severity,
    string TitleEn,
    string TitleAr,
    string RootCauseEn,
    string RootCauseAr,
    string RemediationEn,
    string RemediationAr,
    string Status);

public record CreateServiceIncidentRequest(
    string ServiceName,
    string Severity,
    string TitleEn,
    string TitleAr,
    string RootCauseEn,
    string RootCauseAr);

public record UpdateServiceIncidentRequest(
    string Status,
    string? RemediationEn,
    string? RemediationAr);

public record SyntheticCheckDto(
    long Id,
    string Name,
    string Endpoint,
    int IntervalSeconds,
    DateTime? LastRunAt,
    string LastStatus,
    int LastLatencyMs,
    bool Enabled);

public record ToggleSyntheticCheckRequest(bool Enabled);

// Gap 3 — Continuous improvement
public record KpiThresholdDto(
    long Id,
    long KpiId,
    string KpiKey,
    decimal ThresholdValue,
    string ComparisonOp,
    string BreachAction,
    bool Enabled);

public record UpdateKpiThresholdRequest(
    decimal ThresholdValue,
    string ComparisonOp,
    string BreachAction,
    bool Enabled);

public record ImprovementItemDto(
    long Id,
    string SourceType,
    long? SourceRefId,
    string TitleEn,
    string TitleAr,
    string DescriptionEn,
    string DescriptionAr,
    string Owner,
    string Priority,
    string PdcaStage,
    DateTime CreatedAt,
    DateTime? TargetDate,
    DateTime? ClosedAt);

public record PdcaLogDto(
    long Id,
    long ImprovementItemId,
    string FromStage,
    string ToStage,
    long? ActorUserId,
    DateTime ChangedAt,
    string NotesEn,
    string NotesAr);

public record ImprovementItemDetailDto(ImprovementItemDto Item, IReadOnlyList<PdcaLogDto> Log);

public record CreateImprovementItemRequest(
    string SourceType,
    long? SourceRefId,
    string TitleEn,
    string TitleAr,
    string DescriptionEn,
    string DescriptionAr,
    string Owner,
    string Priority,
    DateTime? TargetDate);

public record TransitionPdcaRequest(string ToStage, string NotesEn, string NotesAr);

// Gap 4 — CX analytics
public record CxAnalyticsSnapshotDto(
    long Id,
    DateTime SnapshotDate,
    decimal Csat,
    decimal Nps,
    decimal Ces,
    int ComplaintVolume,
    decimal ResolutionRateP95Hours,
    long? JourneyId,
    string Segment);

public record CxAnalyticsTrendDto(
    string Segment,
    IReadOnlyList<CxAnalyticsSnapshotDto> Points);

public record RootCauseLinkDto(
    long Id,
    string FromType,
    long FromRefId,
    string ToType,
    long ToRefId,
    decimal LinkStrength,
    string Notes);

public record CreateRootCauseLinkRequest(
    string FromType,
    long FromRefId,
    string ToType,
    long ToRefId,
    decimal LinkStrength,
    string Notes);

// Gap 5 — Content & channels
public record ContentReviewCycleDto(
    long Id,
    long KbArticleId,
    string ArticleTitleEn,
    string ArticleTitleAr,
    DateTime DueDate,
    string AssignedReviewer,
    string Status,
    DateTime? CompletedAt,
    int FreshnessScore,
    bool EnArParityFlag,
    string Notes);

public record CreateContentReviewCycleRequest(
    long KbArticleId,
    DateTime DueDate,
    string AssignedReviewer);

public record UpdateContentReviewCycleRequest(
    string Status,
    string AssignedReviewer,
    string Notes);

public record ChannelPerformanceMetricDto(
    long Id,
    string Channel,
    DateTime MeasuredAt,
    int VolumeCount,
    decimal AvgResponseMinutes,
    decimal ResolutionRatePct,
    decimal CsatScore);

public record StaleArticleDto(
    long ArticleId,
    string TitleEn,
    string TitleAr,
    string Category,
    DateTime UpdatedAt,
    int FreshnessScore,
    bool EnArParityFlag);

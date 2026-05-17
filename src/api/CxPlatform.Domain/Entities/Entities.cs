// All Phase 0 entities live here in a single file for readability — each
// entity is a plain POCO with no behaviour. Persistence config is in
// AppDbContext (Infrastructure). Long IDs throughout, MySQL-friendly.

using CxPlatform.Domain.Enums;

namespace CxPlatform.Domain.Entities;

public abstract class EntityBase
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ── identity / auth ─────────────────────────────────────────────────────────

public class User : EntityBase
{
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "customer";    // admin/supervisor/agent/quality/customer/executive
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string TitleEn { get; set; } = "";
    public string TitleAr { get; set; } = "";
    public string FunctionEn { get; set; } = "";
    public string FunctionAr { get; set; } = "";
    public string Status { get; set; } = "active";    // active/disabled
    public string Landing { get; set; } = "/dashboard";
}

public class RefreshToken : EntityBase
{
    public long UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}

public class RolePermission : EntityBase
{
    public string Role { get; set; } = "";
    public string PageKey { get; set; } = "";    // e.g. "/dashboard"
    public bool Allowed { get; set; }
}

// ── KPIs ────────────────────────────────────────────────────────────────────

public class Kpi : EntityBase
{
    public string Key { get; set; } = "";           // e.g. "csat", "response_time"
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public decimal Value { get; set; }
    public string Unit { get; set; } = "";          // %, h, count
    public decimal Delta { get; set; }              // signed delta vs target
    public decimal? Target { get; set; }
    public string Source { get; set; } = "Excel";   // "Excel" | "API"
    public DateTime LastSyncAt { get; set; } = DateTime.UtcNow;
    public string RoleScope { get; set; } = "all";  // pipe-joined roles or "all"
}

// ── complaints ──────────────────────────────────────────────────────────────

public class Complaint : EntityBase
{
    public string Code { get; set; } = "";          // e.g. "COMP-001"
    public string Category { get; set; } = "";
    public string SubjectEn { get; set; } = "";
    public string SubjectAr { get; set; } = "";
    public string BodyEn { get; set; } = "";
    public string BodyAr { get; set; } = "";
    public long? CustomerId { get; set; }
    public ComplaintStatus Status { get; set; } = ComplaintStatus.New;
    public Priority Priority { get; set; } = Priority.Normal;
    public string Channel { get; set; } = "web";    // web/email/whatsapp/walk-in
    public bool DownJourney { get; set; }
    public string? JourneyStageEn { get; set; }
    public string? JourneyStageAr { get; set; }
    public long? AssignedTo { get; set; }           // user id
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public string? MonafasahRef { get; set; }
}

public class ComplaintEvent : EntityBase
{
    public long ComplaintId { get; set; }
    public string Kind { get; set; } = "";          // "status" | "note" | "assign"
    public string PayloadJson { get; set; } = "{}";
    public long? ByUserId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
}

// ── inbox ───────────────────────────────────────────────────────────────────

public class InboxThread : EntityBase
{
    public InboxChannel Channel { get; set; }
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public string? Subject { get; set; }
    public string Body { get; set; } = "";
    public InboxStatus Status { get; set; } = InboxStatus.New;
    public Priority Priority { get; set; } = Priority.Normal;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RepliedAt { get; set; }
    public string? ReplySubject { get; set; }
    public string? ReplyBody { get; set; }
    public long? CustomerId { get; set; }
    public string? ExternalId { get; set; }
}

// ── audit (hash-chained) ────────────────────────────────────────────────────

public class AuditEvent : EntityBase
{
    public string Kind { get; set; } = "";          // e.g. "complaint.status_changed"
    public long? ActorUserId { get; set; }
    public string TargetKind { get; set; } = "";    // entity name
    public long? TargetId { get; set; }
    public string PrevHash { get; set; } = new string('0', 64);
    public string EntryHash { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public DateTime At { get; set; } = DateTime.UtcNow;
}

// ── contact channels (Admin) ────────────────────────────────────────────────

public class ContactChannel : EntityBase
{
    public string Key { get; set; } = "";           // "whatsapp" | "info_email" | "support_hours"
    public string Value { get; set; } = "";
    public long? UpdatedBy { get; set; }
}

// ── notifications ───────────────────────────────────────────────────────────

public class Notification : EntityBase
{
    public long UserId { get; set; }
    public string TitleEn { get; set; } = "";
    public string TitleAr { get; set; } = "";
    public string BodyEn { get; set; } = "";
    public string BodyAr { get; set; } = "";
    public string Kind { get; set; } = "info";       // info/warning/success
    public DateTime? ReadAt { get; set; }
}

// ── Phase 1 — Journeys ──────────────────────────────────────────────────────

public class Journey : EntityBase
{
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string Persona { get; set; } = "";        // e.g. "Citizen — Onboarding", "Business — Renewal"
    public int StageCount { get; set; }              // denormalised count for list views
    public string Status { get; set; } = "active";   // active/draft/retired
}

public class JourneyStage : EntityBase
{
    public long JourneyId { get; set; }
    public int Sequence { get; set; }                // 1-based stage order
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string TouchpointEn { get; set; } = "";
    public string TouchpointAr { get; set; } = "";
    public string PainPointEn { get; set; } = "";
    public string PainPointAr { get; set; } = "";
    public int EmotionScore { get; set; }            // -2..+2 (frustrated → delighted)
}

// ── Phase 1 — Voice of Customer ─────────────────────────────────────────────

public class VocResponse : EntityBase
{
    public string SurveyEn { get; set; } = "";       // e.g. "Service satisfaction Q1"
    public string SurveyAr { get; set; } = "";
    public string Channel { get; set; } = "";        // email/whatsapp/portal/branch
    public int NpsScore { get; set; }                // 0-10
    public string Sentiment { get; set; } = "neutral"; // positive/neutral/negative
    public string CommentEn { get; set; } = "";
    public string CommentAr { get; set; } = "";
    public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    public string CustomerName { get; set; } = "";
}

// ── Phase 1 — Knowledge Base ────────────────────────────────────────────────

public class KbArticle : EntityBase
{
    public string TitleEn { get; set; } = "";
    public string TitleAr { get; set; } = "";
    public string Category { get; set; } = "";       // free-form taxonomy, e.g. "complaints"
    public string BodyEn { get; set; } = "";
    public string BodyAr { get; set; } = "";
    public long? AuthorId { get; set; }
    public string Status { get; set; } = "published"; // draft/published/retired
}

// ── Phase 1 — Programme initiatives ─────────────────────────────────────────

public class ProgrammeInitiative : EntityBase
{
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string Owner { get; set; } = "";          // person/team name (denormalised)
    public string RagStatus { get; set; } = "amber"; // red/amber/green
    public int ProgressPct { get; set; }             // 0..100
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime TargetDate { get; set; } = DateTime.UtcNow.AddMonths(3);
    public string Notes { get; set; } = "";
}

// ── Phase 1 — Governance bodies + decisions ─────────────────────────────────

public class GovernanceBody : EntityBase
{
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string Cadence { get; set; } = "monthly"; // weekly/biweekly/monthly/quarterly
    public string Chair { get; set; } = "";
    public string MembersJson { get; set; } = "[]";  // JSON array of names
    public string? CharterUrl { get; set; }
}

public class GovernanceDecision : EntityBase
{
    public long BodyId { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    public string TitleEn { get; set; } = "";
    public string TitleAr { get; set; } = "";
    public string Decision { get; set; } = "";       // free-form summary
    public string OwnerEn { get; set; } = "";
    public string OwnerAr { get; set; } = "";
    public DateTime? DueDate { get; set; }
}

// ── Phase 2 — About page editable sections ─────────────────────────────────

public class AboutSection : EntityBase
{
    public string KeyEn { get; set; } = "";          // e.g. "Our story"
    public string KeyAr { get; set; } = "";          // e.g. "قصتنا"
    public string BodyEn { get; set; } = "";
    public string BodyAr { get; set; } = "";
    public int OrderIndex { get; set; }              // controls page order
}

// ── Phase 2 — Copilot interactions ─────────────────────────────────────────

public class CopilotInteraction : EntityBase
{
    public long? UserId { get; set; }
    public string PromptEn { get; set; } = "";
    public string PromptAr { get; set; } = "";
    public string ResponseEn { get; set; } = "";
    public string ResponseAr { get; set; } = "";
    public string Intent { get; set; } = "ask";      // ask/draft_reply/summarise/find_similar
    public int LatencyMs { get; set; }
    public bool Success { get; set; } = true;
}

// ── Phase 2 — Automation (RPA) rules ───────────────────────────────────────

public class AutomationRule : EntityBase
{
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string TriggerType { get; set; } = "";    // complaint.created / voc.negative / scheduled / inbox.new
    public string ConditionJson { get; set; } = "{}";
    public string ActionType { get; set; } = "";     // notify / assign / escalate / kb_publish
    public bool Enabled { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public string LastRunStatus { get; set; } = "";  // success / failure / skipped / never
    public int RunCount { get; set; }
}

// ── Phase 2 — Customer-portal lodge / track requests ───────────────────────

public class PortalRequest : EntityBase
{
    public long? CustomerId { get; set; }
    public string Type { get; set; } = "complaint";  // complaint / inquiry / appointment
    public string SubjectEn { get; set; } = "";
    public string SubjectAr { get; set; } = "";
    public string BodyEn { get; set; } = "";
    public string BodyAr { get; set; } = "";
    public string Status { get; set; } = "new";      // new / in_progress / resolved / closed
}

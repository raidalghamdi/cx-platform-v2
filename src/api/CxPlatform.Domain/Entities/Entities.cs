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

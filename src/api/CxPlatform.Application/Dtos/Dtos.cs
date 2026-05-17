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

using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;

namespace CxPlatform.Api.Mappers;

// Thin manual mappers — AutoMapper-free to keep the dependency graph small.
public static class Mappers
{
    public static UserDto ToDto(this User u) =>
        new(u.Id, u.Email, u.Role, u.NameEn, u.NameAr, u.TitleEn, u.TitleAr, u.Landing);

    public static KpiDto ToDto(this Kpi k) =>
        new(k.Key, k.NameEn, k.NameAr, k.Value, k.Unit, k.Delta, k.Target, k.Source, k.LastSyncAt);

    public static ComplaintListItemDto ToListItem(this Complaint c) =>
        new(c.Id, c.Code, c.Category, c.SubjectEn, c.SubjectAr, c.Status, c.Priority, c.Channel,
            c.DownJourney, c.JourneyStageEn, c.JourneyStageAr, c.OpenedAt, c.ClosedAt);

    public static ComplaintDto ToDto(this Complaint c) =>
        new(c.Id, c.Code, c.Category, c.SubjectEn, c.SubjectAr, c.BodyEn, c.BodyAr,
            c.Status, c.Priority, c.Channel, c.DownJourney, c.JourneyStageEn, c.JourneyStageAr,
            c.CustomerId, c.AssignedTo, c.OpenedAt, c.ClosedAt, c.MonafasahRef);

    public static InboxThreadDto ToDto(this InboxThread t) =>
        new(t.Id, t.Channel, t.FromAddress, t.FromName, t.Subject, t.Body,
            t.Status, t.Priority, t.ReceivedAt, t.RepliedAt, t.ReplySubject, t.ReplyBody);

    public static RolePermissionDto ToDto(this RolePermission p) =>
        new(p.Role, p.PageKey, p.Allowed);

    public static ContactChannelDto ToDto(this ContactChannel c) =>
        new(c.Key, c.Value);

    public static NotificationDto ToDto(this Notification n) =>
        new(n.Id, n.TitleEn, n.TitleAr, n.BodyEn, n.BodyAr, n.Kind, n.CreatedAt, n.ReadAt);

    // ── Phase 1 mappers ────────────────────────────────────────────────────

    public static JourneyDto ToDto(this Journey j) =>
        new(j.Id, j.NameEn, j.NameAr, j.Persona, j.StageCount, j.Status, j.CreatedAt);

    public static JourneyStageDto ToDto(this JourneyStage s) =>
        new(s.Id, s.JourneyId, s.Sequence, s.NameEn, s.NameAr,
            s.TouchpointEn, s.TouchpointAr, s.PainPointEn, s.PainPointAr, s.EmotionScore);

    public static VocResponseDto ToDto(this VocResponse v) =>
        new(v.Id, v.SurveyEn, v.SurveyAr, v.Channel, v.NpsScore, v.Sentiment,
            v.CommentEn, v.CommentAr, v.RespondedAt, v.CustomerName);

    public static KbArticleDto ToDto(this KbArticle a) =>
        new(a.Id, a.TitleEn, a.TitleAr, a.Category, a.BodyEn, a.BodyAr, a.AuthorId, a.Status, a.UpdatedAt);

    public static ProgrammeInitiativeDto ToDto(this ProgrammeInitiative p) =>
        new(p.Id, p.NameEn, p.NameAr, p.Owner, p.RagStatus, p.ProgressPct,
            p.StartDate, p.TargetDate, p.Notes);

    public static GovernanceBodyDto ToDto(this GovernanceBody b)
    {
        // Members are persisted as a JSON array of strings. Decode safely so
        // a malformed row doesn't break the whole list endpoint.
        IReadOnlyList<string> members;
        try
        {
            members = System.Text.Json.JsonSerializer.Deserialize<List<string>>(
                string.IsNullOrWhiteSpace(b.MembersJson) ? "[]" : b.MembersJson)
                ?? new List<string>();
        }
        catch
        {
            members = new List<string>();
        }
        return new GovernanceBodyDto(b.Id, b.NameEn, b.NameAr, b.Cadence, b.Chair, members, b.CharterUrl);
    }

    public static GovernanceDecisionDto ToDto(this GovernanceDecision d) =>
        new(d.Id, d.BodyId, d.DecidedAt, d.TitleEn, d.TitleAr, d.Decision,
            d.OwnerEn, d.OwnerAr, d.DueDate);

    // ── Phase 2 mappers ────────────────────────────────────────────────────

    public static AboutSectionDto ToDto(this AboutSection s) =>
        new(s.Id, s.KeyEn, s.KeyAr, s.BodyEn, s.BodyAr, s.OrderIndex, s.UpdatedAt);

    public static PortalRequestDto ToDto(this PortalRequest r) =>
        new(r.Id, r.Type, r.SubjectEn, r.SubjectAr, r.BodyEn, r.BodyAr, r.Status, r.CreatedAt);

    public static CopilotInteractionDto ToDto(this CopilotInteraction c) =>
        new(c.Id, c.Intent, c.PromptEn, c.PromptAr, c.ResponseEn, c.ResponseAr,
            c.LatencyMs, c.Success, c.CreatedAt);

    public static AuditEventDto ToDto(this AuditEvent a) =>
        new(a.Id, a.Kind, a.ActorUserId, a.TargetKind, a.TargetId,
            a.PrevHash, a.EntryHash, a.PayloadJson, a.At);

    public static AutomationRuleDto ToDto(this AutomationRule r) =>
        new(r.Id, r.NameEn, r.NameAr, r.TriggerType, r.ConditionJson, r.ActionType,
            r.Enabled, r.LastRunAt, r.LastRunStatus, r.RunCount);
}

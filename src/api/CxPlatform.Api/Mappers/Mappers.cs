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
}

using CxPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public DbSet<User>            Users           => Set<User>();
    public DbSet<RefreshToken>    RefreshTokens   => Set<RefreshToken>();
    public DbSet<RolePermission>  RolePermissions => Set<RolePermission>();
    public DbSet<Kpi>             Kpis            => Set<Kpi>();
    public DbSet<Complaint>       Complaints      => Set<Complaint>();
    public DbSet<ComplaintEvent>  ComplaintEvents => Set<ComplaintEvent>();
    public DbSet<InboxThread>     InboxThreads    => Set<InboxThread>();
    public DbSet<AuditEvent>      AuditEvents     => Set<AuditEvent>();
    public DbSet<ContactChannel>  ContactChannels => Set<ContactChannel>();
    public DbSet<Notification>    Notifications   => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ── Users ───────────────────────────────────────────────────────────
        mb.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(190).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.Property(x => x.NameEn).HasMaxLength(190);
            e.Property(x => x.NameAr).HasMaxLength(190);
            e.Property(x => x.TitleEn).HasMaxLength(190);
            e.Property(x => x.TitleAr).HasMaxLength(190);
            e.Property(x => x.FunctionEn).HasMaxLength(190);
            e.Property(x => x.FunctionAr).HasMaxLength(190);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.Landing).HasMaxLength(190);
        });

        mb.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.Property(x => x.TokenHash).HasMaxLength(190).IsRequired();
        });

        mb.Entity<RolePermission>(e =>
        {
            e.ToTable("role_permissions");
            e.HasIndex(x => new { x.Role, x.PageKey }).IsUnique();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.Property(x => x.PageKey).HasMaxLength(64).IsRequired();
        });

        // ── KPIs ────────────────────────────────────────────────────────────
        mb.Entity<Kpi>(e =>
        {
            e.ToTable("kpis");
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64).IsRequired();
            e.Property(x => x.NameEn).HasMaxLength(190);
            e.Property(x => x.NameAr).HasMaxLength(190);
            e.Property(x => x.Unit).HasMaxLength(16);
            e.Property(x => x.Source).HasMaxLength(32);
            e.Property(x => x.RoleScope).HasMaxLength(190);
            e.Property(x => x.Value).HasColumnType("decimal(18,4)");
            e.Property(x => x.Delta).HasColumnType("decimal(18,4)");
            e.Property(x => x.Target).HasColumnType("decimal(18,4)");
        });

        // ── Complaints ──────────────────────────────────────────────────────
        mb.Entity<Complaint>(e =>
        {
            e.ToTable("complaints");
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.DownJourney);
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Category).HasMaxLength(96);
            e.Property(x => x.SubjectEn).HasMaxLength(255);
            e.Property(x => x.SubjectAr).HasMaxLength(255);
            e.Property(x => x.BodyEn).HasColumnType("text");
            e.Property(x => x.BodyAr).HasColumnType("text");
            e.Property(x => x.Channel).HasMaxLength(32);
            e.Property(x => x.JourneyStageEn).HasMaxLength(96);
            e.Property(x => x.JourneyStageAr).HasMaxLength(96);
            e.Property(x => x.MonafasahRef).HasMaxLength(64);
        });

        mb.Entity<ComplaintEvent>(e =>
        {
            e.ToTable("complaint_events");
            e.HasIndex(x => x.ComplaintId);
            e.Property(x => x.Kind).HasMaxLength(32).IsRequired();
            e.Property(x => x.PayloadJson).HasColumnType("json");
        });

        // ── Inbox ───────────────────────────────────────────────────────────
        mb.Entity<InboxThread>(e =>
        {
            e.ToTable("inbox_threads");
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Channel);
            e.Property(x => x.FromAddress).HasMaxLength(255);
            e.Property(x => x.FromName).HasMaxLength(190);
            e.Property(x => x.Subject).HasMaxLength(255);
            e.Property(x => x.Body).HasColumnType("text");
            e.Property(x => x.ReplySubject).HasMaxLength(255);
            e.Property(x => x.ReplyBody).HasColumnType("text");
            e.Property(x => x.ExternalId).HasMaxLength(190);
        });

        // ── Audit ───────────────────────────────────────────────────────────
        mb.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasIndex(x => x.EntryHash).IsUnique();
            e.HasIndex(x => x.At);
            e.Property(x => x.Kind).HasMaxLength(96).IsRequired();
            e.Property(x => x.TargetKind).HasMaxLength(96);
            e.Property(x => x.PrevHash).HasMaxLength(64).IsFixedLength();
            e.Property(x => x.EntryHash).HasMaxLength(64).IsFixedLength();
            e.Property(x => x.PayloadJson).HasColumnType("json");
        });

        // ── Contact channels ────────────────────────────────────────────────
        mb.Entity<ContactChannel>(e =>
        {
            e.ToTable("contact_channels");
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Key).HasMaxLength(64).IsRequired();
            e.Property(x => x.Value).HasMaxLength(255);
        });

        // ── Notifications ───────────────────────────────────────────────────
        mb.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasIndex(x => x.UserId);
            e.Property(x => x.TitleEn).HasMaxLength(190);
            e.Property(x => x.TitleAr).HasMaxLength(190);
            e.Property(x => x.BodyEn).HasMaxLength(500);
            e.Property(x => x.BodyAr).HasMaxLength(500);
            e.Property(x => x.Kind).HasMaxLength(32);
        });
    }
}

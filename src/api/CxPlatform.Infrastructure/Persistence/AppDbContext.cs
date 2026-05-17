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

    // Phase 1
    public DbSet<Journey>              Journeys              => Set<Journey>();
    public DbSet<JourneyStage>         JourneyStages         => Set<JourneyStage>();
    public DbSet<VocResponse>          VocResponses          => Set<VocResponse>();
    public DbSet<KbArticle>            KbArticles            => Set<KbArticle>();
    public DbSet<ProgrammeInitiative>  ProgrammeInitiatives  => Set<ProgrammeInitiative>();
    public DbSet<GovernanceBody>       GovernanceBodies      => Set<GovernanceBody>();
    public DbSet<GovernanceDecision>   GovernanceDecisions   => Set<GovernanceDecision>();

    // Phase 2
    public DbSet<AboutSection>         AboutSections         => Set<AboutSection>();
    public DbSet<CopilotInteraction>   CopilotInteractions   => Set<CopilotInteraction>();
    public DbSet<AutomationRule>       AutomationRules       => Set<AutomationRule>();
    public DbSet<PortalRequest>        PortalRequests        => Set<PortalRequest>();

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

        // ── Phase 1: Journeys ──────────────────────────────────────────────
        mb.Entity<Journey>(e =>
        {
            e.ToTable("journeys");
            e.HasIndex(x => x.Status);
            e.Property(x => x.NameEn).HasMaxLength(190).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(190).IsRequired();
            e.Property(x => x.Persona).HasMaxLength(96);
            e.Property(x => x.Status).HasMaxLength(32);
        });

        mb.Entity<JourneyStage>(e =>
        {
            e.ToTable("journey_stages");
            e.HasIndex(x => new { x.JourneyId, x.Sequence });
            e.Property(x => x.NameEn).HasMaxLength(190).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(190).IsRequired();
            e.Property(x => x.TouchpointEn).HasMaxLength(190);
            e.Property(x => x.TouchpointAr).HasMaxLength(190);
            e.Property(x => x.PainPointEn).HasColumnType("text");
            e.Property(x => x.PainPointAr).HasColumnType("text");
        });

        // ── Phase 1: VoC ───────────────────────────────────────────────────
        mb.Entity<VocResponse>(e =>
        {
            e.ToTable("voc_responses");
            e.HasIndex(x => x.Channel);
            e.HasIndex(x => x.Sentiment);
            e.HasIndex(x => x.RespondedAt);
            e.Property(x => x.SurveyEn).HasMaxLength(190);
            e.Property(x => x.SurveyAr).HasMaxLength(190);
            e.Property(x => x.Channel).HasMaxLength(32);
            e.Property(x => x.Sentiment).HasMaxLength(16);
            e.Property(x => x.CommentEn).HasColumnType("text");
            e.Property(x => x.CommentAr).HasColumnType("text");
            e.Property(x => x.CustomerName).HasMaxLength(190);
        });

        // ── Phase 1: KB ────────────────────────────────────────────────────
        mb.Entity<KbArticle>(e =>
        {
            e.ToTable("kb_articles");
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Status);
            e.Property(x => x.TitleEn).HasMaxLength(255).IsRequired();
            e.Property(x => x.TitleAr).HasMaxLength(255).IsRequired();
            e.Property(x => x.Category).HasMaxLength(96);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.BodyEn).HasColumnType("text");
            e.Property(x => x.BodyAr).HasColumnType("text");
        });

        // ── Phase 1: Programme ─────────────────────────────────────────────
        mb.Entity<ProgrammeInitiative>(e =>
        {
            e.ToTable("programme_initiatives");
            e.HasIndex(x => x.RagStatus);
            e.Property(x => x.NameEn).HasMaxLength(190).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(190).IsRequired();
            e.Property(x => x.Owner).HasMaxLength(190);
            e.Property(x => x.RagStatus).HasMaxLength(16);
            e.Property(x => x.Notes).HasColumnType("text");
        });

        // ── Phase 1: Governance ────────────────────────────────────────────
        mb.Entity<GovernanceBody>(e =>
        {
            e.ToTable("governance_bodies");
            e.Property(x => x.NameEn).HasMaxLength(190).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(190).IsRequired();
            e.Property(x => x.Cadence).HasMaxLength(32);
            e.Property(x => x.Chair).HasMaxLength(190);
            e.Property(x => x.MembersJson).HasColumnType("json");
            e.Property(x => x.CharterUrl).HasMaxLength(500);
        });

        mb.Entity<GovernanceDecision>(e =>
        {
            e.ToTable("governance_decisions");
            e.HasIndex(x => x.BodyId);
            e.HasIndex(x => x.DecidedAt);
            e.Property(x => x.TitleEn).HasMaxLength(255).IsRequired();
            e.Property(x => x.TitleAr).HasMaxLength(255).IsRequired();
            e.Property(x => x.Decision).HasColumnType("text");
            e.Property(x => x.OwnerEn).HasMaxLength(190);
            e.Property(x => x.OwnerAr).HasMaxLength(190);
        });

        // ── Phase 2: About sections ────────────────────────────────────────
        mb.Entity<AboutSection>(e =>
        {
            e.ToTable("about_sections");
            e.HasIndex(x => x.OrderIndex);
            e.Property(x => x.KeyEn).HasMaxLength(190).IsRequired();
            e.Property(x => x.KeyAr).HasMaxLength(190).IsRequired();
            e.Property(x => x.BodyEn).HasColumnType("text");
            e.Property(x => x.BodyAr).HasColumnType("text");
        });

        // ── Phase 2: Copilot interactions ──────────────────────────────────
        mb.Entity<CopilotInteraction>(e =>
        {
            e.ToTable("copilot_interactions");
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.Intent).HasMaxLength(32).IsRequired();
            e.Property(x => x.PromptEn).HasColumnType("text");
            e.Property(x => x.PromptAr).HasColumnType("text");
            e.Property(x => x.ResponseEn).HasColumnType("text");
            e.Property(x => x.ResponseAr).HasColumnType("text");
        });

        // ── Phase 2: Automation rules ──────────────────────────────────────
        mb.Entity<AutomationRule>(e =>
        {
            e.ToTable("automation_rules");
            e.HasIndex(x => x.Enabled);
            e.Property(x => x.NameEn).HasMaxLength(190).IsRequired();
            e.Property(x => x.NameAr).HasMaxLength(190).IsRequired();
            e.Property(x => x.TriggerType).HasMaxLength(64).IsRequired();
            e.Property(x => x.ActionType).HasMaxLength(64).IsRequired();
            e.Property(x => x.ConditionJson).HasColumnType("json");
            e.Property(x => x.LastRunStatus).HasMaxLength(32);
        });

        // ── Phase 2: Portal requests ───────────────────────────────────────
        mb.Entity<PortalRequest>(e =>
        {
            e.ToTable("portal_requests");
            e.HasIndex(x => x.CustomerId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Type).HasMaxLength(32);
            e.Property(x => x.Status).HasMaxLength(32);
            e.Property(x => x.SubjectEn).HasMaxLength(255);
            e.Property(x => x.SubjectAr).HasMaxLength(255);
            e.Property(x => x.BodyEn).HasColumnType("text");
            e.Property(x => x.BodyAr).HasColumnType("text");
        });
    }
}

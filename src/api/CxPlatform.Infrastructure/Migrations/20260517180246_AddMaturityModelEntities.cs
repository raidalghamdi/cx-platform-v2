using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CxPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaturityModelEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accessibility_audits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuditDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Auditor = table.Column<string>(type: "varchar(190)", maxLength: 190, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ScopePagesJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WcagLevel = table.Column<int>(type: "int", nullable: false),
                    TotalIssues = table.Column<int>(type: "int", nullable: false),
                    OpenIssues = table.Column<int>(type: "int", nullable: false),
                    ReportUrl = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessibility_audits", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "accessibility_remediations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuditId = table.Column<long>(type: "bigint", nullable: false),
                    WcagCriterion = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescriptionAr = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Owner = table.Column<string>(type: "varchar(190)", maxLength: 190, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessibility_remediations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "channel_performance_metrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Channel = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MeasuredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    VolumeCount = table.Column<int>(type: "int", nullable: false),
                    AvgResponseMinutes = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    ResolutionRatePct = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    CsatScore = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_performance_metrics", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "content_review_cycles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    KbArticleId = table.Column<long>(type: "bigint", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AssignedReviewer = table.Column<string>(type: "varchar(190)", maxLength: 190, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FreshnessScore = table.Column<int>(type: "int", nullable: false),
                    EnArParityFlag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_review_cycles", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cx_analytics_snapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SnapshotDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Csat = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    Nps = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    Ces = table.Column<decimal>(type: "decimal(6,3)", nullable: false),
                    ComplaintVolume = table.Column<int>(type: "int", nullable: false),
                    ResolutionRateP95Hours = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    JourneyId = table.Column<long>(type: "bigint", nullable: true),
                    Segment = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cx_analytics_snapshots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "improvement_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    SourceRefId = table.Column<long>(type: "bigint", nullable: true),
                    TitleEn = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleAr = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescriptionEn = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DescriptionAr = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Owner = table.Column<string>(type: "varchar(190)", maxLength: 190, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    PdcaStage = table.Column<int>(type: "int", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_improvement_items", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "kpi_thresholds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    KpiId = table.Column<long>(type: "bigint", nullable: false),
                    ThresholdValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ComparisonOp = table.Column<int>(type: "int", nullable: false),
                    BreachAction = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kpi_thresholds", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pdca_cycle_logs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ImprovementItemId = table.Column<long>(type: "bigint", nullable: false),
                    FromStage = table.Column<int>(type: "int", nullable: false),
                    ToStage = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<long>(type: "bigint", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NotesEn = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NotesAr = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdca_cycle_logs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "root_cause_links",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FromType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromRefId = table.Column<long>(type: "bigint", nullable: false),
                    ToType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToRefId = table.Column<long>(type: "bigint", nullable: false),
                    LinkStrength = table.Column<decimal>(type: "decimal(4,3)", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_root_cause_links", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_health_metrics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServiceName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MeasuredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UptimePct = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    P95LatencyMs = table.Column<int>(type: "int", nullable: false),
                    ErrorRatePct = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    MttrMinutes = table.Column<int>(type: "int", nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_health_metrics", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_incidents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ServiceName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OpenedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    TitleEn = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TitleAr = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RootCauseEn = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RootCauseAr = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemediationEn = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RemediationAr = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_incidents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "synthetic_checks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(96)", maxLength: 96, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Endpoint = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IntervalSeconds = table.Column<int>(type: "int", nullable: false),
                    LastRunAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastStatus = table.Column<int>(type: "int", nullable: false),
                    LastLatencyMs = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_synthetic_checks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_accessibility_audits_AuditDate",
                table: "accessibility_audits",
                column: "AuditDate");

            migrationBuilder.CreateIndex(
                name: "IX_accessibility_remediations_AuditId",
                table: "accessibility_remediations",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_accessibility_remediations_Status",
                table: "accessibility_remediations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_channel_performance_metrics_Channel_MeasuredAt",
                table: "channel_performance_metrics",
                columns: new[] { "Channel", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_content_review_cycles_KbArticleId",
                table: "content_review_cycles",
                column: "KbArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_content_review_cycles_Status",
                table: "content_review_cycles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_cx_analytics_snapshots_SnapshotDate_Segment",
                table: "cx_analytics_snapshots",
                columns: new[] { "SnapshotDate", "Segment" });

            migrationBuilder.CreateIndex(
                name: "IX_improvement_items_PdcaStage",
                table: "improvement_items",
                column: "PdcaStage");

            migrationBuilder.CreateIndex(
                name: "IX_improvement_items_SourceType",
                table: "improvement_items",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_kpi_thresholds_KpiId",
                table: "kpi_thresholds",
                column: "KpiId");

            migrationBuilder.CreateIndex(
                name: "IX_pdca_cycle_logs_ImprovementItemId",
                table: "pdca_cycle_logs",
                column: "ImprovementItemId");

            migrationBuilder.CreateIndex(
                name: "IX_root_cause_links_FromType_FromRefId",
                table: "root_cause_links",
                columns: new[] { "FromType", "FromRefId" });

            migrationBuilder.CreateIndex(
                name: "IX_root_cause_links_ToType_ToRefId",
                table: "root_cause_links",
                columns: new[] { "ToType", "ToRefId" });

            migrationBuilder.CreateIndex(
                name: "IX_service_health_metrics_ServiceName_MeasuredAt",
                table: "service_health_metrics",
                columns: new[] { "ServiceName", "MeasuredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_service_incidents_ServiceName_OpenedAt",
                table: "service_incidents",
                columns: new[] { "ServiceName", "OpenedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_service_incidents_Status",
                table: "service_incidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_synthetic_checks_Enabled",
                table: "synthetic_checks",
                column: "Enabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accessibility_audits");

            migrationBuilder.DropTable(
                name: "accessibility_remediations");

            migrationBuilder.DropTable(
                name: "channel_performance_metrics");

            migrationBuilder.DropTable(
                name: "content_review_cycles");

            migrationBuilder.DropTable(
                name: "cx_analytics_snapshots");

            migrationBuilder.DropTable(
                name: "improvement_items");

            migrationBuilder.DropTable(
                name: "kpi_thresholds");

            migrationBuilder.DropTable(
                name: "pdca_cycle_logs");

            migrationBuilder.DropTable(
                name: "root_cause_links");

            migrationBuilder.DropTable(
                name: "service_health_metrics");

            migrationBuilder.DropTable(
                name: "service_incidents");

            migrationBuilder.DropTable(
                name: "synthetic_checks");
        }
    }
}

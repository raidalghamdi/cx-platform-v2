using System.Text.Json;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Infrastructure.Persistence;

// Seed runs on first startup when the database has no users. Idempotent —
// any subsequent call is a no-op. All bilingual strings are stored EN+AR
// side-by-side so the API never has to translate.
public static class Seed
{
    private const string DemoPassword = "demo";

    public static async Task RunAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct)) return;     // already seeded

        // ── Users (6) — passwords bcrypt-hashed, all "demo" ─────────────────
        var pwHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
        var admin = new User
        {
            Email = "admin@cx.gov.sa", PasswordHash = pwHash, Role = "admin",
            NameEn = "Noor Al Noor", NameAr = "نور النور",
            TitleEn = "System Administrator", TitleAr = "مسؤول النظام",
            FunctionEn = "Administration", FunctionAr = "الإدارة",
            Landing = "/admin",
        };
        var supervisor = new User
        {
            Email = "supervisor@cx.gov.sa", PasswordHash = pwHash, Role = "supervisor",
            NameEn = "Fatima Al-Otaibi", NameAr = "فاطمة العتيبي",
            TitleEn = "CX Supervisor", TitleAr = "مشرفة تجربة المستفيد",
            FunctionEn = "Operations", FunctionAr = "العمليات",
            Landing = "/dashboard",
        };
        var agent = new User
        {
            Email = "agent@cx.gov.sa", PasswordHash = pwHash, Role = "agent",
            NameEn = "Ahmed Al-Harbi", NameAr = "أحمد الحربي",
            TitleEn = "Service Agent", TitleAr = "موظف خدمة المستفيدين",
            FunctionEn = "Front-line", FunctionAr = "الخط الأمامي",
            Landing = "/inbox",
        };
        var quality = new User
        {
            Email = "quality@cx.gov.sa", PasswordHash = pwHash, Role = "quality",
            NameEn = "Layla Al-Qahtani", NameAr = "ليلى القحطاني",
            TitleEn = "Quality Officer", TitleAr = "مسؤولة الجودة",
            FunctionEn = "Quality", FunctionAr = "الجودة",
            Landing = "/complaints",
        };
        var customer = new User
        {
            Email = "customer@cx.gov.sa", PasswordHash = pwHash, Role = "customer",
            NameEn = "Khalid Al-Mutairi", NameAr = "خالد المطيري",
            TitleEn = "Citizen", TitleAr = "مواطن",
            FunctionEn = "Citizen", FunctionAr = "مواطن",
            Landing = "/dashboard",
        };
        var executive = new User
        {
            Email = "executive@cx.gov.sa", PasswordHash = pwHash, Role = "executive",
            NameEn = "Raid Al-Ghamdi", NameAr = "رائد الغامدي",
            TitleEn = "Chief of Strategy & Excellence", TitleAr = "رئيس الاستراتيجية والتميز",
            FunctionEn = "Strategy", FunctionAr = "الاستراتيجية",
            Landing = "/dashboard",
        };
        db.Users.AddRange(admin, supervisor, agent, quality, customer, executive);
        await db.SaveChangesAsync(ct);

        // ── Role permissions (admin all true; others per React pilot) ──────
        string[] pages = new[]
        {
            "/about", "/dashboard", "/journeys", "/voc", "/complaints", "/inbox",
            "/kb", "/copilot", "/portal", "/programme", "/governance",
            "/architecture", "/audit", "/automation", "/admin", "/notifications", "/profile",
        };
        // matrix[role][page] = allowed
        var matrix = new Dictionary<string, HashSet<string>>
        {
            ["admin"]      = new(pages),
            ["supervisor"] = new(new[]{"/about","/dashboard","/journeys","/voc","/complaints","/inbox","/kb","/copilot","/programme","/governance","/architecture","/automation","/notifications","/profile"}),
            ["agent"]      = new(new[]{"/about","/journeys","/complaints","/inbox","/kb","/copilot","/notifications","/profile"}),
            ["quality"]    = new(new[]{"/about","/journeys","/voc","/complaints","/kb","/governance","/architecture","/audit","/notifications","/profile"}),
            ["customer"]   = new(new[]{"/about","/portal","/kb","/notifications","/profile"}),
            ["executive"]  = new(new[]{"/about","/dashboard","/journeys","/voc","/kb","/programme","/governance","/architecture","/notifications","/profile"}),
        };
        foreach (var role in matrix.Keys)
            foreach (var p in pages)
                db.RolePermissions.Add(new RolePermission { Role = role, PageKey = p, Allowed = matrix[role].Contains(p) });
        await db.SaveChangesAsync(ct);

        // ── KPIs (9) — Strategic KPIs Excel for most; Monafasah+ API for one ────
        var nowSync = DateTime.UtcNow;
        db.Kpis.AddRange(
            new Kpi { Key = "csat", NameEn = "Customer Satisfaction", NameAr = "نسبة رضا المستفيدين", Value = 87.4m, Unit = "%", Delta = 2.1m, Target = 90m, Source = "Excel", LastSyncAt = nowSync },
            new Kpi { Key = "nps", NameEn = "Net Promoter Score", NameAr = "صافي مؤشر الترويج", Value = 42m, Unit = "", Delta = 6m, Target = 50m, Source = "Excel", LastSyncAt = nowSync },
            new Kpi { Key = "fcr", NameEn = "First Contact Resolution", NameAr = "الحل من أول تواصل", Value = 71.3m, Unit = "%", Delta = 1.4m, Target = 75m, Source = "Excel", LastSyncAt = nowSync },
            new Kpi { Key = "sla", NameEn = "SLA Compliance", NameAr = "الالتزام بمستوى الخدمة", Value = 91.2m, Unit = "%", Delta = -0.6m, Target = 95m, Source = "Excel", LastSyncAt = nowSync },
            new Kpi { Key = "open", NameEn = "Open Complaints", NameAr = "الشكاوى المفتوحة", Value = 318m, Unit = "", Delta = -24m, Source = "API", LastSyncAt = nowSync },
            new Kpi { Key = "response_time", NameEn = "Response Time", NameAr = "زمن الاستجابة", Value = 2.4m, Unit = "h", Delta = -0.3m, Target = 2m, Source = "API", LastSyncAt = nowSync },
            new Kpi { Key = "num_services", NameEn = "Number of Services", NameAr = "عدد الخدمات", Value = 12m, Unit = "", Delta = 1m, Source = "Excel", LastSyncAt = nowSync },
            new Kpi { Key = "completion_rate", NameEn = "Service Completion Rate", NameAr = "معدل إتمام الخدمة", Value = 87.4m, Unit = "%", Delta = 1.2m, Target = 95m, Source = "Excel", LastSyncAt = nowSync },
            new Kpi { Key = "recurring_patterns", NameEn = "Recurring Patterns", NameAr = "الأنماط المتكررة", Value = 6m, Unit = "", Delta = -1m, Source = "Excel", LastSyncAt = nowSync }
        );
        await db.SaveChangesAsync(ct);

        // ── Complaints (8 — 4 marked DownJourney with stages) ─────────────────
        var openedBase = DateTime.UtcNow.AddDays(-9);
        DateTime O(int days) => openedBase.AddDays(days);

        var complaints = new[]
        {
            new Complaint {
                Code = "COMP-001", Category = "Traffic services",
                SubjectEn = "Delay in driving licence issuance", SubjectAr = "تأخر في إصدار رخصة القيادة",
                BodyEn = "Submitted my driving licence renewal 14 days ago. Documents complete but no issuance yet.",
                BodyAr = "قدمت طلب تجديد رخصة القيادة قبل 14 يوماً ولم يصدر حتى الآن رغم اكتمال المستندات.",
                CustomerId = customer.Id, Status = ComplaintStatus.InProgress, Priority = Priority.High,
                Channel = "app", DownJourney = true,
                JourneyStageEn = "Status Tracking", JourneyStageAr = "متابعة الحالة",
                AssignedTo = agent.Id, OpenedAt = O(0), MonafasahRef = "MNF-A-1148",
            },
            new Complaint {
                Code = "COMP-002", Category = "Medical appointments",
                SubjectEn = "Specialist clinic appointment delay", SubjectAr = "تأخر موعد العيادة التخصصية",
                BodyEn = "My appointment has been postponed three times with no notice.",
                BodyAr = "تم تأجيل موعدي ثلاث مرات دون إشعار.",
                CustomerId = customer.Id, Status = ComplaintStatus.New, Priority = Priority.Normal,
                Channel = "whatsapp", OpenedAt = O(3),
            },
            new Complaint {
                Code = "COMP-003", Category = "Fees & payments",
                SubjectEn = "Incorrect residency fee calculation", SubjectAr = "خطأ في احتساب رسوم الإقامة",
                BodyEn = "Residency fee charged above the official rate. Receipt attached.",
                BodyAr = "تم احتساب رسوم الإقامة بمبلغ أعلى من الحد المقرر.",
                CustomerId = customer.Id, Status = ComplaintStatus.InProgress, Priority = Priority.High,
                Channel = "email", DownJourney = true,
                JourneyStageEn = "Payment", JourneyStageAr = "السداد",
                AssignedTo = supervisor.Id, OpenedAt = O(-4),
            },
            new Complaint {
                Code = "COMP-004", Category = "E-registration",
                SubjectEn = "Student e-registration issue", SubjectAr = "مشكلة في تسجيل الطالب الإلكتروني",
                BodyEn = "The system refuses to upload the birth certificate.",
                BodyAr = "لا يقبل النظام تحميل شهادة الميلاد.",
                CustomerId = customer.Id, Status = ComplaintStatus.Resolved, Priority = Priority.Normal,
                Channel = "web", DownJourney = true,
                JourneyStageEn = "Application Submission", JourneyStageAr = "تقديم الطلب",
                AssignedTo = agent.Id, OpenedAt = O(-6), ClosedAt = DateTime.UtcNow.AddDays(-2),
            },
            new Complaint {
                Code = "COMP-005", Category = "Staff conduct",
                SubjectEn = "Misconduct from branch service staff", SubjectAr = "إساءة من موظف خدمة في الفرع",
                BodyEn = "Experienced inappropriate conduct during my branch visit.",
                BodyAr = "تعرضت لتعامل غير لائق أثناء مراجعة الفرع.",
                CustomerId = customer.Id, Status = ComplaintStatus.InProgress, Priority = Priority.High,
                Channel = "phone", OpenedAt = O(-2),
            },
            new Complaint {
                Code = "COMP-006", Category = "Digital payments",
                SubjectEn = "Public-transport app rejects payment card", SubjectAr = "تطبيق النقل العام لا يقبل بطاقة الدفع",
                BodyEn = "Trying to top up my transit card — payment never completes.",
                BodyAr = "محاولة شحن البطاقة عبر التطبيق لا تكتمل.",
                CustomerId = customer.Id, Status = ComplaintStatus.New, Priority = Priority.Low,
                Channel = "twitter", OpenedAt = O(2),
            },
            new Complaint {
                Code = "COMP-007", Category = "Branch experience",
                SubjectEn = "Long wait time at branch", SubjectAr = "وقت انتظار طويل في الفرع",
                BodyEn = "Waited more than two hours without being called.",
                BodyAr = "انتظرت أكثر من ساعتين دون استدعاء الدور.",
                CustomerId = customer.Id, Status = ComplaintStatus.Closed, Priority = Priority.Low,
                Channel = "walkin", OpenedAt = O(-9), ClosedAt = DateTime.UtcNow.AddDays(-5),
            },
            new Complaint {
                Code = "COMP-008", Category = "Digital identity",
                SubjectEn = "Difficulty signing in via Nafath", SubjectAr = "صعوبة تسجيل الدخول إلى نفاذ",
                BodyEn = "Cannot sign in via Nafath for two days.",
                BodyAr = "لا أستطيع الدخول عبر نفاذ منذ يومين.",
                CustomerId = customer.Id, Status = ComplaintStatus.New, Priority = Priority.Normal,
                Channel = "web", DownJourney = true,
                JourneyStageEn = "Service Search", JourneyStageAr = "البحث عن الخدمة",
                OpenedAt = O(4),
            },
        };
        db.Complaints.AddRange(complaints);
        await db.SaveChangesAsync(ct);

        // ── Inbox threads (8: 3 email + 3 whatsapp + 2 chat) ───────────────────
        var now = DateTime.UtcNow;
        db.InboxThreads.AddRange(
            new InboxThread { Channel = InboxChannel.Email, FromAddress = "k.almutairi@example.com", FromName = "Khalid Al-Mutairi",
                Subject = "Service application stuck on submit",
                Body = "I tried to submit my new commercial-registry application this morning but the page froze after the OTP. The reference number never arrived by SMS.",
                Status = InboxStatus.New, Priority = Priority.High, ReceivedAt = now.AddHours(-2), CustomerId = customer.Id },
            new InboxThread { Channel = InboxChannel.Email, FromAddress = "f.alzahrani@example.com", FromName = "Fatima Al-Zahrani",
                Subject = "Question about the new tariff schedule",
                Body = "Could you confirm whether the published Q3 tariff schedule supersedes the one in your December circular?",
                Status = InboxStatus.Open, Priority = Priority.Normal, ReceivedAt = now.AddHours(-5) },
            new InboxThread { Channel = InboxChannel.Email, FromAddress = "m.alamri@example.com", FromName = "Mohammed Al-Amri",
                Subject = "Refund request — duplicate fee charge",
                Body = "Attached is the receipt for the duplicate residency-fee charge from 04 May. Please refund the second transaction.",
                Status = InboxStatus.Replied, Priority = Priority.High, ReceivedAt = now.AddDays(-1),
                RepliedAt = now.AddHours(-20),
                ReplySubject = "Re: Refund request — duplicate fee charge",
                ReplyBody = "Thank you for the receipt. The refund will be processed within five working days." },
            new InboxThread { Channel = InboxChannel.WhatsApp, FromAddress = "+966 50 123 4567", FromName = "Aisha Al-Dosari",
                Body = "ما زلت أنتظر تحديث طلب التسجيل منذ يومين، هل من تحديث؟",
                Status = InboxStatus.New, Priority = Priority.Normal, ReceivedAt = now.AddMinutes(-30) },
            new InboxThread { Channel = InboxChannel.WhatsApp, FromAddress = "+966 55 765 4321", FromName = "Sara Al-Shehri",
                Body = "Hello, can someone confirm whether tomorrow's appointment window is still 09:00–11:00?",
                Status = InboxStatus.Open, Priority = Priority.Low, ReceivedAt = now.AddHours(-3) },
            new InboxThread { Channel = InboxChannel.WhatsApp, FromAddress = "+966 53 999 8888", FromName = "Abdullah Al-Subaie",
                Body = "تم رفض الدفع مرتين في تطبيق النقل. أحتاج مساعدة عاجلة.",
                Status = InboxStatus.New, Priority = Priority.High, ReceivedAt = now.AddMinutes(-15) },
            new InboxThread { Channel = InboxChannel.Chat, FromAddress = "chat-anon-7f2c", FromName = "Anonymous visitor",
                Body = "Hi — where can I find the latest published competition guidelines?",
                Status = InboxStatus.New, Priority = Priority.Normal, ReceivedAt = now.AddMinutes(-10) },
            new InboxThread { Channel = InboxChannel.Chat, FromAddress = "chat-anon-91d3", FromName = "Anonymous visitor",
                Body = "Quick one: is the office open on Thursday afternoons?",
                Status = InboxStatus.Replied, Priority = Priority.Low, ReceivedAt = now.AddDays(-1),
                RepliedAt = now.AddDays(-1).AddMinutes(6),
                ReplyBody = "Yes — our offices are open Sunday through Thursday, 08:00–16:00. Closed on weekends." }
        );

        // ── Contact channels (3) ─────────────────────────────────────────────
        db.ContactChannels.AddRange(
            new ContactChannel { Key = "whatsapp", Value = "+966 11 000 0000", UpdatedBy = admin.Id },
            new ContactChannel { Key = "info_email", Value = "info@cx.gov.sa", UpdatedBy = admin.Id },
            new ContactChannel { Key = "support_hours", Value = "24/7", UpdatedBy = admin.Id }
        );

        // ── Notifications (5 for admin) ──────────────────────────────────────
        db.Notifications.AddRange(
            new Notification { UserId = admin.Id, TitleEn = "SLA breach on COMP-003", TitleAr = "تجاوز مستوى الخدمة في COMP-003", BodyEn = "Case has been open beyond the urgent SLA window.", BodyAr = "تجاوز الحالة نافذة المهلة العاجلة.", Kind = "warning" },
            new Notification { UserId = admin.Id, TitleEn = "New complaint via WhatsApp", TitleAr = "شكوى جديدة عبر واتساب", BodyEn = "COMP-008 was logged from Nafath sign-in difficulty.", BodyAr = "تم تسجيل COMP-008 لصعوبة الدخول عبر نفاذ.", Kind = "info" },
            new Notification { UserId = admin.Id, TitleEn = "Survey response — Branch experience", TitleAr = "ردّ استبيان — تجربة الفرع", BodyEn = "Three new responses ready for closed-loop follow-up.", BodyAr = "ثلاثة ردود جديدة جاهزة للمتابعة.", Kind = "info" },
            new Notification { UserId = admin.Id, TitleEn = "Knowledge article published", TitleAr = "نشر مقال جديد", BodyEn = "Nafath sign-in troubleshooting was published.", BodyAr = "تم نشر مقال حول حل مشكلات نفاذ.", Kind = "success" },
            new Notification { UserId = admin.Id, TitleEn = "Audit chain verified", TitleAr = "تم التحقق من سلسلة التدقيق", BodyEn = "0 mismatches on the last daily verify.", BodyAr = "0 اختلاف في آخر تحقق يومي.", Kind = "success" }
        );

        // ── Genesis audit event ──────────────────────────────────────────────
        var genesisPayload = JsonSerializer.Serialize(new
        {
            kind = "system.boot",
            actor = "system",
            note = "CX Platform v2 — Phase 0 genesis entry",
            at = DateTime.UtcNow,
        });
        var genesisHash = HashChain.ComputeEntryHash(HashChain.Genesis, genesisPayload);
        db.AuditEvents.Add(new AuditEvent
        {
            Kind = "system.boot",
            TargetKind = "system",
            PrevHash = HashChain.Genesis,
            EntryHash = genesisHash,
            PayloadJson = genesisPayload,
            At = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);
    }
}

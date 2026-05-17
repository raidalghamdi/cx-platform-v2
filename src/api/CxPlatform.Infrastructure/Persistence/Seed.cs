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
            // Round 5 — maturity-model pages
            "/accessibility", "/service-health", "/improvement", "/cx-analytics", "/content-governance",
        };
        // matrix[role][page] = allowed. Page-level access is binary (the
        // RolePermission table doesn't model read/write). Controller-level
        // [Authorize(Roles=...)] enforces write privileges separately.
        //
        // Phase 1 grants (per brief): journeys/voc admin+supervisor+quality+executive+agent;
        // kb everyone except customer-write; programme exec+admin+supervisor+quality;
        // governance admin+supervisor+quality+executive.
        // Phase 2 grants (per brief): /about everyone read; /architecture all
        // non-customer; /portal admin+customer; /copilot all non-customer;
        // /audit admin+supervisor+quality+executive; /automation admin+
        // supervisor+executive.
        // Round 5 grants (per brief):
        //   /accessibility       — admin, supervisor, agent, quality, customer (read), executive
        //   /service-health      — admin, supervisor, agent (read), quality, executive (read)
        //   /improvement         — admin, supervisor, agent (read), quality, executive
        //   /cx-analytics        — admin, supervisor, agent (read), quality, executive
        //   /content-governance  — admin, supervisor, agent, quality, executive (read)
        var matrix = new Dictionary<string, HashSet<string>>
        {
            ["admin"]      = new(pages),
            ["supervisor"] = new(new[]{"/about","/dashboard","/journeys","/voc","/complaints","/inbox","/kb","/copilot","/programme","/governance","/architecture","/audit","/automation","/notifications","/profile",
                                       "/accessibility","/service-health","/improvement","/cx-analytics","/content-governance"}),
            ["agent"]      = new(new[]{"/about","/architecture","/journeys","/complaints","/inbox","/kb","/copilot","/notifications","/profile",
                                       "/accessibility","/service-health","/improvement","/cx-analytics","/content-governance"}),
            ["quality"]    = new(new[]{"/about","/architecture","/journeys","/voc","/complaints","/kb","/programme","/governance","/audit","/copilot","/notifications","/profile",
                                       "/accessibility","/service-health","/improvement","/cx-analytics","/content-governance"}),
            ["customer"]   = new(new[]{"/about","/portal","/kb","/notifications","/profile","/accessibility"}),
            ["executive"]  = new(new[]{"/about","/architecture","/dashboard","/journeys","/voc","/kb","/programme","/governance","/audit","/automation","/copilot","/notifications","/profile",
                                       "/accessibility","/service-health","/improvement","/cx-analytics","/content-governance"}),
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

        // ── Phase 1: Journeys (4 with stages) ─────────────────────────────
        var journeys = new[]
        {
            new Journey { NameEn = "New customer onboarding", NameAr = "إعداد المستفيد الجديد",
                Persona = "Citizen — Onboarding", Status = "active", StageCount = 4 },
            new Journey { NameEn = "Complaint resolution",    NameAr = "حل الشكوى",
                Persona = "Citizen — Service",    Status = "active", StageCount = 3 },
            new Journey { NameEn = "Commercial registry renewal", NameAr = "تجديد السجل التجاري",
                Persona = "Business — Renewal",   Status = "active", StageCount = 4 },
            new Journey { NameEn = "Public consultation participation", NameAr = "المشاركة في الاستشارة العامة",
                Persona = "Citizen — Tafa3al",    Status = "draft",  StageCount = 3 },
        };
        db.Journeys.AddRange(journeys);
        await db.SaveChangesAsync(ct);

        db.JourneyStages.AddRange(
            new JourneyStage { JourneyId = journeys[0].Id, Sequence = 1, NameEn = "Awareness", NameAr = "الاطلاع",
                TouchpointEn = "Web search / national portal", TouchpointAr = "البحث عبر الإنترنت / البوابة الوطنية",
                PainPointEn = "Hard to discover which authority owns the service.",
                PainPointAr = "صعوبة معرفة الجهة المسؤولة عن الخدمة.", EmotionScore = -1 },
            new JourneyStage { JourneyId = journeys[0].Id, Sequence = 2, NameEn = "Sign-in via Nafath", NameAr = "الدخول عبر نفاذ",
                TouchpointEn = "Nafath app", TouchpointAr = "تطبيق نفاذ",
                PainPointEn = "Timeout if the OTP arrives late.", PainPointAr = "انتهاء الجلسة عند تأخر رمز التحقق.",
                EmotionScore = 0 },
            new JourneyStage { JourneyId = journeys[0].Id, Sequence = 3, NameEn = "Profile completion", NameAr = "استكمال الملف",
                TouchpointEn = "Portal profile form", TouchpointAr = "نموذج الملف في البوابة",
                PainPointEn = "Repeats data already in national registry.",
                PainPointAr = "تكرار بيانات موجودة في السجل الوطني.", EmotionScore = -1 },
            new JourneyStage { JourneyId = journeys[0].Id, Sequence = 4, NameEn = "First service request", NameAr = "أول طلب خدمة",
                TouchpointEn = "Service catalog", TouchpointAr = "كتالوج الخدمات",
                PainPointEn = "No clear ETA on the request.", PainPointAr = "لا يوجد وقت إنجاز واضح للطلب.",
                EmotionScore = 1 },

            new JourneyStage { JourneyId = journeys[1].Id, Sequence = 1, NameEn = "Lodge complaint", NameAr = "تقديم الشكوى",
                TouchpointEn = "Portal / WhatsApp / Branch", TouchpointAr = "البوابة / واتساب / الفرع",
                PainPointEn = "Unclear which channel resolves fastest.",
                PainPointAr = "غير واضح أي قناة الأسرع في الرد.", EmotionScore = -2 },
            new JourneyStage { JourneyId = journeys[1].Id, Sequence = 2, NameEn = "Triage & investigation", NameAr = "الفرز والتحقيق",
                TouchpointEn = "Internal queue", TouchpointAr = "قائمة العمل الداخلية",
                PainPointEn = "Customer waits without status updates.",
                PainPointAr = "ينتظر المستفيد بدون تحديثات.", EmotionScore = -1 },
            new JourneyStage { JourneyId = journeys[1].Id, Sequence = 3, NameEn = "Closure & follow-up", NameAr = "الإغلاق والمتابعة",
                TouchpointEn = "Email / SMS", TouchpointAr = "البريد / الرسائل القصيرة",
                PainPointEn = "No closed-loop satisfaction check.",
                PainPointAr = "لا يوجد تأكد من رضا المستفيد بعد الإغلاق.", EmotionScore = 0 },

            new JourneyStage { JourneyId = journeys[2].Id, Sequence = 1, NameEn = "Reminder", NameAr = "تذكير",
                TouchpointEn = "SMS / email 60 days out", TouchpointAr = "رسالة قبل 60 يوماً",
                PainPointEn = "No reminder if mobile number is outdated.",
                PainPointAr = "لا يصل التذكير عند تغيير رقم الجوال.", EmotionScore = 0 },
            new JourneyStage { JourneyId = journeys[2].Id, Sequence = 2, NameEn = "Payment", NameAr = "السداد",
                TouchpointEn = "SADAD invoice", TouchpointAr = "فاتورة سداد",
                PainPointEn = "Double-charge edge case on retried payments.",
                PainPointAr = "احتمال خصم مزدوج عند إعادة المحاولة.", EmotionScore = -1 },
            new JourneyStage { JourneyId = journeys[2].Id, Sequence = 3, NameEn = "Certificate issuance", NameAr = "إصدار الشهادة",
                TouchpointEn = "Portal download", TouchpointAr = "تحميل من البوابة",
                PainPointEn = "Certificate PDF not Arabic-tagged for screen readers.",
                PainPointAr = "ملف PDF بدون وسوم عربية للقارئات.", EmotionScore = 1 },
            new JourneyStage { JourneyId = journeys[2].Id, Sequence = 4, NameEn = "Verification", NameAr = "التحقق",
                TouchpointEn = "QR code on certificate", TouchpointAr = "رمز QR على الشهادة",
                PainPointEn = "Third parties don't always trust the QR.",
                PainPointAr = "جهات خارجية أحياناً لا تثق برمز التحقق.", EmotionScore = 1 },

            new JourneyStage { JourneyId = journeys[3].Id, Sequence = 1, NameEn = "Discover consultation", NameAr = "اكتشاف الاستشارة",
                TouchpointEn = "Tafa3al portal", TouchpointAr = "بوابة تفاعل",
                PainPointEn = "Discoverability low without push.",
                PainPointAr = "اكتشاف ضعيف بدون إشعار.", EmotionScore = 0 },
            new JourneyStage { JourneyId = journeys[3].Id, Sequence = 2, NameEn = "Submit feedback", NameAr = "إرسال الرأي",
                TouchpointEn = "Tafa3al form", TouchpointAr = "نموذج تفاعل",
                PainPointEn = "Form is long; mobile UX rough.",
                PainPointAr = "النموذج طويل على الجوال.", EmotionScore = -1 },
            new JourneyStage { JourneyId = journeys[3].Id, Sequence = 3, NameEn = "Outcome feedback", NameAr = "الإفادة بالنتيجة",
                TouchpointEn = "Public response page", TouchpointAr = "صفحة الردود العامة",
                PainPointEn = "Citizens don't always receive a closing summary.",
                PainPointAr = "لا يصل دائماً ملخص ختامي للمشارك.", EmotionScore = 0 }
        );

        // ── Phase 1: VoC responses (4) ─────────────────────────────────────
        var nowVoc = DateTime.UtcNow;
        db.VocResponses.AddRange(
            new VocResponse { SurveyEn = "Service satisfaction Q1", SurveyAr = "رضا الخدمة — الربع الأول",
                Channel = "email", NpsScore = 9, Sentiment = "positive",
                CommentEn = "Smooth experience overall, especially the Nafath sign-in.",
                CommentAr = "تجربة سلسة عموماً، خصوصاً الدخول عبر نفاذ.",
                RespondedAt = nowVoc.AddDays(-3), CustomerName = "Khalid Al-Mutairi" },
            new VocResponse { SurveyEn = "Service satisfaction Q1", SurveyAr = "رضا الخدمة — الربع الأول",
                Channel = "whatsapp", NpsScore = 4, Sentiment = "negative",
                CommentEn = "Waited two weeks for the licence renewal, no clear status.",
                CommentAr = "انتظرت أسبوعين لتجديد الرخصة دون توضيح للحالة.",
                RespondedAt = nowVoc.AddDays(-1), CustomerName = "Fatima Al-Zahrani" },
            new VocResponse { SurveyEn = "Branch experience", SurveyAr = "تجربة الفرع",
                Channel = "branch", NpsScore = 7, Sentiment = "neutral",
                CommentEn = "Friendly staff but the wait time was long.",
                CommentAr = "الموظفون متعاونون لكن وقت الانتظار طويل.",
                RespondedAt = nowVoc.AddHours(-8), CustomerName = "Mohammed Al-Amri" },
            new VocResponse { SurveyEn = "Portal usability", SurveyAr = "سهولة استخدام البوابة",
                Channel = "portal", NpsScore = 10, Sentiment = "positive",
                CommentEn = "The complaint workflow is much clearer now.",
                CommentAr = "تدفق تقديم الشكوى أوضح بكثير الآن.",
                RespondedAt = nowVoc.AddHours(-2), CustomerName = "Aisha Al-Dosari" }
        );

        // ── Phase 1: KB articles (4) ───────────────────────────────────────
        db.KbArticles.AddRange(
            new KbArticle { TitleEn = "How to lodge a complaint", TitleAr = "كيفية تقديم شكوى",
                Category = "complaints", AuthorId = admin.Id, Status = "published",
                BodyEn = "1) Sign in with Nafath. 2) Open Complaints → New. 3) Pick category. 4) Submit. You will receive a reference number.",
                BodyAr = "١) سجّل الدخول عبر نفاذ. ٢) افتح الشكاوى ← جديدة. ٣) اختر الفئة. ٤) أرسل. ستصلك رسالة بالرقم المرجعي." },
            new KbArticle { TitleEn = "Reset your Nafath password", TitleAr = "إعادة تعيين كلمة مرور نفاذ",
                Category = "identity", AuthorId = admin.Id, Status = "published",
                BodyEn = "Open the Nafath app, tap 'Forgot password', verify by mobile, set a new password.",
                BodyAr = "افتح تطبيق نفاذ، اضغط «نسيت كلمة المرور»، تحقق برقم الجوال، ثم أنشئ كلمة مرور جديدة." },
            new KbArticle { TitleEn = "Renew commercial registry online", TitleAr = "تجديد السجل التجاري إلكترونياً",
                Category = "services", AuthorId = admin.Id, Status = "published",
                BodyEn = "Use the Commerce portal: sign in, select your registry, settle the SADAD invoice, download the certificate.",
                BodyAr = "استخدم بوابة التجارة: سجّل الدخول، اختر السجل، سدد فاتورة سداد، ثم نزّل الشهادة." },
            new KbArticle { TitleEn = "Understanding closed-loop follow-up", TitleAr = "متابعة الحلقة المغلقة",
                Category = "voc", AuthorId = admin.Id, Status = "draft",
                BodyEn = "After a low score we contact the customer within five working days with a remediation plan.",
                BodyAr = "بعد التقييم المنخفض نتواصل مع المستفيد خلال خمسة أيام عمل لإبلاغه بالخطة." }
        );

        // ── Phase 1: Programme initiatives (4) ─────────────────────────────
        db.ProgrammeInitiatives.AddRange(
            new ProgrammeInitiative {
                NameEn = "Monafasah+ API integration", NameAr = "تكامل منافسة+ عبر الـ API",
                Owner = "Fatima Al-Otaibi", RagStatus = "green", ProgressPct = 75,
                StartDate = DateTime.UtcNow.AddMonths(-3), TargetDate = DateTime.UtcNow.AddMonths(2),
                Notes = "Phase 0 → 1 wiring complete; production cutover in May." },
            new ProgrammeInitiative {
                NameEn = "Closed-loop VoC automation", NameAr = "أتمتة الحلقة المغلقة لصوت المستفيد",
                Owner = "Layla Al-Qahtani", RagStatus = "amber", ProgressPct = 40,
                StartDate = DateTime.UtcNow.AddMonths(-2), TargetDate = DateTime.UtcNow.AddMonths(3),
                Notes = "Templates and routing rules under review with Quality." },
            new ProgrammeInitiative {
                NameEn = "Knowledge base bilingual refresh", NameAr = "تحديث قاعدة المعرفة ثنائية اللغة",
                Owner = "Ahmed Al-Harbi", RagStatus = "green", ProgressPct = 60,
                StartDate = DateTime.UtcNow.AddMonths(-1), TargetDate = DateTime.UtcNow.AddMonths(1),
                Notes = "20 of 35 articles translated; reviewers assigned." },
            new ProgrammeInitiative {
                NameEn = "Branch wait-time reduction", NameAr = "خفض زمن الانتظار في الفروع",
                Owner = "Raid Al-Ghamdi", RagStatus = "red", ProgressPct = 15,
                StartDate = DateTime.UtcNow.AddMonths(-2), TargetDate = DateTime.UtcNow.AddMonths(2),
                Notes = "Pilot ticketing system rollout slipped — vendor lead-time issue." }
        );

        // ── Phase 1: Governance bodies + decisions (4 bodies, 4 decisions) ─
        var bodies = new[]
        {
            new GovernanceBody {
                NameEn = "CX Steering Committee", NameAr = "اللجنة التوجيهية لتجربة المستفيد",
                Cadence = "monthly", Chair = "Raid Al-Ghamdi",
                MembersJson = "[\"Raid Al-Ghamdi\",\"Fatima Al-Otaibi\",\"Layla Al-Qahtani\",\"Noor Al Noor\"]",
                CharterUrl = "https://internal.gac.gov.sa/charters/cx-steering.pdf" },
            new GovernanceBody {
                NameEn = "Data & Privacy Council", NameAr = "مجلس البيانات والخصوصية",
                Cadence = "quarterly", Chair = "Noor Al Noor",
                MembersJson = "[\"Noor Al Noor\",\"Layla Al-Qahtani\",\"Mohammed Al-Amri\"]",
                CharterUrl = null },
            new GovernanceBody {
                NameEn = "Operations Review Forum", NameAr = "منتدى مراجعة العمليات",
                Cadence = "biweekly", Chair = "Fatima Al-Otaibi",
                MembersJson = "[\"Fatima Al-Otaibi\",\"Ahmed Al-Harbi\",\"Aisha Al-Dosari\"]",
                CharterUrl = null },
            new GovernanceBody {
                NameEn = "Customer Council", NameAr = "مجلس المستفيدين",
                Cadence = "quarterly", Chair = "Layla Al-Qahtani",
                MembersJson = "[\"Layla Al-Qahtani\",\"Khalid Al-Mutairi\",\"Fatima Al-Zahrani\"]",
                CharterUrl = null }
        };
        db.GovernanceBodies.AddRange(bodies);
        await db.SaveChangesAsync(ct);

        db.GovernanceDecisions.AddRange(
            new GovernanceDecision { BodyId = bodies[0].Id,
                TitleEn = "Approve Monafasah+ rollout plan", TitleAr = "إقرار خطة إطلاق منافسة+",
                Decision = "Approved with a staged go-live starting May.",
                OwnerEn = "Fatima Al-Otaibi", OwnerAr = "فاطمة العتيبي",
                DueDate = DateTime.UtcNow.AddMonths(2), DecidedAt = DateTime.UtcNow.AddDays(-5) },
            new GovernanceDecision { BodyId = bodies[0].Id,
                TitleEn = "Lock GAC visual identity for v2", TitleAr = "اعتماد الهوية البصرية للنسخة الثانية",
                Decision = "Approved gold/navy/blue palette; pill buttons; 10px cards.",
                OwnerEn = "Noor Al Noor", OwnerAr = "نور النور",
                DueDate = null, DecidedAt = DateTime.UtcNow.AddDays(-20) },
            new GovernanceDecision { BodyId = bodies[1].Id,
                TitleEn = "Adopt PDPL-aligned retention policy", TitleAr = "اعتماد سياسة الاحتفاظ بالبيانات",
                Decision = "12-month default retention; right-to-be-forgotten workflow in Q3.",
                OwnerEn = "Noor Al Noor", OwnerAr = "نور النور",
                DueDate = DateTime.UtcNow.AddMonths(3), DecidedAt = DateTime.UtcNow.AddDays(-30) },
            new GovernanceDecision { BodyId = bodies[2].Id,
                TitleEn = "Reduce branch wait-time SLA to 12 minutes", TitleAr = "تقليص مهلة الانتظار في الفروع إلى 12 دقيقة",
                Decision = "Approved subject to ticketing rollout.",
                OwnerEn = "Ahmed Al-Harbi", OwnerAr = "أحمد الحربي",
                DueDate = DateTime.UtcNow.AddMonths(1), DecidedAt = DateTime.UtcNow.AddDays(-2) }
        );

        await db.SaveChangesAsync(ct);

        // ── Phase 2: About sections (4) ────────────────────────────────────
        db.AboutSections.AddRange(
            new AboutSection { OrderIndex = 1,
                KeyEn = "Our story", KeyAr = "قصتنا",
                BodyEn = "The GAC Customer Experience programme was founded to centralise complaints, surveys, and customer journeys across all service touchpoints. The platform you are using is the second-generation rebuild on the GAC mandatory stack.",
                BodyAr = "أُسس برنامج تجربة المستفيد في الهيئة العامة للمنافسة لتوحيد الشكاوى والاستبيانات ورحلات المستفيد عبر جميع نقاط التماس. هذه المنصة هي الإصدار الثاني المعاد بناؤه على الكدسة التقنية المعتمدة من الهيئة." },
            new AboutSection { OrderIndex = 2,
                KeyEn = "Vision 2030 alignment", KeyAr = "المواءمة مع رؤية 2030",
                BodyEn = "Our work supports the Vision 2030 Quality of Life programme by improving public service responsiveness and transparency, in line with the Digital Government Authority's customer-experience maturity model.",
                BodyAr = "تدعم أعمالنا برنامج جودة الحياة ضمن رؤية 2030 من خلال تحسين سرعة الاستجابة وشفافية الخدمات العامة، بما يتوافق مع نموذج نضج تجربة المستفيد لهيئة الحكومة الرقمية." },
            new AboutSection { OrderIndex = 3,
                KeyEn = "How we work", KeyAr = "كيف نعمل",
                BodyEn = "We run on Angular 17, ASP.NET Core 8, MySQL 8 and YARP. Every mutation is captured in a hash-chained audit log so investigators can prove what changed, by whom, and when.",
                BodyAr = "نشغّل النظام على Angular 17 و ASP.NET Core 8 و MySQL 8 و YARP. كل تعديل يُسجَّل في سجل تدقيق مرتبط بسلسلة هاش بحيث يمكن للمدققين إثبات ما تغيّر ومن غيّره ومتى." },
            new AboutSection { OrderIndex = 4,
                KeyEn = "Team", KeyAr = "الفريق",
                BodyEn = "Noor Al Noor (System Administrator), Raid Al-Ghamdi (Chief of Strategy & Excellence), Fatima Al-Otaibi (CX Supervisor), Layla Al-Qahtani (Quality Officer), Ahmed Al-Harbi (Service Agent).",
                BodyAr = "نور النور (مسؤول النظام)، رائد الغامدي (رئيس الاستراتيجية والتميز)، فاطمة العتيبي (مشرفة تجربة المستفيد)، ليلى القحطاني (مسؤولة الجودة)، أحمد الحربي (موظف خدمة المستفيدين)." }
        );

        // ── Phase 2: Automation rules (4) ──────────────────────────────────
        db.AutomationRules.AddRange(
            new AutomationRule { NameEn = "Auto-acknowledge new complaint", NameAr = "إشعار تلقائي للشكوى الجديدة",
                TriggerType = "complaint.created",
                ConditionJson = "{\"any\": true}",
                ActionType = "notify",
                Enabled = true, LastRunAt = DateTime.UtcNow.AddMinutes(-15),
                LastRunStatus = "success", RunCount = 124 },
            new AutomationRule { NameEn = "Escalate detractor VoC response", NameAr = "تصعيد ردود VoC السلبية",
                TriggerType = "voc.negative",
                ConditionJson = "{\"nps_lte\": 6}",
                ActionType = "escalate",
                Enabled = true, LastRunAt = DateTime.UtcNow.AddHours(-3),
                LastRunStatus = "success", RunCount = 17 },
            new AutomationRule { NameEn = "Daily inbox triage", NameAr = "فرز يومي للصندوق",
                TriggerType = "scheduled",
                ConditionJson = "{\"cron\": \"0 8 * * *\"}",
                ActionType = "assign",
                Enabled = true, LastRunAt = DateTime.UtcNow.Date.AddHours(8),
                LastRunStatus = "success", RunCount = 31 },
            new AutomationRule { NameEn = "Publish reviewed KB drafts", NameAr = "نشر مقالات المعرفة المعتمدة",
                TriggerType = "scheduled",
                ConditionJson = "{\"cron\": \"0 12 * * 1\"}",
                ActionType = "kb_publish",
                Enabled = false, LastRunAt = null,
                LastRunStatus = "never", RunCount = 0 }
        );

        // ── Phase 2: Portal requests for the demo customer (4) ─────────────
        db.PortalRequests.AddRange(
            new PortalRequest { CustomerId = customer.Id, Type = "complaint",
                SubjectEn = "Delay in licence renewal", SubjectAr = "تأخر تجديد الرخصة",
                BodyEn = "Submitted 14 days ago, no update yet despite complete documents.",
                BodyAr = "قدمت قبل 14 يوماً ولا يوجد تحديث رغم اكتمال المستندات.",
                Status = "in_progress", CreatedAt = DateTime.UtcNow.AddDays(-9) },
            new PortalRequest { CustomerId = customer.Id, Type = "inquiry",
                SubjectEn = "How do I update my registered mobile number?", SubjectAr = "كيف أحدّث رقم الجوال المسجل؟",
                BodyEn = "I changed my number and need to update it on my account.",
                BodyAr = "غيّرت رقمي وأرغب في تحديثه في الحساب.",
                Status = "resolved", CreatedAt = DateTime.UtcNow.AddDays(-22) },
            new PortalRequest { CustomerId = customer.Id, Type = "appointment",
                SubjectEn = "Request branch appointment", SubjectAr = "طلب موعد في الفرع",
                BodyEn = "Need an in-person visit for original document submission.",
                BodyAr = "أحتاج زيارة الفرع لتسليم الوثائق الأصلية.",
                Status = "new", CreatedAt = DateTime.UtcNow.AddHours(-4) },
            new PortalRequest { CustomerId = customer.Id, Type = "complaint",
                SubjectEn = "Tariff invoice mismatch", SubjectAr = "اختلاف في الفاتورة",
                BodyEn = "Invoiced amount differs from published tariff.",
                BodyAr = "المبلغ في الفاتورة يختلف عن التعرفة المنشورة.",
                Status = "closed", CreatedAt = DateTime.UtcNow.AddDays(-40) }
        );

        // ── Phase 2: Copilot interactions — short history (4) ──────────────
        db.CopilotInteractions.AddRange(
            new CopilotInteraction { UserId = supervisor.Id, Intent = "summarise",
                PromptEn = "Summarise the open complaints from this week.",
                PromptAr = "لخّص الشكاوى المفتوحة لهذا الأسبوع.",
                ResponseEn = "8 open complaints — 3 high priority. Top theme: licence renewal delays.",
                ResponseAr = "8 شكاوى مفتوحة — 3 ذات أولوية عالية. الموضوع الأبرز: تأخر تجديد الرخص.",
                LatencyMs = 1120, Success = true,
                CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new CopilotInteraction { UserId = agent.Id, Intent = "draft_reply",
                PromptEn = "Draft a reply to a customer waiting on licence renewal.",
                PromptAr = "صغ رداً لمستفيد ينتظر تجديد رخصة القيادة.",
                ResponseEn = "Thank you for your patience. We have escalated your request to the issuing authority and will update you within 48 hours.",
                ResponseAr = "شكراً لصبرك. تم تصعيد طلبك إلى الجهة المُصدِرة وسنوافيك بتحديث خلال 48 ساعة.",
                LatencyMs = 980, Success = true,
                CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new CopilotInteraction { UserId = quality.Id, Intent = "find_similar",
                PromptEn = "Find complaints similar to COMP-003.",
                PromptAr = "ابحث عن شكاوى مشابهة لـ COMP-003.",
                ResponseEn = "3 similar complaints found this quarter — all relate to residency fee miscalculation.",
                ResponseAr = "وجدت 3 شكاوى مشابهة هذا الربع — جميعها تتعلق باحتساب رسوم الإقامة.",
                LatencyMs = 1320, Success = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
            new CopilotInteraction { UserId = admin.Id, Intent = "ask",
                PromptEn = "What is our current SLA compliance?",
                PromptAr = "ما نسبة الالتزام الحالية بمستوى الخدمة؟",
                ResponseEn = "91.2% (down 0.6 points week-over-week). The dip is concentrated in payment-related complaints.",
                ResponseAr = "91.2% (انخفاض 0.6 نقطة عن الأسبوع الماضي). التراجع مركّز في الشكاوى المتعلقة بالمدفوعات.",
                LatencyMs = 1080, Success = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-12) }
        );

        await db.SaveChangesAsync(ct);

        // ──────────────────────────────────────────────────────────────────
        // Round 5 — maturity-model seed
        // ──────────────────────────────────────────────────────────────────
        var nowR5 = DateTime.UtcNow;
        var today = nowR5.Date;
        var rng = new Random(20260517);                            // deterministic demo data

        // Gap 1 — Accessibility audit + 12 remediation items
        var audit = new AccessibilityAuditEntry
        {
            AuditDate = today.AddDays(-21),
            Auditor = "Layla Al-Qahtani",
            ScopePagesJson = JsonSerializer.Serialize(new[] {
                "/dashboard", "/complaints", "/inbox", "/portal", "/kb", "/landing"
            }),
            WcagLevel = WcagLevel.AA,
            TotalIssues = 12, OpenIssues = 3,
            ReportUrl = "https://internal.gac.gov.sa/audits/2026-04-wcag-aa.pdf",
            Notes = "Quarterly WCAG 2.2 AA review across the customer-facing pages.",
        };
        db.AccessibilityAudits.Add(audit);
        await db.SaveChangesAsync(ct);

        var remediationData = new (string Crit, AccessibilitySeverity Sev, string En, string Ar, AccessibilityItemStatus St, int? DueDays)[]
        {
            ("1.4.3",  AccessibilitySeverity.High,     "Text contrast on muted captions below 4.5:1.",                              "تباين نص الشروحات الخفيفة أقل من 4.5:1.",                                 AccessibilityItemStatus.Resolved,   -10),
            ("1.4.11", AccessibilitySeverity.Medium,   "Pill button border vs background ratio 2.7:1 — needs 3:1.",                 "حد أزرار pill مقابل الخلفية 2.7:1 — المطلوب 3:1.",                         AccessibilityItemStatus.Resolved,    -5),
            ("2.1.1",  AccessibilitySeverity.High,     "Inbox drawer not fully keyboard-reachable when RTL is active.",             "اللوحة الجانبية للصندوق لا يمكن الوصول إليها بالكامل بلوحة المفاتيح في وضع RTL.", AccessibilityItemStatus.Resolved,    -3),
            ("2.4.7",  AccessibilitySeverity.Medium,   "Focus ring lost on gold pill buttons when pressed.",                        "اختفاء حلقة التركيز من أزرار pill الذهبية عند الضغط.",                       AccessibilityItemStatus.Resolved,    -1),
            ("2.5.8",  AccessibilitySeverity.Medium,   "Toast dismiss target smaller than 24×24 CSS pixels.",                       "هدف إغلاق التوست أصغر من 24×24 بكسل CSS.",                                  AccessibilityItemStatus.Resolved,    -2),
            ("3.1.2",  AccessibilitySeverity.Low,      "Mixed-language tooltips missing lang attribute on Arabic spans.",           "تلميحات ثنائية اللغة بدون سمة lang على فقرات العربية.",                      AccessibilityItemStatus.Resolved,    -7),
            ("4.1.2",  AccessibilitySeverity.Medium,   "Status badge announced only as text — needs aria-label including state.",   "شارة الحالة تُعلَن كنص فقط — تحتاج aria-label يشمل الحالة.",                  AccessibilityItemStatus.Resolved,    -4),
            ("4.1.3",  AccessibilitySeverity.Medium,   "Toast not announced to assistive tech.",                                    "التوست لا يُعلَن للتقنيات المساعدة.",                                        AccessibilityItemStatus.Resolved,     0),
            ("1.3.1",  AccessibilitySeverity.Low,      "Tables missing scope attribute on header cells.",                            "خلايا رأس الجداول تفتقد سمة scope.",                                       AccessibilityItemStatus.Resolved,    -1),
            ("1.1.1",  AccessibilitySeverity.Medium,   "Architecture SVG missing accessible name.",                                  "رسم البنية بصيغة SVG بلا اسم متاح.",                                        AccessibilityItemStatus.Open,        14),
            ("2.4.7",  AccessibilitySeverity.High,     "Skip-to-content link not visible on focus in RTL mode.",                     "رابط القفز إلى المحتوى غير ظاهر عند التركيز في وضع RTL.",                    AccessibilityItemStatus.InProgress,  10),
            ("3.3.7",  AccessibilitySeverity.Low,      "New-complaint form re-asks the customer's national ID.",                     "نموذج الشكوى الجديدة يطلب رقم الهوية مجدداً.",                                AccessibilityItemStatus.Open,        30),
        };
        foreach (var r in remediationData)
        {
            db.AccessibilityRemediations.Add(new AccessibilityRemediationItem
            {
                AuditId = audit.Id,
                WcagCriterion = r.Crit, Severity = r.Sev,
                DescriptionEn = r.En, DescriptionAr = r.Ar,
                Owner = "Layla Al-Qahtani",
                Status = r.St,
                TargetDate = r.DueDays is null ? null : today.AddDays(r.DueDays.Value),
                ResolvedDate = r.St == AccessibilityItemStatus.Resolved
                    ? today.AddDays(r.DueDays ?? -1) : null,
            });
        }
        await db.SaveChangesAsync(ct);

        // Gap 2 — 30 days of service-health metrics across 8 services
        string[] services = { "Auth", "Complaints", "Inbox", "Dashboard", "Portal", "Copilot", "Audit", "KB" };
        for (int day = 30; day >= 1; day--)
        {
            var ts = today.AddDays(-day).AddHours(12);
            foreach (var svc in services)
            {
                // Uptime 99.60–99.95, latency 80–450 ms, error rate 0.10–2.30 %
                var uptime = 99.60m + (decimal)(rng.NextDouble() * 0.35);
                var p95 = 80 + rng.Next(0, 371);
                var err = 0.10m + (decimal)(rng.NextDouble() * 2.20);
                var mttr = 5 + rng.Next(0, 36);
                var reqs = 1500 + rng.Next(0, 8500);
                db.ServiceHealthMetrics.Add(new ServiceHealthMetric
                {
                    ServiceName = svc, MeasuredAt = ts,
                    UptimePct = Math.Round(uptime, 4),
                    P95LatencyMs = p95,
                    ErrorRatePct = Math.Round(err, 4),
                    MttrMinutes = mttr, RequestCount = reqs,
                });
            }
        }

        // 3 incidents with realistic timelines
        db.ServiceIncidents.AddRange(
            new ServiceIncident
            {
                ServiceName = "Inbox",
                OpenedAt = today.AddDays(-12).AddHours(9).AddMinutes(14),
                ResolvedAt = today.AddDays(-12).AddHours(11).AddMinutes(2),
                Severity = IncidentSeverity.Sev2, Status = IncidentStatus.Resolved,
                TitleEn = "WhatsApp adapter degraded — 30% reply timeouts.",
                TitleAr = "تدنّي أداء واتساب — 30% من الردود تجاوزت المهلة.",
                RootCauseEn = "Upstream WhatsApp Business API rate-limit reduction not picked up by retry policy.",
                RootCauseAr = "خفض غير معلن لحدّ معدّل الطلبات في واجهة WhatsApp Business لم تلتقطه سياسة إعادة المحاولة.",
                RemediationEn = "Backed off retries and lifted into a circuit breaker; vendor confirmed limits.",
                RemediationAr = "تخفيف إعادة المحاولات وتغليفها بقاطع دائرة؛ تأكيد الحدود من المزوّد.",
            },
            new ServiceIncident
            {
                ServiceName = "Audit",
                OpenedAt = today.AddDays(-2).AddHours(7).AddMinutes(48),
                ResolvedAt = null,
                Severity = IncidentSeverity.Sev3, Status = IncidentStatus.Mitigating,
                TitleEn = "Audit verify endpoint p95 elevated under burst.",
                TitleAr = "ارتفاع p95 لنقطة التحقق من السجل تحت ضغط متفجّر.",
                RootCauseEn = "Full-table chain replay scales linearly; verify under load needs incremental verifier.",
                RootCauseAr = "إعادة بناء السلسلة الكاملة خطيّ النمو؛ يلزم محقّق تدريجي تحت الحمل.",
                RemediationEn = "Subagent 2 will add incremental verifier with cached tail offset.",
                RemediationAr = "ستضيف الموجة الثانية محقّقاً تدريجياً مع موقع إزاحة مخزّن.",
            },
            new ServiceIncident
            {
                ServiceName = "KB",
                OpenedAt = today.AddDays(-27).AddHours(15).AddMinutes(0),
                ResolvedAt = today.AddDays(-27).AddHours(15).AddMinutes(38),
                Severity = IncidentSeverity.Sev4, Status = IncidentStatus.Resolved,
                TitleEn = "KB search returned stale cache after publish.",
                TitleAr = "بحث قاعدة المعرفة عرض ذاكرة قديمة بعد النشر.",
                RootCauseEn = "Cache eviction on publish missed a tag.",
                RootCauseAr = "فاتت عملية إخلاء الذاكرة عند النشر إحدى الوسوم.",
                RemediationEn = "Tag-set extended to include category.",
                RemediationAr = "توسيع مجموعة الوسوم لتشمل الفئة.",
            }
        );

        // 6 synthetic checks (all enabled). LastRunAt/Latency are placeholders
        // until Subagent 2's BackgroundService starts overwriting them.
        var checkDefs = new (string Name, string Endpoint, int Interval, int Latency, CheckStatus Status)[]
        {
            ("Auth login",         "/api/v1/auth/login",          60, 142, CheckStatus.Pass),
            ("Complaints list",    "/api/v1/complaints",          60, 198, CheckStatus.Pass),
            ("Inbox threads",      "/api/v1/inbox/threads",       60, 221, CheckStatus.Pass),
            ("Dashboard KPIs",     "/api/v1/kpis",                60, 117, CheckStatus.Pass),
            ("Portal my-requests", "/api/v1/portal/my-requests", 120, 167, CheckStatus.Pass),
            ("Audit verify",       "/api/v1/audit/verify",       300, 612, CheckStatus.Pass),
        };
        foreach (var c in checkDefs)
        {
            db.SyntheticChecks.Add(new SyntheticCheck
            {
                Name = c.Name, Endpoint = c.Endpoint,
                IntervalSeconds = c.Interval,
                LastRunAt = nowR5.AddMinutes(-(rng.Next(0, c.Interval / 60 + 1))),
                LastStatus = c.Status, LastLatencyMs = c.Latency, Enabled = true,
            });
        }

        // Gap 3 — KPI thresholds tied to existing seeded KPIs
        var kpiRows = await db.Kpis.Where(k => new[] { "csat", "nps", "fcr", "sla", "response_time" }.Contains(k.Key))
            .ToDictionaryAsync(k => k.Key, ct);
        if (kpiRows.TryGetValue("csat", out var csatKpi))
            db.KpiThresholds.Add(new KpiThreshold { KpiId = csatKpi.Id, ThresholdValue = 80m, ComparisonOp = ThresholdComparison.LessThan, BreachAction = ThresholdBreachAction.Both, Enabled = true });
        if (kpiRows.TryGetValue("nps", out var npsKpi))
            db.KpiThresholds.Add(new KpiThreshold { KpiId = npsKpi.Id, ThresholdValue = 30m, ComparisonOp = ThresholdComparison.LessThan, BreachAction = ThresholdBreachAction.Both, Enabled = true });
        if (kpiRows.TryGetValue("fcr", out var fcrKpi))
            db.KpiThresholds.Add(new KpiThreshold { KpiId = fcrKpi.Id, ThresholdValue = 65m, ComparisonOp = ThresholdComparison.LessThan, BreachAction = ThresholdBreachAction.CreateImprovementItem, Enabled = true });
        if (kpiRows.TryGetValue("sla", out var slaKpi))
            db.KpiThresholds.Add(new KpiThreshold { KpiId = slaKpi.Id, ThresholdValue = 90m, ComparisonOp = ThresholdComparison.LessThan, BreachAction = ThresholdBreachAction.Both, Enabled = true });
        if (kpiRows.TryGetValue("response_time", out var rtKpi))
            db.KpiThresholds.Add(new KpiThreshold { KpiId = rtKpi.Id, ThresholdValue = 3m, ComparisonOp = ThresholdComparison.GreaterThan, BreachAction = ThresholdBreachAction.NotifyOnly, Enabled = true });

        // 8 ImprovementItems across the PDCA stages
        var improvements = new[]
        {
            new ImprovementItem { SourceType = ImprovementSource.KpiBreach, TitleEn = "Lift CSAT above 90 for branch journey", TitleAr = "رفع رضا المستفيد فوق 90 لرحلة الفرع",
                DescriptionEn = "CSAT for branch experience dipped below the 80 threshold last quarter; root-cause analysis points to wait time.",
                DescriptionAr = "تراجع رضا تجربة الفرع تحت عتبة 80 الربع الماضي؛ تشير الأسباب إلى وقت الانتظار.",
                Owner = "Fatima Al-Otaibi", Priority = ImprovementPriority.High, PdcaStage = PdcaStage.Plan,
                TargetDate = today.AddMonths(2), CreatedAt = today.AddDays(-9) },
            new ImprovementItem { SourceType = ImprovementSource.AccessibilityAudit, SourceRefId = audit.Id,
                TitleEn = "Fix RTL skip-link visibility (WCAG 2.4.7)", TitleAr = "تصحيح ظهور رابط القفز في وضع RTL (WCAG 2.4.7)",
                DescriptionEn = "Skip-to-content not visible on focus in RTL.",
                DescriptionAr = "رابط القفز إلى المحتوى غير ظاهر عند التركيز في وضع RTL.",
                Owner = "Ahmed Al-Harbi", Priority = ImprovementPriority.High, PdcaStage = PdcaStage.Plan,
                TargetDate = today.AddDays(14), CreatedAt = today.AddDays(-6) },
            new ImprovementItem { SourceType = ImprovementSource.Manual,
                TitleEn = "Reduce inbox p95 latency under burst", TitleAr = "خفض p95 لزمن الصندوق تحت الضغط",
                DescriptionEn = "Add adaptive concurrency to channel adapters.",
                DescriptionAr = "إضافة تحكّم تكيّفي بعدد الطلبات لمحولات القنوات.",
                Owner = "Fatima Al-Otaibi", Priority = ImprovementPriority.Medium, PdcaStage = PdcaStage.Do,
                TargetDate = today.AddMonths(1), CreatedAt = today.AddDays(-18) },
            new ImprovementItem { SourceType = ImprovementSource.ContentReview,
                TitleEn = "Refresh top-10 KB articles", TitleAr = "تحديث أعلى 10 مقالات بقاعدة المعرفة",
                DescriptionEn = "Bring freshness scores above 75 across the most-viewed articles.",
                DescriptionAr = "رفع درجة الحداثة فوق 75 لأكثر المقالات قراءة.",
                Owner = "Layla Al-Qahtani", Priority = ImprovementPriority.Medium, PdcaStage = PdcaStage.Do,
                TargetDate = today.AddMonths(1), CreatedAt = today.AddDays(-14) },
            new ImprovementItem { SourceType = ImprovementSource.KpiBreach,
                TitleEn = "Stabilise SLA compliance week-over-week", TitleAr = "تثبيت الالتزام بمستوى الخدمة أسبوعياً",
                DescriptionEn = "Trend dipped 0.6 points; check payment-related routing.",
                DescriptionAr = "تراجع الاتجاه 0.6 نقطة؛ مراجعة توجيه الشكاوى المتعلقة بالمدفوعات.",
                Owner = "Raid Al-Ghamdi", Priority = ImprovementPriority.High, PdcaStage = PdcaStage.Check,
                TargetDate = today.AddDays(20), CreatedAt = today.AddDays(-25) },
            new ImprovementItem { SourceType = ImprovementSource.Manual,
                TitleEn = "Roll out copilot draft-reply across agent inbox", TitleAr = "تعميم اقتراح الردود من المساعد على صندوق الموظفين",
                DescriptionEn = "After successful pilot, expand to all agents and measure CSAT delta.",
                DescriptionAr = "بعد نجاح التجربة، التعميم على جميع الموظفين وقياس فرق رضا المستفيد.",
                Owner = "Noor Al Noor", Priority = ImprovementPriority.Medium, PdcaStage = PdcaStage.Act,
                TargetDate = today.AddDays(7), CreatedAt = today.AddDays(-40) },
            new ImprovementItem { SourceType = ImprovementSource.AccessibilityAudit, SourceRefId = audit.Id,
                TitleEn = "Improve focus-ring contrast on gold pill buttons", TitleAr = "تحسين تباين حلقة التركيز لأزرار pill الذهبية",
                DescriptionEn = "Ring color updated to navy-700, ratio raised to 4.8:1.",
                DescriptionAr = "تعديل لون الحلقة إلى navy-700 ورفع التباين إلى 4.8:1.",
                Owner = "Ahmed Al-Harbi", Priority = ImprovementPriority.Medium, PdcaStage = PdcaStage.Closed,
                TargetDate = today.AddDays(-10), ClosedAt = today.AddDays(-5), CreatedAt = today.AddDays(-30) },
            new ImprovementItem { SourceType = ImprovementSource.Manual,
                TitleEn = "Document incident response runbook for inbox adapters", TitleAr = "توثيق دليل الاستجابة لحوادث محولات الصندوق",
                DescriptionEn = "After the Sev2 WhatsApp incident, codify the steps.",
                DescriptionAr = "بعد حادثة WhatsApp من المستوى الثاني، توحيد الخطوات.",
                Owner = "Fatima Al-Otaibi", Priority = ImprovementPriority.Low, PdcaStage = PdcaStage.Closed,
                TargetDate = today.AddDays(-2), ClosedAt = today.AddDays(-1), CreatedAt = today.AddDays(-22) },
        };
        db.ImprovementItems.AddRange(improvements);
        await db.SaveChangesAsync(ct);

        // PDCA log entries for items that have transitioned at least once
        db.PdcaCycleLogs.AddRange(
            new PdcaCycleLog { ImprovementItemId = improvements[2].Id, FromStage = PdcaStage.Plan, ToStage = PdcaStage.Do,
                ActorUserId = supervisor.Id, ChangedAt = today.AddDays(-14),
                NotesEn = "Scoped the work, kicked off implementation.", NotesAr = "تم تحديد النطاق وبدء التنفيذ." },
            new PdcaCycleLog { ImprovementItemId = improvements[3].Id, FromStage = PdcaStage.Plan, ToStage = PdcaStage.Do,
                ActorUserId = quality.Id, ChangedAt = today.AddDays(-10),
                NotesEn = "Article batch assigned to reviewers.", NotesAr = "تم إسناد دفعة المقالات إلى المراجعين." },
            new PdcaCycleLog { ImprovementItemId = improvements[4].Id, FromStage = PdcaStage.Plan, ToStage = PdcaStage.Do,
                ActorUserId = executive.Id, ChangedAt = today.AddDays(-20),
                NotesEn = "Hypothesis: payment routing.", NotesAr = "الفرضية: توجيه الشكاوى المتعلقة بالمدفوعات." },
            new PdcaCycleLog { ImprovementItemId = improvements[4].Id, FromStage = PdcaStage.Do, ToStage = PdcaStage.Check,
                ActorUserId = executive.Id, ChangedAt = today.AddDays(-7),
                NotesEn = "Measuring 14-day window.", NotesAr = "قياس نافذة 14 يوماً." },
            new PdcaCycleLog { ImprovementItemId = improvements[5].Id, FromStage = PdcaStage.Check, ToStage = PdcaStage.Act,
                ActorUserId = admin.Id, ChangedAt = today.AddDays(-3),
                NotesEn = "Pilot succeeded — rolling out to all agents.", NotesAr = "نجحت التجربة — التعميم على جميع الموظفين." },
            new PdcaCycleLog { ImprovementItemId = improvements[6].Id, FromStage = PdcaStage.Act, ToStage = PdcaStage.Closed,
                ActorUserId = admin.Id, ChangedAt = today.AddDays(-5),
                NotesEn = "Closed — measured contrast ratio at 4.8:1.", NotesAr = "تم الإغلاق — قياس التباين 4.8:1." },
            new PdcaCycleLog { ImprovementItemId = improvements[7].Id, FromStage = PdcaStage.Act, ToStage = PdcaStage.Closed,
                ActorUserId = supervisor.Id, ChangedAt = today.AddDays(-1),
                NotesEn = "Runbook published.", NotesAr = "تم نشر دليل الاستجابة." }
        );

        // Gap 4 — 90 days of CxAnalyticsSnapshot for segment=All + 30 days × 3 segments
        for (int day = 89; day >= 0; day--)
        {
            var d = today.AddDays(-day);
            var csat = 82m + (decimal)(rng.NextDouble() * 8.5);
            var nps  = 28m + (decimal)(rng.NextDouble() * 22);
            var ces  = 2.3m + (decimal)(rng.NextDouble() * 1.6);
            var volume = 18 + rng.Next(0, 17);
            var p95Hours = 6m + (decimal)(rng.NextDouble() * 18);
            db.CxAnalyticsSnapshots.Add(new CxAnalyticsSnapshot
            {
                SnapshotDate = d, Segment = "All",
                Csat = Math.Round(csat, 3), Nps = Math.Round(nps, 3),
                Ces = Math.Round(ces, 3), ComplaintVolume = volume,
                ResolutionRateP95Hours = Math.Round(p95Hours, 2),
            });
        }
        string[] segments = { "NewCustomer", "Returning", "VIP" };
        foreach (var seg in segments)
        {
            for (int day = 29; day >= 0; day--)
            {
                var d = today.AddDays(-day);
                // Slightly different baselines per segment for visual variety in the trend chart.
                var bumpCsat = seg == "VIP" ? 4m : seg == "NewCustomer" ? -2m : 0m;
                var bumpNps  = seg == "VIP" ? 9m : seg == "NewCustomer" ? -4m : 0m;
                db.CxAnalyticsSnapshots.Add(new CxAnalyticsSnapshot
                {
                    SnapshotDate = d, Segment = seg,
                    Csat = Math.Round(80m + bumpCsat + (decimal)(rng.NextDouble() * 8), 3),
                    Nps  = Math.Round(26m + bumpNps  + (decimal)(rng.NextDouble() * 20), 3),
                    Ces  = Math.Round(2.5m + (decimal)(rng.NextDouble() * 1.5), 3),
                    ComplaintVolume = 6 + rng.Next(0, 12),
                    ResolutionRateP95Hours = Math.Round(7m + (decimal)(rng.NextDouble() * 16), 2),
                });
            }
        }

        // 6 RootCauseLinks linking VoC → Complaint → ImprovementItem.
        // We tie back to seeded VoC/complaint IDs by fetching by content.
        var vocIds = await db.VocResponses.OrderBy(v => v.Id).Select(v => v.Id).Take(4).ToListAsync(ct);
        var complaintIds = await db.Complaints.OrderBy(c => c.Id).Select(c => c.Id).Take(4).ToListAsync(ct);
        for (int i = 0; i < Math.Min(vocIds.Count, complaintIds.Count); i++)
        {
            db.RootCauseLinks.Add(new RootCauseLink
            {
                FromType = "VocResponse", FromRefId = vocIds[i],
                ToType = "Complaint", ToRefId = complaintIds[i],
                LinkStrength = (decimal)(0.55 + rng.NextDouble() * 0.4),
                Notes = "Pattern surfaced by VoC sentiment + complaint category.",
            });
        }
        // Two links Complaint → ImprovementItem
        for (int i = 0; i < Math.Min(2, complaintIds.Count); i++)
        {
            db.RootCauseLinks.Add(new RootCauseLink
            {
                FromType = "Complaint", FromRefId = complaintIds[i],
                ToType = "ImprovementItem", ToRefId = improvements[i % improvements.Length].Id,
                LinkStrength = (decimal)(0.6 + rng.NextDouble() * 0.35),
                Notes = "Complaint pattern routed into a PDCA item.",
            });
        }

        // Gap 5 — 8 ContentReviewCycles across seeded KB articles
        var kbIds = await db.KbArticles.OrderBy(a => a.Id).Select(a => a.Id).ToListAsync(ct);
        var cycleData = new (int Idx, int DueOffset, ContentReviewStatus St, int Fresh, bool Parity)[]
        {
            (0, -10, ContentReviewStatus.Approved,  92, true),
            (1,  -3, ContentReviewStatus.Approved,  85, true),
            (2,  14, ContentReviewStatus.Pending,   70, true),
            (3, -20, ContentReviewStatus.Rejected,  48, false),
            (0,  45, ContentReviewStatus.Pending,   78, true),
            (1, -45, ContentReviewStatus.Approved,  95, true),
            (2,  -8, ContentReviewStatus.InReview,  55, true),
            (3,  -1, ContentReviewStatus.InReview,  35, false),
        };
        foreach (var c in cycleData)
        {
            if (c.Idx >= kbIds.Count) continue;
            db.ContentReviewCycles.Add(new ContentReviewCycle
            {
                KbArticleId = kbIds[c.Idx],
                DueDate = today.AddDays(c.DueOffset),
                AssignedReviewer = c.Idx % 2 == 0 ? "Layla Al-Qahtani" : "Fatima Al-Otaibi",
                Status = c.St,
                CompletedAt = c.St is ContentReviewStatus.Approved or ContentReviewStatus.Rejected
                    ? today.AddDays(c.DueOffset - 1) : null,
                FreshnessScore = c.Fresh, EnArParityFlag = c.Parity,
                Notes = c.Parity ? "Routine quarterly review." : "EN/AR parity broken — translation gap flagged.",
            });
        }

        // 30 days × 5 channels of ChannelPerformanceMetric
        string[] channels = { "Email", "WhatsApp", "Chat", "Portal", "Phone" };
        foreach (var ch in channels)
        {
            for (int day = 29; day >= 0; day--)
            {
                var d = today.AddDays(-day);
                var vol = ch switch {
                    "Email"    => 120 + rng.Next(0, 70),
                    "WhatsApp" => 180 + rng.Next(0, 90),
                    "Chat"     =>  80 + rng.Next(0, 50),
                    "Portal"   =>  60 + rng.Next(0, 40),
                    _          =>  45 + rng.Next(0, 30),
                };
                var avgMin = ch switch {
                    "Email"    => 36m + (decimal)(rng.NextDouble() * 14),
                    "WhatsApp" =>  8m + (decimal)(rng.NextDouble() * 6),
                    "Chat"     =>  3m + (decimal)(rng.NextDouble() * 3),
                    "Portal"   => 14m + (decimal)(rng.NextDouble() * 8),
                    _          =>  5m + (decimal)(rng.NextDouble() * 4),
                };
                var resPct = 70m + (decimal)(rng.NextDouble() * 25);
                var csatCh = 80m + (decimal)(rng.NextDouble() * 12);
                db.ChannelPerformanceMetrics.Add(new ChannelPerformanceMetric
                {
                    Channel = ch, MeasuredAt = d,
                    VolumeCount = vol,
                    AvgResponseMinutes = Math.Round(avgMin, 2),
                    ResolutionRatePct = Math.Round(resPct, 3),
                    CsatScore = Math.Round(csatCh, 3),
                });
            }
        }

        await db.SaveChangesAsync(ct);

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

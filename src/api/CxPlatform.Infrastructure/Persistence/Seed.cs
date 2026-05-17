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
        // matrix[role][page] = allowed. Page-level access is binary (the
        // RolePermission table doesn't model read/write). Controller-level
        // [Authorize(Roles=...)] enforces write privileges separately.
        //
        // Phase 1 grants (per brief): journeys/voc admin+supervisor+quality+executive+agent;
        // kb everyone except customer-write; programme exec+admin+supervisor+quality;
        // governance admin+supervisor+quality+executive.
        var matrix = new Dictionary<string, HashSet<string>>
        {
            ["admin"]      = new(pages),
            ["supervisor"] = new(new[]{"/about","/dashboard","/journeys","/voc","/complaints","/inbox","/kb","/copilot","/programme","/governance","/architecture","/automation","/notifications","/profile"}),
            ["agent"]      = new(new[]{"/about","/journeys","/complaints","/inbox","/kb","/copilot","/notifications","/profile"}),
            ["quality"]    = new(new[]{"/about","/journeys","/voc","/complaints","/kb","/programme","/governance","/architecture","/audit","/notifications","/profile"}),
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

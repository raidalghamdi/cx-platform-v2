using CxPlatform.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CxPlatform.Api.Controllers;

// Static reference data for the GAC enterprise architecture. No database —
// the 5 domains + 9 patterns are mandated and rarely change, so we serve
// them from code and let the SPA render the diagram + table client-side.
[ApiController]
[Authorize]
[Route("api/v1/architecture")]
public class ArchitectureController : ControllerBase
{
    private static readonly ArchitectureDomainDto[] Domains = new[]
    {
        new ArchitectureDomainDto("channels",
            "Channels", "القنوات",
            "Web, mobile, voice, walk-in, social, IoT — every customer touchpoint.",
            "الويب والجوال والصوت والزيارة والقنوات الاجتماعية و IoT — كل نقطة تماس مع المستفيد."),
        new ArchitectureDomainDto("experience",
            "Experience layer", "طبقة التجربة",
            "Portal, agent inbox, dashboards, copilot — what humans see.",
            "البوابة وصندوق الموظف ولوحات المؤشرات والمساعد الذكي — ما يراه المستخدمون."),
        new ArchitectureDomainDto("services",
            "Business services", "الخدمات الأعمالية",
            "Complaints, VoC, KB, journeys, programmes, governance — the domain APIs.",
            "الشكاوى وصوت المستفيد وقاعدة المعرفة والرحلات والبرامج والحوكمة — واجهات الأعمال."),
        new ArchitectureDomainDto("integration",
            "Integration & data", "التكامل والبيانات",
            "ESB, GSB, data lake, master data, message bus, batch pipelines.",
            "ناقل الخدمات الحكومي وبحيرة البيانات والبيانات المرجعية وناقل الرسائل والأنابيب الدفعية."),
        new ArchitectureDomainDto("foundations",
            "Foundations", "الأساسيات",
            "Identity (Nafath), audit, observability, security, cloud, DevSecOps.",
            "الهوية (نفاذ) والتدقيق والملاحظة والأمن والسحابة و DevSecOps."),
    };

    private static readonly ArchitecturePatternDto[] Patterns = new[]
    {
        new ArchitecturePatternDto("rest",          "REST",                "REST",                          "synchronous", "Default for cross-service request/response.",         "الافتراضي لتبادل الطلب/الاستجابة بين الخدمات."),
        new ArchitecturePatternDto("soap",          "SOAP",                "SOAP",                          "synchronous", "Legacy government partners that mandate WS-* contracts.", "الجهات الحكومية القديمة التي تشترط عقود WS-*."),
        new ArchitecturePatternDto("file",          "Secure file transfer", "نقل الملفات الآمن",            "batch",       "SFTP for bulk certificate exchange.",                "SFTP لتبادل الشهادات بحجم كبير."),
        new ArchitecturePatternDto("esb",           "Enterprise service bus", "ناقل خدمات المؤسسة",          "synchronous", "Internal mediation, routing, and protocol translation.", "الوساطة الداخلية والتوجيه وترجمة البروتوكولات."),
        new ArchitecturePatternDto("g2g_gsb",       "G2G via GSB",         "G2G عبر GSB",                   "synchronous", "Inter-ministry calls through the Government Service Bus.", "تكامل بين الجهات الحكومية عبر ناقل الخدمات الحكومي."),
        new ArchitecturePatternDto("microservices", "Microservices",       "الخدمات المصغّرة",              "asynchronous","Bounded contexts deployed independently.",          "نطاقات محدّدة تُنشَر باستقلال."),
        new ArchitecturePatternDto("pubsub",        "Pub/Sub",             "النشر/الاشتراك",                "asynchronous","Event fan-out for complaint, VoC, inbox events.",   "بثّ الأحداث لمتطلبات الشكاوى وصوت المستفيد والصندوق."),
        new ArchitecturePatternDto("data_migration","Data migration",      "ترحيل البيانات",                "batch",       "One-off / scheduled loads when onboarding a new agency.", "تحميلات مجدولة عند تأهيل جهة جديدة."),
        new ArchitecturePatternDto("data_agg",      "Data aggregation",    "تجميع البيانات",                "batch",       "ELT pipelines feeding the analytics lake.",         "أنابيب ELT تغذي بحيرة التحليلات."),
    };

    [HttpGet]
    public ActionResult<ArchitectureReferenceDto> Get()
        => Ok(new ArchitectureReferenceDto(Domains, Patterns));
}

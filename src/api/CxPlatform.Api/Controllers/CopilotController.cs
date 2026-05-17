using System.Security.Claims;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// Mock AI copilot. Same shape as the Phase 0 channel adapters — simulate
// 800-1500ms latency and 95% success. Production swap = drop in a real LLM
// client and reuse the same DTOs/persistence path.
[ApiController]
[Authorize]
[Route("api/v1/copilot")]
public class CopilotController : ControllerBase
{
    private static readonly Random Rng = new();
    private readonly AppDbContext _db;
    public CopilotController(AppDbContext db) { _db = db; }

    [HttpPost("ask")]
    public async Task<ActionResult<CopilotInteractionDto>> Ask([FromBody] AskCopilotRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.PromptEn) && string.IsNullOrWhiteSpace(req.PromptAr))
            return BadRequest(new { error = "prompt is required in at least one language" });

        int delay; bool ok;
        lock (Rng)
        {
            delay = Rng.Next(800, 1501);
            ok = Rng.NextDouble() < 0.95;
        }
        await Task.Delay(delay, ct);

        var intent = NormaliseIntent(req.Intent);
        var (replyEn, replyAr) = ok
            ? Reply(intent, req.PromptEn ?? "", req.PromptAr ?? "")
            : ("The copilot is temporarily unavailable. Please retry shortly.",
               "المساعد الذكي غير متاح حالياً. الرجاء المحاولة بعد قليل.");

        var row = new CopilotInteraction
        {
            UserId = CurrentUserId(),
            Intent = intent,
            PromptEn = req.PromptEn ?? "",
            PromptAr = req.PromptAr ?? "",
            ResponseEn = replyEn,
            ResponseAr = replyAr,
            LatencyMs = delay,
            Success = ok,
        };
        _db.CopilotInteractions.Add(row);
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<CopilotInteractionDto>>> History(CancellationToken ct)
    {
        var uid = CurrentUserId();
        var q = _db.CopilotInteractions.AsNoTracking().AsQueryable();
        if (uid is not null && !User.IsInRole("admin")) q = q.Where(c => c.UserId == uid);
        var rows = await q.OrderByDescending(c => c.CreatedAt).Take(50).Select(c => c.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    private static string NormaliseIntent(string s) => s?.ToLowerInvariant() switch
    {
        "draft_reply"   => "draft_reply",
        "summarise"     => "summarise",
        "find_similar"  => "find_similar",
        _               => "ask",
    };

    private static (string en, string ar) Reply(string intent, string promptEn, string promptAr) => intent switch
    {
        "draft_reply"  => (
            "Draft reply:\n\nThank you for reaching out. Your case has been received and is being reviewed by our team. We will respond within 48 working hours with a status update.",
            "مسودة رد:\n\nشكراً لتواصلك. تم استلام حالتك وهي قيد المراجعة من قِبَل فريقنا. سنوافيك بتحديث خلال 48 ساعة عمل."),
        "summarise"    => (
            "Summary: thread spans 3 messages over the past 24h. Customer reports a delay; agent escalated to back office; awaiting confirmation.",
            "الملخص: المحادثة تمتد على 3 رسائل خلال 24 ساعة. أبلغ المستفيد عن تأخير؛ صعّد الموظف الطلب للجهة المعنية؛ بانتظار التأكيد."),
        "find_similar" => (
            "Found 3 similar complaints in the last 30 days, all relating to invoice mismatch on tariff schedules. Recommend grouping for root-cause analysis.",
            "وُجدت 3 شكاوى مشابهة خلال 30 يوماً، جميعها تتعلق باختلاف الفواتير عن جدول التعرفة. يُنصح بتجميعها لتحليل السبب الجذري."),
        _              => (
            "Based on the data available, the most recent KPI movement of interest is SLA compliance — down 0.6 points week-over-week. Drill into the SLA breach trend for the underlying drivers.",
            "بناءً على البيانات المتاحة، أبرز تغيّر حديث في المؤشرات هو الالتزام بمستوى الخدمة — تراجع 0.6 نقطة أسبوعياً. راجع اتجاه تجاوزات SLA لفهم الأسباب."),
    };

    private long? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var v) ? v : null;
    }
}

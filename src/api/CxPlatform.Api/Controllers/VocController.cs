using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/voc")]
public class VocController : ControllerBase
{
    private readonly AppDbContext _db;
    public VocController(AppDbContext db) { _db = db; }

    // GET /api/v1/voc?channel=email&sentiment=negative
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VocResponseDto>>> List(
        [FromQuery] string? channel,
        [FromQuery] string? sentiment,
        CancellationToken ct)
    {
        var q = _db.VocResponses.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(channel)) q = q.Where(v => v.Channel == channel);
        if (!string.IsNullOrWhiteSpace(sentiment)) q = q.Where(v => v.Sentiment == sentiment);
        var rows = await q.OrderByDescending(v => v.RespondedAt).Select(v => v.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<VocResponseDto>> Create([FromBody] CreateVocResponseRequest req, CancellationToken ct)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Channel)) return BadRequest();
        var row = new VocResponse {
            SurveyEn = req.SurveyEn ?? "", SurveyAr = req.SurveyAr ?? "",
            Channel = req.Channel, NpsScore = Math.Clamp(req.NpsScore, 0, 10),
            Sentiment = NormaliseSentiment(req.Sentiment),
            CommentEn = req.CommentEn ?? "", CommentAr = req.CommentAr ?? "",
            CustomerName = req.CustomerName ?? "",
            RespondedAt = DateTime.UtcNow,
        };
        _db.VocResponses.Add(row);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), null, row.ToDto());
    }

    // PUT /api/v1/voc/{id}/comment — update the bilingual comment fields.
    [HttpPut("{id:long}/comment")]
    [Authorize(Roles = "admin,supervisor,quality")]
    public async Task<ActionResult<VocResponseDto>> UpdateComment(long id, [FromBody] UpdateVocCommentRequest req, CancellationToken ct)
    {
        var row = await _db.VocResponses.FirstOrDefaultAsync(v => v.Id == id, ct);
        if (row is null) return NotFound();
        row.CommentEn = req.CommentEn ?? "";
        row.CommentAr = req.CommentAr ?? "";
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    private static string NormaliseSentiment(string s) =>
        s?.ToLowerInvariant() switch
        {
            "positive" => "positive",
            "negative" => "negative",
            _ => "neutral",
        };
}

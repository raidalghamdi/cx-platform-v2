using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Infrastructure.Persistence;
using CxPlatform.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// Read-only viewer for the hash-chained audit log + a verification endpoint
// that recomputes every entry hash from the genesis row forward.
[ApiController]
[Authorize(Roles = "admin,supervisor,quality,executive")]
[Route("api/v1/audit")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db) { _db = db; }

    // GET /api/v1/audit/events?userId=&kind=&from=&to=&page=1&pageSize=50
    [HttpGet("events")]
    public async Task<ActionResult<AuditPageDto>> Events(
        [FromQuery] long? userId,
        [FromQuery] string? kind,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.AuditEvents.AsNoTracking().AsQueryable();
        if (userId is not null) q = q.Where(a => a.ActorUserId == userId);
        if (!string.IsNullOrWhiteSpace(kind))
        {
            var like = $"%{kind}%";
            q = q.Where(a => EF.Functions.Like(a.Kind, like));
        }
        if (from is not null) q = q.Where(a => a.At >= from);
        if (to is not null) q = q.Where(a => a.At <= to);

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => a.ToDto())
            .ToListAsync(ct);
        return Ok(new AuditPageDto(items, total, page, pageSize));
    }

    // GET /api/v1/audit/verify — recomputes the chain from genesis. Returns
    // ok=true if every entry matches; otherwise reports the first mismatch.
    [HttpGet("verify")]
    public async Task<ActionResult<AuditVerifyResultDto>> Verify(CancellationToken ct)
    {
        var prev = HashChain.Genesis;
        var index = 0;
        long? firstBrokenId = null;
        int? firstBrokenIndex = null;
        await foreach (var row in _db.AuditEvents.AsNoTracking().OrderBy(a => a.Id).AsAsyncEnumerable().WithCancellation(ct))
        {
            var expected = HashChain.ComputeEntryHash(prev, row.PayloadJson ?? "{}");
            // Either the prev-hash linkage broke, or the row's own entry hash
            // doesn't match the recomputed value over its prev+payload.
            if (row.PrevHash != prev || row.EntryHash != expected)
            {
                firstBrokenIndex = index;
                firstBrokenId = row.Id;
                break;
            }
            prev = row.EntryHash;
            index++;
        }
        var total = await _db.AuditEvents.CountAsync(ct);
        return Ok(new AuditVerifyResultDto(firstBrokenIndex is null, total, firstBrokenIndex, firstBrokenId));
    }
}

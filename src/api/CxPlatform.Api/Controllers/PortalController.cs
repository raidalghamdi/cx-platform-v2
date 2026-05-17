using System.Security.Claims;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

// Customer-facing portal flow. Only the customer themselves (and admin)
// can see their own requests — never another customer's.
[ApiController]
[Authorize(Roles = "customer,admin")]
[Route("api/v1/portal")]
public class PortalController : ControllerBase
{
    private readonly AppDbContext _db;
    public PortalController(AppDbContext db) { _db = db; }

    [HttpGet("my-requests")]
    public async Task<ActionResult<IReadOnlyList<PortalRequestDto>>> MyRequests(CancellationToken ct)
    {
        var uid = CurrentUserId();
        var q = _db.PortalRequests.AsNoTracking().AsQueryable();
        // Admin sees all customer requests for support purposes; customer
        // only sees their own.
        if (!User.IsInRole("admin") && uid is not null)
            q = q.Where(r => r.CustomerId == uid);
        var rows = await q.OrderByDescending(r => r.CreatedAt).Select(r => r.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<PortalRequestDto>> Get(long id, CancellationToken ct)
    {
        var uid = CurrentUserId();
        var row = await _db.PortalRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (row is null) return NotFound();
        if (!User.IsInRole("admin") && row.CustomerId != uid) return Forbid();
        return Ok(row.ToDto());
    }

    [HttpPost]
    public async Task<ActionResult<PortalRequestDto>> Create([FromBody] CreatePortalRequestRequest req, CancellationToken ct)
    {
        if (req is null) return BadRequest();
        if (string.IsNullOrWhiteSpace(req.SubjectEn) && string.IsNullOrWhiteSpace(req.SubjectAr))
            return BadRequest(new { error = "subject is required in at least one language" });
        var row = new PortalRequest
        {
            CustomerId = CurrentUserId(),
            Type = string.IsNullOrWhiteSpace(req.Type) ? "complaint" : req.Type,
            SubjectEn = req.SubjectEn ?? "",
            SubjectAr = req.SubjectAr ?? "",
            BodyEn = req.BodyEn ?? "",
            BodyAr = req.BodyAr ?? "",
            Status = "new",
        };
        _db.PortalRequests.Add(row);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = row.Id }, row.ToDto());
    }

    private long? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var v) ? v : null;
    }
}

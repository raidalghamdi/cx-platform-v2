using System.Security.Claims;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public NotificationsController(AppDbContext db) { _db = db; }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> List(CancellationToken ct)
    {
        var uid = CurrentUserId();
        if (uid is null) return Forbid();
        var rows = await _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == uid)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => n.ToDto())
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id, CancellationToken ct)
    {
        var uid = CurrentUserId();
        if (uid is null) return Forbid();
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid, ct);
        if (n is null) return NotFound();
        n.ReadAt ??= DateTime.UtcNow;
        n.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private long? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var v) ? v : null;
    }
}

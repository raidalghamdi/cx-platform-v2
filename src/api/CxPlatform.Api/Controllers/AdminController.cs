using System.Security.Claims;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize(Roles = "admin")]
[Route("api/v1/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminController(AppDbContext db) { _db = db; }

    // ── Role permissions ────────────────────────────────────────────────────

    [HttpGet("role-permissions")]
    public async Task<ActionResult<IReadOnlyList<RolePermissionDto>>> ListPerms(CancellationToken ct)
    {
        var rows = await _db.RolePermissions.AsNoTracking()
            .OrderBy(x => x.Role).ThenBy(x => x.PageKey)
            .Select(x => x.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    // PATCH /api/v1/admin/role-permissions — upsert in bulk. Admin is locked
    // to all-true and the server enforces it regardless of payload.
    [HttpPatch("role-permissions")]
    public async Task<IActionResult> UpdatePerms([FromBody] UpdateRolePermissionsRequest req, CancellationToken ct)
    {
        if (req?.Items is null) return BadRequest();
        var byKey = (await _db.RolePermissions.ToListAsync(ct))
            .ToDictionary(x => (x.Role, x.PageKey));
        foreach (var item in req.Items)
        {
            var allowed = item.Role == "admin" || item.Allowed;
            if (byKey.TryGetValue((item.Role, item.PageKey), out var row))
            {
                row.Allowed = allowed;
                row.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.RolePermissions.Add(new RolePermission { Role = item.Role, PageKey = item.PageKey, Allowed = allowed });
            }
        }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Contact channels ────────────────────────────────────────────────────

    [HttpGet("contact-channels")]
    public async Task<ActionResult<IReadOnlyList<ContactChannelDto>>> ListChannels(CancellationToken ct)
    {
        var rows = await _db.ContactChannels.AsNoTracking().OrderBy(x => x.Key).Select(x => x.ToDto()).ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPatch("contact-channels/{key}")]
    public async Task<ActionResult<ContactChannelDto>> UpdateChannel(string key, [FromBody] UpdateContactChannelRequest req, CancellationToken ct)
    {
        var row = await _db.ContactChannels.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (row is null) return NotFound();
        row.Value = req.Value ?? "";
        row.UpdatedBy = CurrentUserId();
        row.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(row.ToDto());
    }

    private long? CurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return long.TryParse(sub, out var v) ? v : null;
    }
}

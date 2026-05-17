using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kpis")]
public class KpisController : ControllerBase
{
    private readonly AppDbContext _db;
    public KpisController(AppDbContext db) { _db = db; }

    // RoleScope = "all" or pipe-joined roles (e.g. "executive|admin"). Phase 0
    // seeds everything as "all", but the filter is in place for later phases.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KpiDto>>> List(CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        var rows = await _db.Kpis.AsNoTracking()
            .Where(k => k.RoleScope == "all" || k.RoleScope.Contains(role))
            .OrderBy(k => k.Key)
            .ToListAsync(ct);
        return Ok(rows.Select(k => k.ToDto()).ToList());
    }
}

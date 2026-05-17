using CxPlatform.Api.Auth;
using CxPlatform.Api.Mappers;
using CxPlatform.Application.Dtos;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db; _tokens = tokens;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { error = "email and password are required" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email.ToLower(), ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            // Mitigate user-enumeration via uniform 401.
            return Unauthorized(new { error = "invalid email or password" });
        }
        if (user.Status != "active")
            return Unauthorized(new { error = "account disabled" });

        var access = _tokens.IssueAccessToken(user);
        var (refresh, hash, expires) = _tokens.IssueRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = hash, ExpiresAt = expires });
        await _db.SaveChangesAsync(ct);

        var perms = await _db.RolePermissions
            .Where(p => p.Role == user.Role)
            .OrderBy(p => p.PageKey)
            .Select(p => p.ToDto())
            .ToListAsync(ct);

        return Ok(new LoginResponse(access, refresh, user.ToDto(), perms));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) return BadRequest();
        var hash = _tokens.HashRefreshToken(req.RefreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x =>
            x.TokenHash == hash && x.RevokedAt == null && x.ExpiresAt > DateTime.UtcNow, ct);
        if (token is null) return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null || user.Status != "active") return Unauthorized();

        // Rotate the refresh token (revoke old, issue new) — defends against replay.
        token.RevokedAt = DateTime.UtcNow;
        var (newRefresh, newHash, expires) = _tokens.IssueRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = newHash, ExpiresAt = expires });
        await _db.SaveChangesAsync(ct);

        var access = _tokens.IssueAccessToken(user);
        var perms = await _db.RolePermissions
            .Where(p => p.Role == user.Role)
            .OrderBy(p => p.PageKey)
            .Select(p => p.ToDto())
            .ToListAsync(ct);
        return Ok(new LoginResponse(access, newRefresh, user.ToDto(), perms));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken)) return NoContent();
        var hash = _tokens.HashRefreshToken(req.RefreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return NoContent();
    }
}

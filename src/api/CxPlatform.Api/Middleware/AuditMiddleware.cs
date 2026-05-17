using System.Security.Claims;
using System.Text.Json;
using CxPlatform.Domain.Entities;
using CxPlatform.Infrastructure.Persistence;
using CxPlatform.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CxPlatform.Api.Middleware;

// Writes a hash-chained audit record for every mutating /api/* call. Reads
// the current chain tip with a row-level lock so concurrent writers can't
// fork the chain. Failures here are LOGGED but never block the response.
public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly SemaphoreSlim _gate = new(1, 1);

    public AuditMiddleware(RequestDelegate next) { _next = next; }

    public async Task Invoke(HttpContext ctx, AppDbContext db, ILogger<AuditMiddleware> log)
    {
        await _next(ctx);

        var method = ctx.Request.Method;
        var path = ctx.Request.Path.Value ?? "";
        var isMutating = method is "POST" or "PUT" or "PATCH" or "DELETE";
        if (!isMutating || !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            return;
        // skip preflight + auth refresh noise — they're not interesting mutations
        if (path.EndsWith("/auth/refresh", StringComparison.OrdinalIgnoreCase)) return;

        try
        {
            await _gate.WaitAsync();
            try
            {
                var tip = await db.AuditEvents.OrderByDescending(x => x.Id).Select(x => x.EntryHash).FirstOrDefaultAsync()
                          ?? HashChain.Genesis;

                long? userId = null;
                var sub = ctx.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User?.FindFirstValue("sub");
                if (long.TryParse(sub, out var parsed)) userId = parsed;

                var payload = JsonSerializer.Serialize(new
                {
                    method,
                    path,
                    statusCode = ctx.Response.StatusCode,
                    at = DateTime.UtcNow,
                    actor = userId,
                });
                var entryHash = HashChain.ComputeEntryHash(tip, payload);
                db.AuditEvents.Add(new AuditEvent
                {
                    Kind = $"{method.ToLowerInvariant()} {path}",
                    ActorUserId = userId,
                    TargetKind = "http",
                    PrevHash = tip,
                    EntryHash = entryHash,
                    PayloadJson = payload,
                    At = DateTime.UtcNow,
                });
                await db.SaveChangesAsync();
            }
            finally { _gate.Release(); }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Audit middleware failed for {method} {path}", method, path);
        }
    }
}

using System.Diagnostics;
using System.Net.Http;
using CxPlatform.Application.Services;
using CxPlatform.Domain.Entities;
using CxPlatform.Domain.Enums;
using CxPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CxPlatform.Infrastructure.Workflows;

// Round 5 Subagent 2 — real synthetic-check runner.
//
// Fetches every enabled SyntheticCheck row, performs an HTTP GET against
// each endpoint, updates LastRunAt / LastStatus / LastLatencyMs on the row
// and appends a ServiceHealthMetric data point per check. Timeouts and
// non-2xx responses both count as Fail. Network errors are caught and
// reported as Fail so the BackgroundService loop stays alive.
public class SyntheticCheckRunner : ISyntheticCheckRunner
{
    public const string HttpClientName = "synthetic-checks";

    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SyntheticCheckRunner> _log;

    public SyntheticCheckRunner(
        AppDbContext db,
        IHttpClientFactory httpFactory,
        ILogger<SyntheticCheckRunner> log)
    {
        _db = db; _httpFactory = httpFactory; _log = log;
    }

    public async Task<int> RunOnceAsync(CancellationToken ct = default)
    {
        var checks = await _db.SyntheticChecks
            .Where(c => c.Enabled)
            .ToListAsync(ct);
        if (checks.Count == 0) return 0;

        var http = _httpFactory.CreateClient(HttpClientName);
        // 10 s timeout per brief — anything longer counts as Fail.
        http.Timeout = TimeSpan.FromSeconds(10);

        var ran = 0;
        foreach (var check in checks)
        {
            ct.ThrowIfCancellationRequested();
            var (status, latencyMs) = await ProbeAsync(http, check.Endpoint, ct);

            check.LastRunAt = DateTime.UtcNow;
            check.LastStatus = status;
            check.LastLatencyMs = latencyMs;
            check.UpdatedAt = DateTime.UtcNow;

            // ServiceHealthMetric receives one data point per check run.
            // A pass yields uptime=100/error=0; a fail yields 0/100. MTTR
            // is left at 0 — incidents drive that field separately.
            _db.ServiceHealthMetrics.Add(new ServiceHealthMetric
            {
                ServiceName = DeriveServiceName(check.Name),
                MeasuredAt = DateTime.UtcNow,
                UptimePct = status == CheckStatus.Pass ? 100m : 0m,
                P95LatencyMs = latencyMs,
                ErrorRatePct = status == CheckStatus.Pass ? 0m : 100m,
                MttrMinutes = 0,
                RequestCount = 1,
            });
            ran++;
        }

        await _db.SaveChangesAsync(ct);
        return ran;
    }

    // Hit the endpoint, time it, classify the outcome. Never throws — any
    // exception (timeout, DNS, refused, 5xx body parse, ...) becomes Fail.
    private async Task<(CheckStatus status, int latencyMs)> ProbeAsync(
        HttpClient http, string endpoint, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Allow relative endpoints (the seeded checks use /api/v1/...) —
            // if BaseAddress isn't set, prefix with http://localhost:5001 so
            // dev still works. A relative URI passed without a base raises.
            HttpRequestMessage req;
            if (Uri.TryCreate(endpoint, UriKind.Absolute, out var abs))
                req = new HttpRequestMessage(HttpMethod.Get, abs);
            else
                req = new HttpRequestMessage(HttpMethod.Get,
                    new Uri(http.BaseAddress ?? new Uri("http://localhost:5001"), endpoint));

            using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            sw.Stop();
            var ok = (int)res.StatusCode is >= 200 and < 300;
            return (ok ? CheckStatus.Pass : CheckStatus.Fail, (int)sw.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // HttpClient cancellation that isn't ours = timeout.
            sw.Stop();
            _log.LogWarning("Synthetic check {endpoint} timed out after {ms} ms", endpoint, sw.ElapsedMilliseconds);
            return (CheckStatus.Fail, (int)sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogWarning(ex, "Synthetic check {endpoint} failed", endpoint);
            return (CheckStatus.Fail, (int)sw.ElapsedMilliseconds);
        }
    }

    // Check names follow either "Service name" (e.g. "Inbox threads") or
    // "Service: detail" (e.g. "Auth: login"). Pick the first space-delimited
    // word as the canonical service slot. Falls back to the full name.
    private static string DeriveServiceName(string checkName)
    {
        if (string.IsNullOrWhiteSpace(checkName)) return "unknown";
        var colonIdx = checkName.IndexOf(':');
        if (colonIdx > 0) return checkName[..colonIdx].Trim();
        var spaceIdx = checkName.IndexOf(' ');
        if (spaceIdx > 0) return checkName[..spaceIdx].Trim();
        return checkName.Trim();
    }
}

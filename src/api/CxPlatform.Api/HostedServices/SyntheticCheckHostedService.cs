using CxPlatform.Application.Services;

namespace CxPlatform.Api.HostedServices;

// Background loop that calls ISyntheticCheckRunner.RunOnceAsync() every 60 s.
// Resolves the scoped runner per tick (it depends on the request-scoped
// AppDbContext) and swallows all exceptions so a transient network or
// database glitch can't crash the host.
public sealed class SyntheticCheckHostedService : BackgroundService
{
    // Configurable interval would be nicer, but 60 s matches the brief and
    // the seeded SyntheticCheck.IntervalSeconds default.
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    private readonly IServiceProvider _services;
    private readonly ILogger<SyntheticCheckHostedService> _log;
    private readonly IHostApplicationLifetime _lifetime;

    public SyntheticCheckHostedService(
        IServiceProvider services,
        ILogger<SyntheticCheckHostedService> log,
        IHostApplicationLifetime lifetime)
    {
        _services = services;
        _log = log;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the host to finish startup before the first tick — this
        // avoids a race where the EF migrate+seed hasn't run yet.
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<ISyntheticCheckRunner>();
                var n = await runner.RunOnceAsync(stoppingToken);
                if (n > 0) _log.LogInformation("Synthetic checks: ran {count}", n);
            }
            catch (OperationCanceledException)
            {
                // Host is shutting down — bail cleanly.
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Synthetic-check tick failed; will retry next interval.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}

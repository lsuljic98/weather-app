using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace WeatherApp.Infrastructure;

/// <summary>
/// Applies pending EF migrations at startup. Retries on connection failures because Postgres
/// can accept TCP connections a moment before it is ready to serve queries, and because a
/// database may restart while the app keeps running under an orchestrator.
/// </summary>
public static class DatabaseMigrator
{
    public const int DefaultMaxAttempts = 10;
    public static readonly TimeSpan DefaultBaseDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(5);

    public static async Task MigrateAsync(
        IServiceProvider services,
        CancellationToken ct = default,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? baseDelay = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);
        var delay = baseDelay ?? DefaultBaseDelay;

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator));

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();

                var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToArray();
                if (pending.Length == 0)
                {
                    logger.LogInformation("Database is up to date; no migrations to apply.");
                    return;
                }

                logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Length, pending);
                await db.Database.MigrateAsync(ct);
                logger.LogInformation("Migrations applied.");
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                // Linear back-off, capped: 1s, 2s, 3s, 4s, 5s, 5s ...
                var wait = TimeSpan.FromTicks(Math.Min(delay.Ticks * attempt, MaxDelay.Ticks));
                logger.LogWarning(ex,
                    "Database not reachable (attempt {Attempt}/{Max}); retrying in {Delay}s.",
                    attempt, maxAttempts, wait.TotalSeconds);
                await Task.Delay(wait, ct);
            }
        }
    }

    // Connection-level failures only. A broken migration script must fail fast, not loop.
    private static bool IsTransient(Exception ex) => ex switch
    {
        NpgsqlException { IsTransient: true } => true,
        NpgsqlException { InnerException: SocketException or TimeoutException } => true,
        SocketException or TimeoutException => true,
        _ => false,
    };
}

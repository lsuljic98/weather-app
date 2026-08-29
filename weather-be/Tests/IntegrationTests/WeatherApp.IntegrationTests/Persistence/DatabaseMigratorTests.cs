using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Npgsql;
using Shouldly;
using WeatherApp.Infrastructure;
using WeatherApp.IntegrationTests.Support;

namespace WeatherApp.IntegrationTests.Persistence;

[Collection(ApiCollection.Name)]
public sealed class DatabaseMigratorTests(ApiFactory factory)
{
    private static (ServiceProvider Provider, FakeLoggerProvider Logs) Build(string connectionString)
    {
        var logs = new FakeLoggerProvider();
        var provider = new ServiceCollection()
            .AddLogging(b => b.AddProvider(logs))
            .AddDbContext<WeatherDbContext>(o => o.UseNpgsql(connectionString))
            .BuildServiceProvider();
        return (provider, logs);
    }

    [Fact]
    public async Task MigrateAsync_WhenDatabaseIsCurrent_ShouldBeIdempotent()
    {
        var (provider, logs) = Build(factory.ConnectionString);
        await using var _ = provider;

        await Should.NotThrowAsync(() => DatabaseMigrator.MigrateAsync(provider));

        logs.Collector.GetSnapshot().ShouldContain(e => e.Message.Contains("up to date"));
        await using var db = factory.NewDbContext();
        (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task MigrateAsync_WhenDatabaseUnreachable_ShouldRetryThenThrow()
    {
        // Port 1 is never a Postgres server: connection refused, immediately.
        var (provider, logs) = Build("Host=127.0.0.1;Port=1;Database=x;Username=x;Password=x;Timeout=1");
        await using var _ = provider;

        await Should.ThrowAsync<NpgsqlException>(() =>
            DatabaseMigrator.MigrateAsync(provider, maxAttempts: 3, baseDelay: TimeSpan.FromMilliseconds(10)));

        var retries = logs.Collector.GetSnapshot().Where(e => e.Level == LogLevel.Warning).ToList();
        retries.Count.ShouldBe(2); // attempts 1 and 2 retried; attempt 3 threw
        retries[0].Message.ShouldContain("attempt 1/3");
        retries[1].Message.ShouldContain("attempt 2/3");
    }
}

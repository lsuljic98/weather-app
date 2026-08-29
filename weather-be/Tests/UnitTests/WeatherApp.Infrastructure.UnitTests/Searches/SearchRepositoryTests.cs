using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shouldly;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Repositories;

namespace WeatherApp.Infrastructure.UnitTests.Searches;

/// <summary>SearchRepository against an in-memory SQLite database built from the real model.</summary>
public sealed class SearchRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly DbContextOptions<WeatherDbContext> _options;

    /// <summary>
    /// SQLite cannot sort DateTimeOffset columns; storing them as UTC ticks (lossless, unlike
    /// the built-in binary converter) keeps the real ORDER BY CreatedAt query runnable here.
    /// Postgres needs no such conversion.
    /// </summary>
    private sealed class SqliteWeatherDbContext(DbContextOptions<WeatherDbContext> options) : WeatherDbContext(options)
    {
        protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        {
            base.ConfigureConventions(builder);
            builder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
        }
    }
    private readonly User _alice = new("alice@example.com", "hash");
    private readonly User _bob = new("bob@example.com", "hash");

    public SearchRepositoryTests()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<WeatherDbContext>().UseSqlite(_connection).Options;

        using var db = new SqliteWeatherDbContext(_options);
        db.Database.EnsureCreated();
        db.Users.AddRange(_alice, _bob);
        db.SaveChanges();
    }

    private sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
        v => v.UtcTicks,
        v => new DateTimeOffset(v, TimeSpan.Zero));

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task AddAsync_ShouldPersistEverySnapshotColumn()
    {
        var search = Row(_alice.Id, "Zagreb", temp: 24.5, condition: "Rain");
        await using (var db = new SqliteWeatherDbContext(_options))
            await new SearchRepository(db).AddAsync(search);

        await using var verify = new SqliteWeatherDbContext(_options);
        var stored = await verify.Searches.SingleAsync();

        stored.Id.ShouldBe(search.Id);
        stored.UserId.ShouldBe(_alice.Id);
        stored.CityName.ShouldBe("Zagreb");
        stored.CountryCode.ShouldBe("HR");
        stored.TemperatureC.ShouldBe(24.5);
        stored.ConditionMain.ShouldBe("Rain");
        stored.CreatedAt.ShouldBe(search.CreatedAt);
    }

    [Fact]
    public async Task GetPageAsync_ShouldReturnOnlyThatUsersRowsNewestFirst()
    {
        await using var db = new SqliteWeatherDbContext(_options);
        var repo = new SearchRepository(db);
        var t0 = DateTimeOffset.UtcNow;
        await repo.AddAsync(Row(_alice.Id, "Oldest"));
        await repo.AddAsync(Row(_alice.Id, "Middle"));
        await repo.AddAsync(Row(_bob.Id, "Bobs"));
        await repo.AddAsync(Row(_alice.Id, "Newest"));

        var page = await repo.GetPageAsync(_alice.Id, page: 1, pageSize: 10);

        page.Select(s => s.CityName).ShouldBe(["Newest", "Middle", "Oldest"]);
        page.ShouldAllBe(s => s.UserId == _alice.Id);
        page.ShouldAllBe(s => s.CreatedAt >= t0);
    }

    [Fact]
    public async Task GetPageAsync_ShouldSkipAndTakeByPage()
    {
        await using var db = new SqliteWeatherDbContext(_options);
        var repo = new SearchRepository(db);
        for (var i = 1; i <= 7; i++)
            await repo.AddAsync(Row(_alice.Id, $"City{i}"));

        var second = await repo.GetPageAsync(_alice.Id, page: 2, pageSize: 3);
        var last = await repo.GetPageAsync(_alice.Id, page: 3, pageSize: 3);
        var beyond = await repo.GetPageAsync(_alice.Id, page: 4, pageSize: 3);

        second.Select(s => s.CityName).ShouldBe(["City4", "City3", "City2"]);
        last.Select(s => s.CityName).ShouldBe(["City1"]);
        beyond.ShouldBeEmpty();
    }

    [Fact]
    public async Task CountAsync_ShouldCountOnlyThatUsersRows()
    {
        await using var db = new SqliteWeatherDbContext(_options);
        var repo = new SearchRepository(db);
        await repo.AddAsync(Row(_alice.Id, "A"));
        await repo.AddAsync(Row(_alice.Id, "B"));
        await repo.AddAsync(Row(_bob.Id, "C"));

        (await repo.CountAsync(_alice.Id)).ShouldBe(2);
        (await repo.CountAsync(_bob.Id)).ShouldBe(1);
        (await repo.CountAsync(Guid.CreateVersion7())).ShouldBe(0);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    public async Task GetPageAsync_WhenPagingInvalid_ShouldThrow(int page, int pageSize)
    {
        await using var db = new SqliteWeatherDbContext(_options);
        var repo = new SearchRepository(db);

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => repo.GetPageAsync(_alice.Id, page, pageSize));
    }

    private static Search Row(Guid userId, string city, double temp = 20, string condition = "Clear") =>
        new(userId, city, "HR", 45.8, 15.9, condition, "clear sky", "01d", temp, 40, 2.5);
}

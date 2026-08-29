using Microsoft.EntityFrameworkCore;
using Shouldly;
using WeatherApp.Domain.Entities;
using WeatherApp.IntegrationTests.Support;

namespace WeatherApp.IntegrationTests.Persistence;

/// <summary>What the committed migration actually creates in Postgres for the searches table.</summary>
[Collection(ApiCollection.Name)]
public sealed class SchemaTests(ApiFactory factory)
{
    [Fact]
    public async Task Migrations_ShouldAllBeApplied()
    {
        await using var db = factory.NewDbContext();

        (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        (await db.Database.GetAppliedMigrationsAsync()).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Searches_ShouldHaveTheThreeUserScopedIndexes()
    {
        await using var db = factory.NewDbContext();

        var indexes = await db.Database
            .SqlQueryRaw<string>("SELECT indexname AS \"Value\" FROM pg_indexes WHERE tablename = 'searches'")
            .ToListAsync();

        indexes.ShouldContain("ix_searches_user_id_searched_at");
        indexes.ShouldContain("ix_searches_user_id_city_name");
        indexes.ShouldContain("ix_searches_user_id_condition_main");
    }

    [Fact]
    public async Task Searches_HistoryIndex_ShouldOrderSearchedAtDescending()
    {
        await using var db = factory.NewDbContext();

        var definition = await db.Database
            .SqlQueryRaw<string>("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE indexname = 'ix_searches_user_id_searched_at'")
            .SingleAsync();

        definition.ShouldContain("searched_at DESC");
    }

    [Fact]
    public async Task DeletingUser_ShouldCascadeToSearches()
    {
        var (_, user) = await factory.RegisterAsync();
        await using (var db = factory.NewDbContext())
        {
            db.Searches.Add(new Search(user.Id, "Zagreb", "HR", 45.8, 15.9, "Clear", "clear sky", "01d", 22, 40, 2));
            db.Searches.Add(new Search(user.Id, "Osijek", "HR", 45.5, 18.7, "Rain", "light rain", "10d", 18, 80, 4));
            await db.SaveChangesAsync();
        }

        await using (var db = factory.NewDbContext())
            await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();

        await using var verify = factory.NewDbContext();
        (await verify.Searches.AnyAsync(s => s.UserId == user.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task InsertingSearch_ForUnknownUser_ShouldViolateForeignKey()
    {
        await using var db = factory.NewDbContext();
        db.Searches.Add(new Search(Guid.CreateVersion7(), "Zagreb", "HR", 45.8, 15.9, "Clear", "clear sky", "01d", 22, 40, 2));

        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}

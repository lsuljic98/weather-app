using WeatherApp.Application.Abstractions;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.UnitTests.Support;

public sealed class InMemorySearchRepository : ISearchRepository
{
    public List<Search> Rows { get; } = [];
    public List<(int Page, int PageSize)> PageRequests { get; } = [];

    public Task AddAsync(Search search, CancellationToken ct = default)
    {
        Rows.Add(search);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Search>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        PageRequests.Add((page, pageSize));
        IReadOnlyList<Search> result = Rows
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(Rows.Count(s => s.UserId == userId));
}

using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Abstractions;

/// <summary>Stores a user's forecast searches and reads them back as history.</summary>
public interface ISearchRepository
{
    Task AddAsync(Search search, CancellationToken ct = default);

    /// <summary>One page of a user's searches, newest first. Page numbering starts at 1.</summary>
    Task<IReadOnlyList<Search>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task<int> CountAsync(Guid userId, CancellationToken ct = default);
}

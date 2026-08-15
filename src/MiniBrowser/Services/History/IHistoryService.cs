using MiniBrowser.Core.Models;

namespace MiniBrowser.Services.History;

/// <summary>
/// Persists and queries browser history. All members are safe to call even when
/// the database is unavailable: failures are recorded instead of thrown so the
/// browser keeps working.
/// </summary>
public interface IHistoryService
{
    /// <summary>False once a database operation has failed.</summary>
    bool IsAvailable { get; }

    /// <summary>Last failure message, if any.</summary>
    string? LastError { get; }

    Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HistoryEntry>> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a visit. Consecutive visits to the same URL collapse into a single
    /// entry (refreshed timestamp) instead of creating duplicates.
    /// </summary>
    Task AddAsync(string url, string? title, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
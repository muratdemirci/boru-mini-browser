using MiniBrowser.Core.Models;

namespace MiniBrowser.Services.Bookmarks;

/// <summary>
/// Persists and queries user bookmarks. All members are safe to call even when
/// the database is unavailable: failures are recorded instead of thrown so the
/// browser keeps working.
/// </summary>
public interface IBookmarkService
{
    /// <summary>False once a database operation has failed.</summary>
    bool IsAvailable { get; }

    /// <summary>Last failure message, if any.</summary>
    string? LastError { get; }

    Task<IReadOnlyList<Bookmark>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a bookmark unless the URL is already bookmarked.
    /// </summary>
    Task AddAsync(string url, string? title, CancellationToken cancellationToken = default);

    Task RemoveAsync(string url, CancellationToken cancellationToken = default);

    Task<bool> IsBookmarkedAsync(string url, CancellationToken cancellationToken = default);
}
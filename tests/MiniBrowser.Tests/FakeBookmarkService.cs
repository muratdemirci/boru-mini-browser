using MiniBrowser.Core.Models;
using MiniBrowser.Services.Bookmarks;

namespace MiniBrowser.Tests;

/// <summary>
/// In-memory IBookmarkService for ViewModel tests. Matches the real service's
/// duplicate-prevention semantics (case-insensitive on the URL).
/// </summary>
public sealed class FakeBookmarkService : IBookmarkService
{
    public List<Bookmark> Items { get; } = [];

    public bool IsAvailable { get; set; } = true;

    public string? LastError { get; set; }

    public Task<IReadOnlyList<Bookmark>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Bookmark> result = Items.ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(string url, string? title, CancellationToken cancellationToken = default)
    {
        var alreadyBookmarked = Items.Any(b =>
            string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase));
        if (!alreadyBookmarked)
        {
            Items.Add(new Bookmark { Id = Items.Count + 1, Url = url, Title = title, CreatedAt = DateTime.UtcNow });
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string url, CancellationToken cancellationToken = default)
    {
        Items.RemoveAll(b => string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase));
        return Task.CompletedTask;
    }

    public Task<bool> IsBookmarkedAsync(string url, CancellationToken cancellationToken = default)
    {
        var result = Items.Any(b => string.Equals(b.Url, url, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(result);
    }
}
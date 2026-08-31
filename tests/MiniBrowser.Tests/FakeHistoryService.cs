using MiniBrowser.Core.Models;
using MiniBrowser.Services.History;

namespace MiniBrowser.Tests;

/// <summary>
/// In-memory IHistoryService for ViewModel tests. Records visits in call order
/// so navigation-event recording can be asserted without a real database.
/// </summary>
public sealed class FakeHistoryService : IHistoryService
{
    public List<HistoryEntry> Entries { get; } = [];

    public bool IsAvailable { get; set; } = true;

    public string? LastError { get; set; }

    public Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HistoryEntry> result = Entries
            .OrderByDescending(e => e.VisitedAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<HistoryEntry>> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetRecentAsync(limit, cancellationToken);
        }

        IReadOnlyList<HistoryEntry> result = Entries
            .Where(e => e.Url.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || (e.Title?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(e => e.VisitedAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(string url, string? title, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Mirrors the real service: collapse consecutive visits to the same URL.
        if (Entries.Count > 0
            && string.Equals(Entries[^1].Url, url, StringComparison.OrdinalIgnoreCase))
        {
            var last = Entries[^1];
            last.Title = title ?? last.Title;
            last.VisitedAt = now;
            return Task.CompletedTask;
        }

        Entries.Add(new HistoryEntry { Id = Entries.Count + 1, Url = url, Title = title, VisitedAt = now });
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Entries.Clear();
        return Task.CompletedTask;
    }
}
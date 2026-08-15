using Microsoft.EntityFrameworkCore;
using MiniBrowser.Core.Models;
using MiniBrowser.Infrastructure.Persistence;

namespace MiniBrowser.Services.History;

public sealed class HistoryService : IHistoryService
{
    private readonly IDbContextFactory<BrowserDbContext> _factory;

    public HistoryService(IDbContextFactory<BrowserDbContext> factory)
    {
        _factory = factory;
    }

    public bool IsAvailable { get; private set; } = true;

    public string? LastError { get; private set; }

    public async Task<IReadOnlyList<HistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async (db, ct) => await db.HistoryEntries
            .OrderByDescending(h => h.VisitedAt)
            .Take(limit)
            .ToListAsync(ct), cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<HistoryEntry>> SearchAsync(string? query, int limit, CancellationToken cancellationToken = default)
    {
        var needle = query?.Trim() ?? string.Empty;
        if (needle.Length == 0)
        {
            return await GetRecentAsync(limit, cancellationToken);
        }

        // Escape LIKE wildcards so user input is matched literally. SQLite's
        // LIKE is case-insensitive for ASCII, which is what we want for search.
        var escape = "\\";
        var pattern = $"%{needle.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";

        return await ExecuteAsync(async (db, ct) => await db.HistoryEntries
            .Where(h => EF.Functions.Like(h.Url, pattern, escape) || (h.Title != null && EF.Functions.Like(h.Title, pattern, escape)))
            .OrderByDescending(h => h.VisitedAt)
            .Take(limit)
            .ToListAsync(ct), cancellationToken) ?? [];
    }

    public async Task AddAsync(string url, string? title, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url?.Trim() ?? string.Empty;
        if (normalizedUrl.Length == 0)
        {
            return;
        }

        var normalizedTitle = NormalizeTitle(title);

        await ExecuteAsync(async (db, ct) =>
        {
            var last = await db.HistoryEntries
                .OrderByDescending(h => h.VisitedAt)
                .FirstOrDefaultAsync(ct);

            var now = DateTime.UtcNow;

            if (last is not null && string.Equals(last.Url, normalizedUrl, StringComparison.OrdinalIgnoreCase))
            {
                // Consecutive visit to the same URL: refresh instead of duplicating.
                last.VisitedAt = now;
                if (normalizedTitle is not null)
                {
                    last.Title = normalizedTitle;
                }
            }
            else
            {
                db.HistoryEntries.Add(new HistoryEntry
                {
                    Url = normalizedUrl,
                    Title = normalizedTitle,
                    VisitedAt = now,
                });
            }

            await db.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async (db, ct) =>
        {
            await db.HistoryEntries.ExecuteDeleteAsync(ct);
        }, cancellationToken);
    }

    private async Task ExecuteAsync(
        Func<BrowserDbContext, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            return;
        }

        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            await operation(db, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RecordFailure(ex);
        }
    }

    private async Task<T?> ExecuteAsync<T>(
        Func<BrowserDbContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            return default;
        }

        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            return await operation(db, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RecordFailure(ex);
            return default;
        }
    }

    private void RecordFailure(Exception ex)
    {
        IsAvailable = false;
        LastError = ex.Message;
    }

    private static string? NormalizeTitle(string? title)
    {
        var trimmed = title?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
using Microsoft.EntityFrameworkCore;
using MiniBrowser.Core.Models;
using MiniBrowser.Infrastructure.Persistence;

namespace MiniBrowser.Services.Bookmarks;

public sealed class BookmarkService : IBookmarkService
{
    private readonly IDbContextFactory<BrowserDbContext> _factory;

    public BookmarkService(IDbContextFactory<BrowserDbContext> factory)
    {
        _factory = factory;
    }

    public bool IsAvailable { get; private set; } = true;

    public string? LastError { get; private set; }

    public async Task<IReadOnlyList<Bookmark>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(async (db, ct) => await db.Bookmarks
            .OrderBy(b => b.CreatedAt)
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
            var existing = await db.Bookmarks
                .Where(b => b.Url == normalizedUrl)
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
            {
                // A bookmark for this URL already exists (also checked below in
                // memory so URL casing differences are caught).
                if (normalizedTitle is not null && string.IsNullOrEmpty(existing.Title))
                {
                    existing.Title = normalizedTitle;
                    await db.SaveChangesAsync(ct);
                }

                return;
            }

            var all = await db.Bookmarks.Select(b => b.Url).ToListAsync(ct);
            if (all.Any(b => string.Equals(b, normalizedUrl, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            db.Bookmarks.Add(new Bookmark
            {
                Url = normalizedUrl,
                Title = normalizedTitle,
                CreatedAt = DateTime.UtcNow,
            });

            await db.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    public async Task RemoveAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url?.Trim() ?? string.Empty;
        if (normalizedUrl.Length == 0)
        {
            return;
        }

        await ExecuteAsync(async (db, ct) =>
        {
            var existing = await db.Bookmarks
                .Where(b => b.Url == normalizedUrl)
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
            {
                db.Bookmarks.Remove(existing);
                await db.SaveChangesAsync(ct);
                return;
            }

            var all = await db.Bookmarks.ToListAsync(ct);
            var match = all.FirstOrDefault(b => string.Equals(b.Url, normalizedUrl, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                db.Bookmarks.Remove(match);
                await db.SaveChangesAsync(ct);
            }
        }, cancellationToken);
    }

    public async Task<bool> IsBookmarkedAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url?.Trim() ?? string.Empty;
        if (normalizedUrl.Length == 0)
        {
            return false;
        }

        return await ExecuteAsync(async (db, ct) =>
        {
            var urls = await db.Bookmarks.Select(b => b.Url).ToListAsync(ct);
            return (bool?)urls.Any(b => string.Equals(b, normalizedUrl, StringComparison.OrdinalIgnoreCase));
        }, cancellationToken) ?? false;
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
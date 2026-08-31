using Microsoft.EntityFrameworkCore;
using MiniBrowser.Core.Models;
using MiniBrowser.Infrastructure.Persistence;
using MiniBrowser.Services.Bookmarks;
using MiniBrowser.Services.History;

namespace MiniBrowser.Tests;

public sealed class DatabaseTests
{
    [Fact]
    public void EnsureCreated_CreatesSchema()
    {
        var path = GetTempPath();

        using var context = BrowserDbContextFactory.Create($"Data Source={path}");
        var created = context.Database.EnsureCreated();

        Assert.True(created);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void EnsureCreated_IsIdempotent()
    {
        var path = GetTempPath();

        using (var context = BrowserDbContextFactory.Create($"Data Source={path}"))
        {
            Assert.True(context.Database.EnsureCreated());
        }

        using (var context = BrowserDbContextFactory.Create($"Data Source={path}"))
        {
            Assert.False(context.Database.EnsureCreated());
        }
    }

    [Fact]
    public async Task Data_SurvivesRestart_WhenContextIsReopened()
    {
        var path = GetTempPath();

        await using (var context = BrowserDbContextFactory.Create($"Data Source={path}"))
        {
            context.Database.EnsureCreated();
            context.HistoryEntries.Add(new HistoryEntry
            {
                Url = "https://example.com/",
                Title = "Example",
                VisitedAt = DateTime.UtcNow,
            });
            context.Bookmarks.Add(new Bookmark
            {
                Url = "https://example.com/",
                Title = "Example",
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();
        }

        await using (var reopened = BrowserDbContextFactory.Create($"Data Source={path}"))
        {
            Assert.Equal(1, await reopened.HistoryEntries.CountAsync());
            Assert.Equal(1, await reopened.Bookmarks.CountAsync());
        }
    }

    [Fact]
    public async Task HistoryService_DatabaseFailure_SetsUnavailable_DoesNotThrow()
    {
        var service = new HistoryService(new ThrowingDbContextFactory());

        var result = await service.GetRecentAsync(100);

        Assert.Empty(result);
        Assert.False(service.IsAvailable);
        Assert.False(string.IsNullOrEmpty(service.LastError));

        // Subsequent operations are no-ops but must never throw.
        await service.AddAsync("https://example.com/", "Example");
        await service.ClearAsync();
        Assert.False(service.IsAvailable);
    }

    [Fact]
    public async Task BookmarkService_DatabaseFailure_SetsUnavailable_DoesNotThrow()
    {
        var service = new BookmarkService(new ThrowingDbContextFactory());

        var result = await service.GetAllAsync();

        Assert.Empty(result);
        Assert.False(service.IsAvailable);
        Assert.False(string.IsNullOrEmpty(service.LastError));

        await service.AddAsync("https://example.com/", "Example");
        Assert.False(await service.IsBookmarkedAsync("https://example.com/"));
        Assert.False(service.IsAvailable);
    }

    private static string GetTempPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "MiniBrowserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "test.db");
    }
}
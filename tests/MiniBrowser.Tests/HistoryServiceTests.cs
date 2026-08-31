using Microsoft.EntityFrameworkCore;
using MiniBrowser.Core.Models;
using MiniBrowser.Services.History;

namespace MiniBrowser.Tests;

public sealed class HistoryServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly HistoryService _service;

    public HistoryServiceTests()
    {
        _service = new HistoryService(_db.Factory);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task AddAsync_ThenGetRecent_ReturnsEntriesNewestFirst()
    {
        await _service.AddAsync("https://example.com/", "Example");
        await Task.Delay(10);
        await _service.AddAsync("https://github.com/", null);

        var all = await _service.GetRecentAsync(100);

        Assert.Equal(2, all.Count);
        Assert.Equal("https://github.com/", all[0].Url);
        Assert.Null(all[0].Title);
        Assert.Equal("https://example.com/", all[1].Url);
        Assert.Equal("Example", all[1].Title);
    }

    [Fact]
    public async Task AddAsync_ConsecutiveSameUrl_CollapsesIntoSingleEntry()
    {
        await _service.AddAsync("https://example.com/", "First");
        await Task.Delay(20);
        await _service.AddAsync("https://example.com/", "Second");

        var all = await _service.GetRecentAsync(100);

        var single = Assert.Single(all);
        Assert.Equal("https://example.com/", single.Url);
        Assert.Equal("Second", single.Title);
    }

    [Fact]
    public async Task AddAsync_SameUrlAfterOtherUrl_CreatesNewEntry()
    {
        await _service.AddAsync("https://a.example/", "A");
        await Task.Delay(20);
        await _service.AddAsync("https://b.example/", "B");
        await Task.Delay(20);
        await _service.AddAsync("https://a.example/", "A again");

        var all = await _service.GetRecentAsync(100);

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task SearchAsync_MatchesTitleAndUrl_CaseInsensitive()
    {
        await _service.AddAsync("https://example.com/", "Example Dot Com");
        await Task.Delay(10);
        await _service.AddAsync("https://github.com/", "GitHub");

        var byTitle = await _service.SearchAsync("github", 100);
        Assert.Single(byTitle);
        Assert.Equal("https://github.com/", byTitle[0].Url);

        var byUrl = await _service.SearchAsync("EXAMPLE", 100);
        Assert.Single(byUrl);
        Assert.Equal("https://example.com/", byUrl[0].Url);
    }

    [Fact]
    public async Task SearchAsync_SearchesLiteralWildcards()
    {
        await _service.AddAsync("https://example.com/100%off", "Deal");

        var all = await _service.SearchAsync("100%off", 100);

        Assert.Single(all);
    }

    [Fact]
    public async Task SearchAsync_EmptyOrBlankQuery_ReturnsRecent()
    {
        await _service.AddAsync("https://a.example/", "A");
        await Task.Delay(10);
        await _service.AddAsync("https://b.example/", "B");

        var all = await _service.SearchAsync("  ", 100);

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetRecentAsync_OrdersByVisitedDescendingAndLimits()
    {
        using var db = new TestDatabase();
        var service = new HistoryService(db.Factory);

        await using (var context = db.Factory.CreateDbContext())
        {
            context.HistoryEntries.AddRange(
                new HistoryEntry { Url = "https://old.example/", Title = null, VisitedAt = DateTime.UtcNow.AddDays(-3) },
                new HistoryEntry { Url = "https://mid.example/", Title = null, VisitedAt = DateTime.UtcNow.AddDays(-1) },
                new HistoryEntry { Url = "https://new.example/", Title = null, VisitedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        var all = await service.GetRecentAsync(2);

        Assert.Equal(2, all.Count);
        Assert.Equal("https://new.example/", all[0].Url);
        Assert.Equal("https://mid.example/", all[1].Url);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        await _service.AddAsync("https://example.com/", "Example");
        await Task.Delay(10);
        await _service.AddAsync("https://github.com/", "GitHub");

        await _service.ClearAsync();

        Assert.Empty(await _service.GetRecentAsync(100));
    }

    [Fact]
    public async Task History_PersistsAcrossNewContext()
    {
        await _service.AddAsync("https://example.com/", "Example");

        await using var context = _db.Factory.CreateDbContext();

        Assert.Equal(1, await context.HistoryEntries.CountAsync());
    }
}
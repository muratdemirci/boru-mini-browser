using Microsoft.EntityFrameworkCore;
using MiniBrowser.Services.Bookmarks;

namespace MiniBrowser.Tests;

public sealed class BookmarkServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly BookmarkService _service;

    public BookmarkServiceTests()
    {
        _service = new BookmarkService(_db.Factory);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Add_ThenGetAll_ReturnsBookmark()
    {
        await _service.AddAsync("https://example.com/", "Example");

        var all = await _service.GetAllAsync();

        var bookmark = Assert.Single(all);
        Assert.Equal("https://example.com/", bookmark.Url);
        Assert.Equal("Example", bookmark.Title);
    }

    [Fact]
    public async Task Add_DuplicateExactUrl_DoesNotAddTwice()
    {
        await _service.AddAsync("https://example.com/", "One");
        await _service.AddAsync("https://example.com/", "Two");

        Assert.Single(await _service.GetAllAsync());
    }

    [Fact]
    public async Task Add_DuplicateUrlDifferentCase_DoesNotAddTwice()
    {
        await _service.AddAsync("https://Example.com/", "One");
        await _service.AddAsync("https://example.com/", "Two");

        Assert.Single(await _service.GetAllAsync());
    }

    [Fact]
    public async Task Add_BlankTitle_StoresNull()
    {
        await _service.AddAsync("https://example.com/", "   ");

        var all = await _service.GetAllAsync();

        Assert.Null(Assert.Single(all).Title);
    }

    [Fact]
    public async Task Remove_RemovesBookmarkAndUpdatesState()
    {
        await _service.AddAsync("https://example.com/", "Example");
        await _service.RemoveAsync("https://example.com/");

        Assert.Empty(await _service.GetAllAsync());
        Assert.False(await _service.IsBookmarkedAsync("https://example.com/"));
    }

    [Fact]
    public async Task Remove_CaseInsensitive()
    {
        await _service.AddAsync("https://Example.com/", "Example");
        await _service.RemoveAsync("https://example.com/");

        Assert.Empty(await _service.GetAllAsync());
    }

    [Fact]
    public async Task IsBookmarked_TrueWhenPresent_FalseWhenAbsent()
    {
        await _service.AddAsync("https://example.com/", null);

        Assert.True(await _service.IsBookmarkedAsync("https://example.com/"));
        Assert.True(await _service.IsBookmarkedAsync("HTTPS://EXAMPLE.COM/"));
        Assert.False(await _service.IsBookmarkedAsync("https://other.example/"));
    }

    [Fact]
    public async Task Bookmark_PersistsAcrossNewContext()
    {
        await _service.AddAsync("https://example.com/", "Example");

        await using var context = _db.Factory.CreateDbContext();

        Assert.Equal(1, await context.Bookmarks.CountAsync());
    }
}
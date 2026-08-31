using MiniBrowser.Core.Browser;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Tests;

/// <summary>
/// Phase 5 behavior: history recording from per-tab navigation events and the
/// bookmark star toggle/state on the main window.
/// </summary>
public sealed class MainWindowViewModelPhase5Tests
{
    private readonly FakeBrowserEngineFactory _factory = new();
    private readonly FakeHistoryService _history = new();
    private readonly FakeBookmarkService _bookmarks = new();
    private readonly FakeDownloadService _downloads = new();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelPhase5Tests()
    {
        _viewModel = new MainWindowViewModel(_factory, _history, _bookmarks, _downloads, new FakeSettingsService());
    }

    private FakeBrowserEngine NewTabAndAttach()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();
        return (FakeBrowserEngine)_viewModel.ActiveTab.Engine;
    }

    /// <summary>
    /// Attaching a tab navigates it to the start page, which is itself recorded in
    /// history. Tests that assert on recorded history clear that baseline first.
    /// </summary>
    private void ClearHistoryBaseline() => _history.Entries.Clear();

    [Fact]
    public void SuccessfulNavigation_RecordsHistoryEntry()
    {
        NewTabAndAttach();
        ClearHistoryBaseline();

        _viewModel.AddressText = "github.com";
        _viewModel.NavigateCommand.Execute(null);

        var entry = Assert.Single(_history.Entries);
        Assert.Equal("https://github.com/", entry.Url);
    }

    [Fact]
    public void FailedNavigation_IsNotRecorded()
    {
        var engine = NewTabAndAttach();
        ClearHistoryBaseline();

        engine.RaiseNavigationCompleted(new Uri("https://example.com/"), isSuccess: false);

        Assert.Empty(_history.Entries);
    }

    [Fact]
    public void NavigationWithoutUri_IsNotRecorded()
    {
        var engine = NewTabAndAttach();
        ClearHistoryBaseline();

        engine.RaiseNavigationCompleted(null, isSuccess: true);

        Assert.Empty(_history.Entries);
    }

    [Fact]
    public void TabSwitching_DoesNotCreateHistoryEntries()
    {
        NewTabAndAttach();
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();
        ClearHistoryBaseline();

        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[0]);
        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[1]);
        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[2]);

        Assert.Empty(_history.Entries);
    }

    [Fact]
    public void ClosedTab_NoLongerRecordsHistory()
    {
        var closedEngine = NewTabAndAttach();
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();
        ClearHistoryBaseline();

        _viewModel.CloseTabCommand.Execute(_viewModel.Tabs[0]);

        closedEngine.RaiseNavigationCompleted(new Uri("https://example.com/"), isSuccess: true);

        Assert.Empty(_history.Entries);
    }

    [Fact]
    public async Task ToggleBookmark_AddsAndRemovesForActiveTab()
    {
        var engine = NewTabAndAttach();
        _viewModel.AddressText = "github.com";
        _viewModel.NavigateCommand.Execute(null);
        engine.PageTitle = "GitHub";

        Assert.False(_viewModel.IsCurrentBookmarked);
        Assert.True(_viewModel.CanBookmark);

        await _viewModel.ToggleBookmarkCommand.ExecuteAsync(null);

        Assert.True(_viewModel.IsCurrentBookmarked);
        var bookmark = Assert.Single(_bookmarks.Items);
        Assert.Equal("https://github.com/", bookmark.Url);

        await _viewModel.ToggleBookmarkCommand.ExecuteAsync(null);

        Assert.False(_viewModel.IsCurrentBookmarked);
        Assert.Empty(_bookmarks.Items);
    }

    [Fact]
    public async Task BookmarkState_UpdatesOnTabSwitch()
    {
        NewTabAndAttach();
        _viewModel.AddressText = "github.com";
        _viewModel.NavigateCommand.Execute(null);

        await _viewModel.ToggleBookmarkCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsCurrentBookmarked);

        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();
        _viewModel.AddressText = "example.com";
        _viewModel.NavigateCommand.Execute(null);

        Assert.False(_viewModel.IsCurrentBookmarked);

        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[0]);
        Assert.True(_viewModel.IsCurrentBookmarked);
    }

    [Fact]
    public async Task BookmarkState_UpdatesOnNavigation()
    {
        var engine = NewTabAndAttach();
        _viewModel.AddressText = "github.com";
        _viewModel.NavigateCommand.Execute(null);

        await _viewModel.ToggleBookmarkCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsCurrentBookmarked);

        _viewModel.AddressText = "example.com";
        _viewModel.NavigateCommand.Execute(null);

        Assert.False(_viewModel.IsCurrentBookmarked);
    }

    [Fact]
    public async Task RemovingBookmarkFromWindow_RefreshesStarState()
    {
        NewTabAndAttach();
        _viewModel.AddressText = "github.com";
        _viewModel.NavigateCommand.Execute(null);

        await _viewModel.ToggleBookmarkCommand.ExecuteAsync(null);
        Assert.True(_viewModel.IsCurrentBookmarked);

        // Removing via the bookmarks window must also revert the main-window star.
        await _viewModel.Bookmarks.RefreshAsync();
        var item = Assert.Single(_viewModel.Bookmarks.Items);
        await _viewModel.Bookmarks.RemoveCommand.ExecuteAsync(item);

        Assert.False(_viewModel.IsCurrentBookmarked);
        Assert.True(_viewModel.CanBookmark);
    }

    [Fact]
    public void OpenUrlInActiveTab_NavigatesActiveTab()
    {
        var engine = NewTabAndAttach();

        _viewModel.OpenUrlInActiveTab("https://example.com/page");

        Assert.Equal(new Uri("https://example.com/page"), engine.LastNavigatedUri);
        Assert.Equal("https://example.com/page", _viewModel.AddressText);
    }
}
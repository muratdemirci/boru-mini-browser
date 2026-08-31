using MiniBrowser.Services.Downloads;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Tests;

/// <summary>
/// Phase 6: main-window routing of download-starting events to the download
/// service and the Developer Tools command on the active tab.
/// </summary>
public sealed class MainWindowViewModelPhase6Tests
{
    private readonly FakeBrowserEngineFactory _factory = new();
    private readonly FakeHistoryService _history = new();
    private readonly FakeBookmarkService _bookmarks = new();
    private readonly FakeDownloadService _downloads = new();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelPhase6Tests()
    {
        _viewModel = new MainWindowViewModel(_factory, _history, _bookmarks, _downloads, new FakeSettingsService());
    }

    private FakeBrowserEngine NewTabAndAttach()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();
        return (FakeBrowserEngine)_viewModel.ActiveTab.Engine;
    }

    [Fact]
    public void EngineDownloadStarting_IsRoutedToDownloadService()
    {
        var engine = NewTabAndAttach();

        engine.RaiseDownloadStarting("https://example.com/report.pdf", @"C:\dl\report.pdf");

        var tracked = Assert.Single(_downloads.Tracked);
        Assert.Equal("report.pdf", tracked.FileName);
        Assert.Equal("https://example.com/report.pdf", tracked.Url);
        Assert.Equal(DownloadPathHelper.SanitizeFileName("report.pdf"), Path.GetFileName(tracked.FilePath));
        Assert.Single(_viewModel.Downloads.Items);
    }

    [Fact]
    public void Download_OnlyRoutedForTheTabThatRaisedIt()
    {
        var engine = NewTabAndAttach();
        engine.RaiseDownloadStarting("https://example.com/a.pdf", @"C:\dl\a.pdf");

        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();

        Assert.Single(_downloads.Tracked);
    }

    [Fact]
    public void ClosedTab_NoLongerRoutesDownloads()
    {
        var engine = NewTabAndAttach();
        var closedTab = _viewModel.ActiveTab!;

        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();

        _viewModel.CloseTabCommand.Execute(closedTab);

        engine.RaiseDownloadStarting("https://example.com/b.pdf", @"C:\dl\b.pdf");

        Assert.Empty(_downloads.Tracked);
    }

    [Fact]
    public void OpenDevTools_ExecutesOnActiveTabEngine()
    {
        var engine = NewTabAndAttach();

        _viewModel.OpenDevToolsCommand.Execute(null);

        Assert.Equal(1, engine.OpenDevToolsCalls);
    }

    [Fact]
    public void OpenDevTools_WhenNoActiveTab_DoesNothing()
    {
        _viewModel.OpenDevToolsCommand.Execute(null);
        Assert.All(_factory.Created, e => Assert.Equal(0, e.OpenDevToolsCalls));
    }

    [Fact]
    public void NewTabSubscribesDownloadEvent_EachTabOwnsItsEngine()
    {
        NewTabAndAttach();
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();

        Assert.Equal(2, _factory.Created.Count);
        Assert.Equal(2, _viewModel.Tabs.Count);
    }
}
using MiniBrowser.Core.Browser;
using MiniBrowser.Core.Models;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Tests;

public class MainWindowViewModelTests
{
    private readonly FakeBrowserEngineFactory _factory;
    private readonly FakeHistoryService _history;
    private readonly FakeBookmarkService _bookmarks;
    private readonly FakeDownloadService _downloads;
    private readonly FakeSettingsService _settings;
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelTests()
    {
        _factory = new FakeBrowserEngineFactory();
        _history = new FakeHistoryService();
        _bookmarks = new FakeBookmarkService();
        _downloads = new FakeDownloadService();
        _settings = new FakeSettingsService();
        _viewModel = new MainWindowViewModel(_factory, _history, _bookmarks, _downloads, _settings);
    }

    /// <summary>
    /// Creates a tab and simulates the view glue attaching its WebView, which is
    /// what unlocks actual navigation.
    /// </summary>
    private FakeBrowserEngine NewTabAndAttach()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.ActiveTab!.OnWebViewAttached();
        return (FakeBrowserEngine)_viewModel.ActiveTab.Engine;
    }

    [Fact]
    public void WindowTitle_DefaultsToMiniBrowser()
    {
        Assert.Equal("MiniBrowser", _viewModel.WindowTitle);
    }

    [Fact]
    public void ActiveTab_IsNullBeforeInitialization()
    {
        Assert.Null(_viewModel.ActiveTab);
        Assert.Empty(_viewModel.Tabs);
    }

    [Fact]
    public void NavigateCommand_NavigatesActiveTabToNormalizedUrl()
    {
        var engine = NewTabAndAttach();

        _viewModel.AddressText = "github.com";
        _viewModel.NavigateCommand.Execute(null);

        Assert.Equal(new Uri("https://github.com/"), engine.LastNavigatedUri);
        Assert.Equal("https://github.com/", _viewModel.AddressText);
    }

    [Fact]
    public void NavigateCommand_KeepsExplicitScheme()
    {
        var engine = NewTabAndAttach();

        _viewModel.AddressText = "http://example.com";
        _viewModel.NavigateCommand.Execute(null);

        Assert.Equal(new Uri("http://example.com/"), engine.LastNavigatedUri);
    }

    [Fact]
    public void NavigateCommand_WithNonUrlInput_SearchesWithConfiguredEngine()
    {
        var engine = NewTabAndAttach();

        _viewModel.AddressText = "not a valid url";
        _viewModel.NavigateCommand.Execute(null);

        Assert.Equal(2, engine.NavigateCalls);
        Assert.Equal(
            new Uri("https://www.muratdemirci.org/search?q=not%20a%20valid%20url"),
            engine.LastNavigatedUri);
        Assert.Equal("https://www.muratdemirci.org/search?q=not%20a%20valid%20url", _viewModel.AddressText);
    }

    [Fact]
    public void NavigateCommand_SearchFallback_RespectsConfiguredEngine()
    {
        var engine = NewTabAndAttach();

        _settings.Current.SearchEngine = SearchEnginePreference.Bing;
        _viewModel.AddressText = "hello world";
        _viewModel.NavigateCommand.Execute(null);

        Assert.Equal(
            new Uri("https://www.bing.com/search?q=hello%20world"),
            engine.LastNavigatedUri);
    }

    [Fact]
    public void NavigateCommand_OnUnattachedTab_IsDeferredUntilAttach()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        _viewModel.AddressText = "github.com";
        _viewModel.NavigateCommand.Execute(null);

        Assert.Equal(0, engine.NavigateCalls);

        _viewModel.ActiveTab.OnWebViewAttached();
        Assert.Equal(new Uri("https://github.com/"), engine.LastNavigatedUri);
    }

    [Fact]
    public void GoBackCommand_DelegatesToActiveTabEngine()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        _viewModel.GoBackCommand.Execute(null);

        Assert.Equal(1, engine.GoBackCalls);
    }

    [Fact]
    public void GoForwardCommand_DelegatesToActiveTabEngine()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        _viewModel.GoForwardCommand.Execute(null);

        Assert.Equal(1, engine.GoForwardCalls);
    }

    [Fact]
    public void ReloadCommand_DelegatesToActiveTabEngine()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        _viewModel.ReloadCommand.Execute(null);

        Assert.Equal(1, engine.ReloadCalls);
    }

    [Fact]
    public void StopCommand_DelegatesToActiveTabEngine()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        _viewModel.StopCommand.Execute(null);

        Assert.Equal(1, engine.StopCalls);
    }

    [Fact]
    public void Commands_WithoutActiveTab_AreNoOps()
    {
        _viewModel.GoBackCommand.Execute(null);
        _viewModel.GoForwardCommand.Execute(null);
        _viewModel.ReloadCommand.Execute(null);
        _viewModel.StopCommand.Execute(null);

        Assert.Empty(_factory.Created);
    }

    [Fact]
    public async Task InitializeAsync_CreatesInitialTabWithStartPage()
    {
        await _viewModel.InitializeAsync();

        Assert.Single(_viewModel.Tabs);
        Assert.NotNull(_viewModel.ActiveTab);
        Assert.Equal("https://www.muratdemirci.org/", _viewModel.AddressText);
        Assert.Equal("https://www.muratdemirci.org/", _viewModel.ActiveTab!.Url);
    }

    [Fact]
    public void EngineState_PropagatesToActiveTabAndAddress()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        engine.IsLoading = true;
        engine.CanGoBack = true;
        engine.CanGoForward = true;
        engine.CurrentUrl = "https://example.com/page";

        Assert.True(_viewModel.ActiveTab!.IsLoading);
        Assert.True(_viewModel.ActiveTab.CanGoBack);
        Assert.True(_viewModel.ActiveTab.CanGoForward);
        Assert.Equal("https://example.com/page", _viewModel.AddressText);
    }

    [Fact]
    public void PageTitle_UpdatesWindowTitle()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        engine.PageTitle = "GitHub";

        Assert.Equal("GitHub · MiniBrowser", _viewModel.WindowTitle);
    }

    [Fact]
    public void WindowTitle_FallsBackToMiniBrowser_WhenPageHasNoTitle()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        engine.PageTitle = "   ";

        Assert.Equal("MiniBrowser", _viewModel.WindowTitle);
    }

    [Fact]
    public void WindowTitle_FollowsActiveTabWhenSwitching()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        var engine0 = (FakeBrowserEngine)_viewModel.Tabs[0].Engine;
        var engine1 = (FakeBrowserEngine)_viewModel.Tabs[1].Engine;

        engine0.PageTitle = "First";
        engine1.PageTitle = "Second";

        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[0]);
        Assert.Equal("First · MiniBrowser", _viewModel.WindowTitle);

        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[1]);
        Assert.Equal("Second · MiniBrowser", _viewModel.WindowTitle);
    }

    [Fact]
    public void EmptyAddress_DoesNotNavigate()
    {
        _viewModel.NewTabCommand.Execute(null);
        var engine = (FakeBrowserEngine)_viewModel.ActiveTab!.Engine;

        _viewModel.AddressText = string.Empty;
        _viewModel.NavigateCommand.Execute(null);

        Assert.Equal(0, engine.NavigateCalls);
    }

    [Fact]
    public void Dispose_DisposesAllEngines()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        _viewModel.Dispose();

        Assert.All(_factory.Created, e => Assert.True(e.IsDisposed));
    }

    [Fact]
    public void NewTab_UsesConfiguredHomepage()
    {
        _settings.Current.Homepage = "https://duckduckgo.com";

        var engine = NewTabAndAttach();

        Assert.Equal(new Uri("https://duckduckgo.com/"), engine.LastNavigatedUri);
    }

    [Fact]
    public void NewTab_InvalidHomepage_FallsBackToDefault()
    {
        _settings.Current.Homepage = "not a url";

        var engine = NewTabAndAttach();

        Assert.Equal(new Uri("https://www.muratdemirci.org/"), engine.LastNavigatedUri);
    }

    [Fact]
    public async Task InitializeAsync_WithHomepageStartup_OpensHomepage()
    {
        _settings.Current.Homepage = "https://example.com";

        await _viewModel.InitializeAsync();

        Assert.Single(_viewModel.Tabs);
        Assert.Equal("https://example.com/", _viewModel.Tabs[0].Url);
    }

    [Fact]
    public async Task InitializeAsync_WithNewTabStartup_OpensEmptyTab()
    {
        _settings.Current.StartupBehavior = StartupBehavior.NewTab;

        await _viewModel.InitializeAsync();

        Assert.Single(_viewModel.Tabs);
        Assert.Equal(0, ((FakeBrowserEngine)_viewModel.Tabs[0].Engine).NavigateCalls);
    }
}
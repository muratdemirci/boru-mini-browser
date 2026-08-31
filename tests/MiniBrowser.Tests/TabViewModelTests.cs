using MiniBrowser.Core.Browser;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Tests;

public class TabViewModelTests
{
    private static readonly Uri StartUri = new("https://www.muratdemirci.org/");

    private readonly FakeBrowserEngine _engine;
    private readonly TabViewModel _tab;

    public TabViewModelTests()
    {
        _engine = new FakeBrowserEngine();
        _tab = new TabViewModel(_engine, StartUri);
    }

    [Fact]
    public void NewTab_DefaultsToNewTabTitle()
    {
        Assert.Equal("New Tab", _tab.Title);
        Assert.Equal("MiniBrowser", _tab.WindowTitle);
    }

    [Fact]
    public void Url_FallsBackToPendingStartUri()
    {
        Assert.Equal(StartUri.AbsoluteUri, _tab.Url);
    }

    [Fact]
    public void Url_ReflectsEngineCurrentUrl()
    {
        _engine.CurrentUrl = "https://example.com/page";

        Assert.Equal("https://example.com/page", _tab.Url);
    }

    [Fact]
    public void OnWebViewAttached_NavigatesPendingStartUri()
    {
        _tab.OnWebViewAttached();

        Assert.Equal(1, _engine.NavigateCalls);
        Assert.Equal(StartUri, _engine.LastNavigatedUri);
    }

    [Fact]
    public async Task NavigateAsync_BeforeAttach_IsDeferredUntilAttach()
    {
        var target = new Uri("https://example.com/");
        await _tab.NavigateAsync(target);

        Assert.Equal(0, _engine.NavigateCalls);

        _tab.OnWebViewAttached();

        Assert.Equal(1, _engine.NavigateCalls);
        Assert.Equal(target, _engine.LastNavigatedUri);
    }

    [Fact]
    public void OnWebViewAttached_IsIdempotent()
    {
        _tab.OnWebViewAttached();
        _tab.OnWebViewAttached();

        Assert.Equal(1, _engine.NavigateCalls);
    }

    [Fact]
    public void PageTitle_UpdatesTabTitleAndWindowTitle()
    {
        _engine.PageTitle = "GitHub";

        Assert.Equal("GitHub", _tab.Title);
        Assert.Equal("GitHub · MiniBrowser", _tab.WindowTitle);
    }

    [Fact]
    public void BlankPageTitle_FallsBackToNewTab()
    {
        _engine.PageTitle = "   ";

        Assert.Equal("New Tab", _tab.Title);
        Assert.Equal("MiniBrowser", _tab.WindowTitle);
    }

    [Fact]
    public void EngineState_PropagatesToTab()
    {
        _engine.IsLoading = true;
        _engine.CanGoBack = true;
        _engine.CanGoForward = true;

        Assert.True(_tab.IsLoading);
        Assert.False(_tab.IsNotLoading);
        Assert.True(_tab.CanGoBack);
        Assert.True(_tab.CanGoForward);
    }

    [Fact]
    public void NavigationState_ForwardsFromEngine()
    {
        _engine.NavigationState = BrowserNavigationState.Error;

        Assert.Equal(BrowserNavigationState.Error, _tab.NavigationState);
        Assert.True(_tab.HasNavigationError);

        _engine.NavigationState = BrowserNavigationState.Loaded;
        Assert.False(_tab.HasNavigationError);
    }

    [Fact]
    public void NavigationCommands_DelegateToEngine()
    {
        _tab.GoBack();
        _tab.GoForward();
        _tab.Reload();
        _tab.Stop();

        Assert.Equal(1, _engine.GoBackCalls);
        Assert.Equal(1, _engine.GoForwardCalls);
        Assert.Equal(1, _engine.ReloadCalls);
        Assert.Equal(1, _engine.StopCalls);
    }

    [Fact]
    public void Dispose_DisposesEngine()
    {
        _tab.Dispose();

        Assert.True(_engine.IsDisposed);
    }
}
using System.ComponentModel;
using MiniBrowser.Core.Browser;

namespace MiniBrowser.Tests;

internal sealed class FakeBrowserEngine : IBrowserEngine
{
    private string _currentUrl = string.Empty;
    private string _pageTitle = string.Empty;
    private bool _isLoading;
    private bool _canGoBack;
    private bool _canGoForward;
    private BrowserNavigationState _navigationState;

    public string CurrentUrl
    {
        get => _currentUrl;
        set
        {
            _currentUrl = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentUrl)));
        }
    }

    public string PageTitle
    {
        get => _pageTitle;
        set
        {
            _pageTitle = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PageTitle)));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
        }
    }

    public bool CanGoBack
    {
        get => _canGoBack;
        set
        {
            _canGoBack = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoBack)));
        }
    }

    public bool CanGoForward
    {
        get => _canGoForward;
        set
        {
            _canGoForward = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanGoForward)));
        }
    }

    public BrowserNavigationState NavigationState
    {
        get => _navigationState;
        set
        {
            _navigationState = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(NavigationState)));
        }
    }

    public Uri? FaviconUrl { get; set; }

    public double ZoomFactor { get; private set; } = 1.0;

    public bool IsSecureConnection { get; set; } = false;

    public Uri? LastNavigatedUri { get; private set; }

    public bool IsDisposed { get; private set; }

    public int NavigateCalls { get; private set; }

    public int GoBackCalls { get; private set; }

    public int GoForwardCalls { get; private set; }

    public int ReloadCalls { get; private set; }

    public int StopCalls { get; private set; }

    public int OpenDevToolsCalls { get; private set; }

    public bool OpenDevToolsResult { get; set; } = true;

    public int FindCalls { get; private set; }

    public string? LastFindText { get; private set; }

    public bool LastFindForward { get; private set; }

    public FindInPageResult FindResult { get; set; } = FindInPageResult.None;

    public int ClearFindCalls { get; private set; }

    public string? LastNavigatedToString { get; private set; }

    public double LastSetZoom { get; private set; } = 1.0;

    public int SetZoomCalls { get; private set; }

    /// <summary>
    /// Simulates the WebView2 download flow: raises <see cref="DownloadStarting"/>
    /// with the supplied arguments so view-model routing can be tested.
    /// </summary>
    public void RaiseDownloadStarting(
        string? uri,
        string? suggestedPath,
        IDownloadOperation? operation = null)
    {
        operation ??= new FakeDownloadOperation();
        DownloadStarting?.Invoke(this, new DownloadStartingEventArgs(uri, null, suggestedPath, operation));
    }

    public event EventHandler<BrowserNavigationCompletedEventArgs>? NavigationCompleted;

    public event EventHandler<DownloadStartingEventArgs>? DownloadStarting;

    public event PropertyChangedEventHandler? PropertyChanged;

    public Task NavigateAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        NavigateCalls++;
        LastNavigatedUri = uri;
        CurrentUrl = uri.AbsoluteUri;
        NavigationCompleted?.Invoke(this, new BrowserNavigationCompletedEventArgs(uri, true));
        return Task.CompletedTask;
    }

    public void NavigateToString(string html)
    {
        LastNavigatedToString = html;
    }

    public Task<FindInPageResult> FindAsync(string text, bool forward, CancellationToken cancellationToken = default)
    {
        FindCalls++;
        LastFindText = text;
        LastFindForward = forward;
        return Task.FromResult(FindResult);
    }

    public Task ClearFindAsync(CancellationToken cancellationToken = default)
    {
        ClearFindCalls++;
        return Task.CompletedTask;
    }

    public Task SetZoomFactorAsync(double factor, CancellationToken cancellationToken = default)
    {
        SetZoomCalls++;
        LastSetZoom = factor;
        ZoomFactor = factor;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ZoomFactor)));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lets tests raise the completion event directly (e.g. to simulate a failed
    /// navigation) without routing through <see cref="NavigateAsync"/>.
    /// </summary>
    public void RaiseNavigationCompleted(Uri? uri, bool isSuccess, string? title = null)
    {
        NavigationCompleted?.Invoke(this, new BrowserNavigationCompletedEventArgs(uri, isSuccess, title));
    }

    public void GoBack() => GoBackCalls++;

    public void GoForward() => GoForwardCalls++;

    public void Reload() => ReloadCalls++;

    public void Stop() => StopCalls++;

    public bool OpenDevTools()
    {
        OpenDevToolsCalls++;
        return OpenDevToolsResult;
    }

    public void Dispose() => IsDisposed = true;
}
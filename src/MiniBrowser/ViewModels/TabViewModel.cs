using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MiniBrowser.Core.Browser;

namespace MiniBrowser.ViewModels;

/// <summary>
/// View model for a single browser tab. Each tab owns its own
/// <see cref="IBrowserEngine"/> instance and forwards engine state to the UI.
/// </summary>
public partial class TabViewModel : ViewModelBase, IDisposable
{
    private static int _nextId;

    private readonly IBrowserEngine _engine;
    private Uri? _startUri;
    private string? _newTabContent;
    private bool _attached;
    private bool _disposed;

    public TabViewModel(IBrowserEngine engine, Uri? startUri = null, string? newTabContent = null)
    {
        ArgumentNullException.ThrowIfNull(engine);

        _engine = engine;
        _startUri = startUri;
        _newTabContent = newTabContent;
        _engine.PropertyChanged += OnEnginePropertyChanged;
        _engine.NavigationCompleted += OnEngineNavigationCompleted;
    }

    /// <summary>Stable identifier for this tab within the session.</summary>
    public int Id { get; } = Interlocked.Increment(ref _nextId);

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private string title = "New Tab";

    [ObservableProperty]
    private string windowTitle = "MiniBrowser";

    public IBrowserEngine Engine => _engine;

    /// <summary>Raw page title from the engine, without the "New Tab" fallback.</summary>
    public string PageTitle => _engine.PageTitle;

    /// <summary>
    /// Raised whenever a navigation completes on this tab (success or failure),
    /// carrying the completion details.
    /// </summary>
    public event EventHandler<BrowserNavigationCompletedEventArgs>? Navigated;

    public string Url => !string.IsNullOrEmpty(_engine.CurrentUrl)
        ? _engine.CurrentUrl
        : _startUri?.AbsoluteUri ?? string.Empty;

    public bool IsLoading => _engine.IsLoading;

    public bool IsNotLoading => !_engine.IsLoading;

    public bool CanGoBack => _engine.CanGoBack;

    public bool CanGoForward => _engine.CanGoForward;

    public BrowserNavigationState NavigationState => _engine.NavigationState;

    public bool HasNavigationError => _engine.NavigationState == BrowserNavigationState.Error;

    public Uri? FaviconUrl => _engine.FaviconUrl;

    public double ZoomFactor => _engine.ZoomFactor;

    public bool IsSecureConnection => _engine.IsSecureConnection;

    public async Task<FindInPageResult> FindAsync(string text, bool forward, CancellationToken cancellationToken = default)
    {
        var result = await _engine.FindAsync(text, forward, cancellationToken);
        OnPropertyChanged(nameof(ZoomFactor));
        return result;
    }

    public Task ClearFindAsync(CancellationToken cancellationToken = default)
        => _engine.ClearFindAsync(cancellationToken);

    public async Task SetZoomFactorAsync(double factor, CancellationToken cancellationToken = default)
    {
        await _engine.SetZoomFactorAsync(factor, cancellationToken);
        OnPropertyChanged(nameof(ZoomFactor));
    }

    /// <summary>
    /// Called by the view glue once the tab's WebView control has attached this
    /// tab's engine. Triggers any navigation that was deferred until attachment.
    /// </summary>
    public void OnWebViewAttached()
    {
        if (_attached)
        {
            return;
        }

        _attached = true;

        if (_startUri is not null)
        {
            var pending = _startUri;
            _startUri = null;
            _ = NavigateAsync(pending);
        }
        else if (_newTabContent is not null)
        {
            var content = _newTabContent;
            _newTabContent = null;
            _engine.NavigateToString(content);
        }
    }

    public async Task NavigateAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (!_attached)
        {
            // The engine is not bound to a WebView yet (for example right after a
            // new tab is created). Defer the navigation until attachment.
            _startUri = uri;
            OnPropertyChanged(nameof(Url));
            return;
        }

        await _engine.NavigateAsync(uri, cancellationToken);
    }

    public void GoBack() => _engine.GoBack();

    public void GoForward() => _engine.GoForward();

    public void Reload() => _engine.Reload();

    public void Stop() => _engine.Stop();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.PropertyChanged -= OnEnginePropertyChanged;
        _engine.NavigationCompleted -= OnEngineNavigationCompleted;
        _engine.Dispose();
    }

    private void OnEngineNavigationCompleted(object? sender, BrowserNavigationCompletedEventArgs e)
    {
        Navigated?.Invoke(this, e);
    }

    private void OnEnginePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IBrowserEngine.CurrentUrl):
                OnPropertyChanged(nameof(Url));
                break;

            case nameof(IBrowserEngine.PageTitle):
                UpdateTitle();
                break;

            case nameof(IBrowserEngine.IsLoading):
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(IsNotLoading));
                break;

            case nameof(IBrowserEngine.CanGoBack):
                OnPropertyChanged(nameof(CanGoBack));
                break;

            case nameof(IBrowserEngine.CanGoForward):
                OnPropertyChanged(nameof(CanGoForward));
                break;

            case nameof(IBrowserEngine.NavigationState):
                OnPropertyChanged(nameof(NavigationState));
                OnPropertyChanged(nameof(HasNavigationError));
                break;

            case nameof(IBrowserEngine.ZoomFactor):
                OnPropertyChanged(nameof(ZoomFactor));
                break;

            case nameof(IBrowserEngine.FaviconUrl):
                OnPropertyChanged(nameof(FaviconUrl));
                break;
        }
    }

    private void UpdateTitle()
    {
        var trimmed = _engine.PageTitle?.Trim() ?? string.Empty;

        Title = string.IsNullOrEmpty(trimmed) ? "New Tab" : trimmed;
        WindowTitle = string.IsNullOrEmpty(trimmed) ? "MiniBrowser" : $"{trimmed} · MiniBrowser";
    }
}
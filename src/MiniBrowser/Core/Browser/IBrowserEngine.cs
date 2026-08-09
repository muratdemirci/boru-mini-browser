using System.ComponentModel;

namespace MiniBrowser.Core.Browser;

/// <summary>
/// Abstraction over the underlying web rendering engine. Keeps browser-engine
/// specifics (e.g. WebView2) out of view models so the implementation can be
/// replaced without touching UI or business logic.
/// </summary>
public interface IBrowserEngine : INotifyPropertyChanged, IDisposable
{
    string CurrentUrl { get; }

    string PageTitle { get; }

    bool IsLoading { get; }

    bool CanGoBack { get; }

    bool CanGoForward { get; }

    /// <summary>Lifecycle state of the current navigation.</summary>
    BrowserNavigationState NavigationState { get; }

    /// <summary>
    /// Site favicon location, if the underlying engine exposes one.
    /// Kept as a placeholder abstraction for the upcoming tab system;
    /// favicon fetching is deliberately not implemented yet.
    /// </summary>
    Uri? FaviconUrl { get; }

    /// <summary>Raised when the engine reports a download starting.</summary>
    event EventHandler<DownloadStartingEventArgs>? DownloadStarting;

/// <summary>Current page zoom factor (1.0 = 100%). The engine reapplies it after
    /// each navigation so it persists across page loads.</summary>
    double ZoomFactor { get; }

    /// <summary>True when the current page is served over HTTPS.</summary>
    bool IsSecureConnection { get; }

    Task NavigateAsync(Uri uri, CancellationToken cancellationToken = default);

    /// <summary>Renders the supplied HTML as the top-level document (no navigation URL).</summary>
    void NavigateToString(string html);

    /// <summary>
    /// Searches the current document for <paramref name="text"/> and moves the
    /// active highlight to the next or previous match.
    /// </summary>
    /// <returns>The total match count and the zero-based index of the active match.</returns>
    Task<FindInPageResult> FindAsync(string text, bool forward, CancellationToken cancellationToken = default);

    /// <summary>Clears any find-in-page highlighting.</summary>
    Task ClearFindAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a page zoom factor (e.g. 1.25 = 125%). The engine reapplies it
    /// after each navigation so zoom persists across page loads.
    /// </summary>
    Task SetZoomFactorAsync(double factor, CancellationToken cancellationToken = default);

    void GoBack();

    void GoForward();

    void Reload();

    void Stop();

    /// <summary>
    /// Opens the developer tools window for this engine's page.
    /// </summary>
    /// <returns>True if the tools were opened, false if the engine cannot.</returns>
    bool OpenDevTools();

    event EventHandler<BrowserNavigationCompletedEventArgs>? NavigationCompleted;
}
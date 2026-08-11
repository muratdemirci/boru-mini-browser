using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Platform;
using MiniBrowser.Core.Browser;

namespace MiniBrowser.Infrastructure.WebView;

/// <summary>
/// <see cref="IBrowserEngine"/> implementation backed by Avalonia's <see cref="NativeWebView"/>,
/// which renders with the platform's native engine (WebView2 on Windows).
/// </summary>
public sealed class NativeWebViewEngine : IBrowserEngine
{
    private NativeWebView? _control;
    private bool _isLoading;
    private BrowserNavigationState _navigationState;
    private bool _stopRequested;
    private string _currentUrl = string.Empty;
    private string _pageTitle = string.Empty;
    private bool _disposed;
    private readonly string _userDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MiniBrowser", "WebView2");

    private IntPtr _coreWebView2;
    private IntPtr _coreWebView2Controller;
    private IntPtr _downloadStartingHandlerPtr;
    private long _downloadStartingToken;
    private IntPtr _acceleratorHandlerPtr;
    private long _acceleratorToken;
    private double _zoomFactor = 1.0;
    private Uri? _faviconUrl = null;
    private DateTime _navigationStart = DateTime.MinValue;
    private bool _navigationStarted = false;

    public double ZoomFactor => _zoomFactor;

    public Uri? FaviconUrl
    {
        get => _faviconUrl;
        set
        {
            if (_faviconUrl != value)
            {
                _faviconUrl = value;
                OnPropertyChanged(nameof(FaviconUrl));
            }
        }
    }

    public bool IsSecureConnection { get; private set; } = false;

    // Find-in-page helper injected into the document. It keeps a page-level
    // state so consecutive FindAsync calls move the active match without
    // rebuilding the highlight set. Runs as a single expression so WebView2
    // returns the JSON serialization of the completion value ({count,index}).
    private const string FindScriptTemplate =
        """
        (function () {
          var findKey = '__miniBrowserFindState__';
          var state = window[findKey] = window[findKey] || { text: null, marks: [], active: -1 };
          if (state.text !== null && state.text !== __TEXT__) {
            for (var i = 0; i < state.marks.length; i++) {
              var m = state.marks[i];
              if (m.parentNode) { m.replaceWith(document.createTextNode(m.textContent)); }
            }
            state.marks = [];
            state.active = -1;
            state.text = null;
          }
          var text = __TEXT__;
          var forward = __FORWARD__;
          if (text && state.marks.length === 0) {
            var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT);
            var nodes = [];
            var node;
            while ((node = walker.nextNode())) {
              if (node.nodeValue && node.nodeValue.toLowerCase().indexOf(text.toLowerCase()) !== -1) {
                nodes.push(node);
              }
            }
            var re = new RegExp(text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
            for (var n = 0; n < nodes.length; n++) {
              var textNode = nodes[n];
              var value = textNode.nodeValue;
              var positions = [];
              var match;
              while ((match = re.exec(value)) !== null) { positions.push(match.index); re.lastIndex = match.index + 1; }
              for (var p = positions.length - 1; p >= 0; p--) {
                var range = document.createRange();
                range.setStart(textNode, positions[p]);
                range.setEnd(textNode, positions[p] + text.length);
                var mark = document.createElement('mark');
                mark.setAttribute('data-minibrowser-find', '');
                try {
                  range.surroundContents(mark);
                } catch (e) { continue; }
                state.marks.push(mark);
              }
            }
            state.text = text;
          }
          if (state.marks.length === 0) { return { count: 0, index: -1 }; }
          if (state.active < 0) { state.active = forward ? 0 : state.marks.length - 1; }
          else if (forward) { state.active = (state.active + 1) % state.marks.length; }
          else { state.active = (state.active - 1 + state.marks.length) % state.marks.length; }
          var activeMark = state.marks[state.active];
          if (activeMark.scrollIntoView) { activeMark.scrollIntoView({ block: 'center', inline: 'center' }); }
          return { count: state.marks.length, index: state.active };
        })()
        """;

    private const string ClearFindScript =
        """
        (function () {
          var state = window['__miniBrowserFindState__'];
          if (state) {
            for (var i = 0; i < state.marks.length; i++) {
              var m = state.marks[i];
              if (m.parentNode) { m.replaceWith(document.createTextNode(m.textContent)); }
            }
            state.marks = [];
            state.active = -1;
            state.text = null;
          }
          return null;
        })()
        """;

    private const string FaviconScript =
        """
        (function () {
          var a = document.querySelector('link[rel~="icon"]');
          return a ? { href: a.href } : null;
        })()
        """;

    public NativeWebViewEngine()
    {
    }

    public string CurrentUrl => _currentUrl;

    public string PageTitle => _pageTitle;

    public bool IsLoading => _isLoading;

    public BrowserNavigationState NavigationState => _navigationState;

    public bool CanGoBack => _control?.CanGoBack ?? false;

    public bool CanGoForward => _control?.CanGoForward ?? false;

    public event EventHandler<BrowserNavigationCompletedEventArgs>? NavigationCompleted;

    public event EventHandler<DownloadStartingEventArgs>? DownloadStarting;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Binds the engine to the visual <see cref="NativeWebView"/> control.
    /// Must be called once, from the UI thread, before the control is used.
    /// </summary>
    public void Attach(NativeWebView control)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(control);

        if (_control is not null)
        {
            throw new InvalidOperationException("A WebView control is already attached to this engine.");
        }

        _control = control;
        _control.EnvironmentRequested += OnEnvironmentRequested;
        _control.NavigationStarted += OnNavigationStarted;
        _control.NavigationCompleted += OnNavigationCompleted;
        _control.AdapterCreated += OnAdapterCreated;
        _control.AdapterDestroyed += OnAdapterDestroyed;
    }

    public Task NavigateAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfNotAttached();

        _control!.Navigate(uri);
        return Task.CompletedTask;
    }

    public void NavigateToString(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        ThrowIfNotAttached();

        _control!.NavigateToString(html);
    }

    public async Task<FindInPageResult> FindAsync(string text, bool forward, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_control is null || string.IsNullOrEmpty(text))
        {
            return FindInPageResult.None;
        }

        try
        {
            var escaped = JsonSerializer.Serialize(text);
            var script = FindScriptTemplate
                .Replace("__TEXT__", escaped, StringComparison.Ordinal)
                .Replace("__FORWARD__", forward ? "true" : "false", StringComparison.Ordinal);
            var result = await _control.InvokeScript(script);
            return ParseFindResult(result);
        }
        catch (Exception)
        {
            // Find-in-page is best-effort; some documents may not allow it.
            return FindInPageResult.None;
        }
    }

    public async Task ClearFindAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_control is null)
        {
            return;
        }

        try
        {
            await _control.InvokeScript(ClearFindScript);
        }
        catch (Exception)
        {
            // Best-effort; ignoring failures keeps browsing uninterrupted.
        }
    }

    public async Task SetZoomFactorAsync(double factor, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var clamped = Math.Clamp(factor, 0.25, 5.0);
        if (Math.Abs(clamped - _zoomFactor) < 0.0001)
        {
            return;
        }

        _zoomFactor = clamped;
        OnPropertyChanged(nameof(ZoomFactor));

        if (_control is null)
        {
            return;
        }

        try
        {
            var script = $"document.documentElement.style.zoom = {JsonSerializer.Serialize(clamped.ToString(System.Globalization.CultureInfo.InvariantCulture))};";
            await _control.InvokeScript(script);
        }
        catch (Exception)
        {
            // Zoom is best-effort; failures must not break navigation.
        }
    }

    public void GoBack()
    {
        ThrowIfNotAttached();
        _control!.GoBack();
        SyncCanNavigate();
    }

    public void GoForward()
    {
        ThrowIfNotAttached();
        _control!.GoForward();
        SyncCanNavigate();
    }

    public void Reload()
    {
        ThrowIfNotAttached();
        _control!.Refresh();
    }

    public void Stop()
    {
        if (_control is null)
        {
            return;
        }

        // WebView2 raises NavigationCompleted with IsSuccess == false for a
        // user-requested stop; remember the intent so we don't surface an
        // error state for an intentional cancellation.
        if (IsLoading)
        {
            _stopRequested = true;
        }

        _control.Stop();
        SetIsLoading(false);
    }

    public bool OpenDevTools()
    {
        if (_coreWebView2 == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return WebView2Com.OpenDevToolsWindow(_coreWebView2) >= 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        UnregisterDownloadStarting();
        UnregisterAcceleratorKeyPressed();

        if (_control is not null)
        {
            // Detach from the visual tree so the native adapter is destroyed and the
            // WebView2 controller is closed before the process exits.
            if (_control.Parent is ContentControl host)
            {
                host.Content = null;
            }
            else if (_control.Parent is Panel panel)
            {
                panel.Children.Remove(_control);
            }

            _control.EnvironmentRequested -= OnEnvironmentRequested;
            _control.NavigationStarted -= OnNavigationStarted;
            _control.NavigationCompleted -= OnNavigationCompleted;
            _control.AdapterCreated -= OnAdapterCreated;
            _control.AdapterDestroyed -= OnAdapterDestroyed;
            _control = null;
        }

        _coreWebView2 = IntPtr.Zero;
        _coreWebView2Controller = IntPtr.Zero;
        _disposed = true;
    }

    private void ThrowIfNotAttached()
    {
        if (_control is null)
        {
            throw new InvalidOperationException("The engine is not attached to a WebView control.");
        }
    }

    private void OnEnvironmentRequested(object? sender, WebViewEnvironmentRequestedEventArgs e)
    {
        if (e is WindowsWebView2EnvironmentRequestedEventArgs windows)
        {
            windows.ProfileName = "default";
            windows.UserDataFolder = _userDataFolder;
        }
    }

    private void OnAdapterCreated(object? sender, WebViewAdapterEventArgs e)
    {
        try
        {
            var handle = e.TryGetPlatformHandle();
            if (handle is not IWindowsWebView2PlatformHandle windows)
            {
                return;
            }

            _coreWebView2 = windows.CoreWebView2;
            _coreWebView2Controller = windows.CoreWebView2Controller;
            if (_coreWebView2 != IntPtr.Zero)
            {
                RegisterDownloadStarting();
            }

            if (_coreWebView2Controller != IntPtr.Zero)
            {
                RegisterAcceleratorKeyPressed();
            }
        }
        catch (Exception)
        {
            // A failure to hook native events must never take down the web view.
            _coreWebView2 = IntPtr.Zero;
            _coreWebView2Controller = IntPtr.Zero;
        }
    }

    private void OnAdapterDestroyed(object? sender, WebViewAdapterEventArgs e)
    {
        UnregisterDownloadStarting();
        UnregisterAcceleratorKeyPressed();
        _coreWebView2 = IntPtr.Zero;
        _coreWebView2Controller = IntPtr.Zero;
    }

    private void RegisterDownloadStarting()
    {
        if (_downloadStartingHandlerPtr != IntPtr.Zero)
        {
            return;
        }

        _downloadStartingHandlerPtr = WebView2EventHandlers.CreateDownloadStartingHandler(OnNativeDownloadStarting);
        if (WebView2Com.AddDownloadStarting(_coreWebView2, _downloadStartingHandlerPtr, out _downloadStartingToken) < 0)
        {
            WebView2Com.Release(_downloadStartingHandlerPtr);
            _downloadStartingHandlerPtr = IntPtr.Zero;
        }
    }

    private void UnregisterDownloadStarting()
    {
        if (_downloadStartingHandlerPtr == IntPtr.Zero)
        {
            return;
        }

        WebView2Com.Release(_downloadStartingHandlerPtr);
        _downloadStartingHandlerPtr = IntPtr.Zero;
    }

    /// <summary>
    /// Registers a native handler for WebView2's <c>AcceleratorKeyPressed</c>
    /// event. WebView2 consumes keyboard input itself while the web content has
    /// focus, so the Avalonia window never sees the key; this native hook is
    /// what makes F12 open DevTools regardless of which control has focus.
    /// </summary>
    private void RegisterAcceleratorKeyPressed()
    {
        if (_acceleratorHandlerPtr != IntPtr.Zero)
        {
            return;
        }

        _acceleratorHandlerPtr = WebView2EventHandlers.CreateAcceleratorKeyPressedHandler(OnNativeAcceleratorKeyPressed);
        if (WebView2Com.ControllerAddAcceleratorKeyPressed(_coreWebView2Controller, _acceleratorHandlerPtr, out _acceleratorToken) < 0)
        {
            WebView2Com.Release(_acceleratorHandlerPtr);
            _acceleratorHandlerPtr = IntPtr.Zero;
        }
    }

    private void UnregisterAcceleratorKeyPressed()
    {
        if (_acceleratorHandlerPtr == IntPtr.Zero)
        {
            return;
        }

        try
        {
            WebView2Com.ControllerRemoveAcceleratorKeyPressed(_coreWebView2Controller, _acceleratorToken);
        }
        catch (Exception)
        {
            // The controller may already be torn down; releasing the handler is enough.
        }

        WebView2Com.Release(_acceleratorHandlerPtr);
        _acceleratorHandlerPtr = IntPtr.Zero;
    }

    private const int VkF12 = 0x7B;
    private const int KeyEventKindKeyDown = 0;

    /// <summary>
    /// Invoked by WebView2 (UI thread) when an accelerator key is pressed while
    /// the web content has focus. F12 opens DevTools for the active tab and the
    /// key is marked handled so WebView2 does nothing further with it.
    /// </summary>
    private void OnNativeAcceleratorKeyPressed(IntPtr nativeArgs)
    {
        if (nativeArgs == IntPtr.Zero)
        {
            return;
        }

        try
        {
            if (WebView2Com.AccelArgsGetKeyEventKind(nativeArgs) != KeyEventKindKeyDown
                || WebView2Com.AccelArgsGetVirtualKey(nativeArgs) != VkF12)
            {
                return;
            }

            if (OpenDevTools())
            {
                WebView2Com.AccelArgsPutHandled(nativeArgs, true);
            }
        }
        catch (Exception)
        {
            // Never crash the renderer thread because of a shortcut decision.
        }
    }

    /// <summary>
    /// Invoked by WebView2 (on the UI thread) when a download is about to start.
    /// Builds the CLR event, lets subscribers decide the target path, then
    /// applies the decisions back to the native event args.
    /// </summary>
    private void OnNativeDownloadStarting(IntPtr nativeArgs)
    {
        if (nativeArgs == IntPtr.Zero)
        {
            return;
        }

        try
        {
            var operationPtr = WebView2Com.ArgsGetDownloadOperation(nativeArgs);
            if (operationPtr == IntPtr.Zero)
            {
                return;
            }

            // Ownership of the operation is handed to subscribers (the download
            // service), which keeps it alive and disposes it when the download
            // is removed. If no one subscribes, release it here.
            var operation = new WebView2DownloadOperation(operationPtr);
            var args = new DownloadStartingEventArgs(
                operation.Uri?.AbsoluteUri,
                operation.MimeType,
                WebView2Com.ArgsGetResultFilePath(nativeArgs),
                operation);

            var handler = DownloadStarting;
            if (handler is null)
            {
                operation.Dispose();
                return;
            }

            handler(this, args);

            if (args.Cancel)
            {
                WebView2Com.ArgsPutCancel(nativeArgs, true);
            }

            if (!string.IsNullOrEmpty(args.ResultFilePath))
            {
                WebView2Com.ArgsPutResultFilePath(nativeArgs, args.ResultFilePath);
            }

            WebView2Com.ArgsPutHandled(nativeArgs, args.Handled);
        }
        catch (Exception)
        {
            // Never crash the renderer thread because of a download decision.
        }
    }

    private void OnNavigationStarted(object? sender, WebViewNavigationStartingEventArgs e)
    {
        _navigationStarted = true;
        _navigationStart = DateTime.UtcNow;
        _stopRequested = false;
        SetNavigationState(BrowserNavigationState.Loading);
        SetIsLoading(true);

        if (e.Request is { } request)
        {
            SetCurrentUrl(request.AbsoluteUri);
        }

        SyncCanNavigate();
    }

    private async void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        SetIsLoading(false);

        if (e.Request is { } request)
        {
            SetCurrentUrl(request.AbsoluteUri);
        }

        SyncCanNavigate();

        if (e.IsSuccess)
        {
            SetNavigationState(BrowserNavigationState.Loaded);
            await TryUpdatePageTitleAsync();
            await ReapplyZoomAsync();
            await ExtractFaviconAsync();
            IsSecureConnection = _currentUrl?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ?? false;
            OnPropertyChanged(nameof(IsSecureConnection));
            NavigationCompleted?.Invoke(this, new BrowserNavigationCompletedEventArgs(e.Request, true, _pageTitle));
        }
        else if (_stopRequested)
        {
            _stopRequested = false;
            SetNavigationState(BrowserNavigationState.Idle);
            NavigationCompleted?.Invoke(this, new BrowserNavigationCompletedEventArgs(e.Request, false));
        }
        else
        {
            SetNavigationState(BrowserNavigationState.Error);
            NavigationCompleted?.Invoke(this, new BrowserNavigationCompletedEventArgs(e.Request, false));
        }

        // Best-effort navigation timing measurement.
        if (_navigationStarted)
        {
            var elapsed = DateTime.UtcNow - _navigationStart;
            _navigationStarted = false;
            // TODO: log or expose elapsed time for performance monitoring.
            // For now we just avoid the measurement going unused.
        }
    }

    private async Task TryUpdatePageTitleAsync()
    {
        try
        {
            var result = await _control!.InvokeScript("document.title");
            var title = DecodeScriptResult(result)?.Trim() ?? string.Empty;
            SetPageTitle(title);
        }
        catch (Exception)
        {
            // Reading the document title is best-effort; some documents may not allow it.
        }
    }

    private async Task ReapplyZoomAsync()
    {
        if (_zoomFactor == 1.0)
        {
            return;
        }

        try
        {
            var script = $"document.documentElement.style.zoom = {JsonSerializer.Serialize(_zoomFactor.ToString(System.Globalization.CultureInfo.InvariantCulture))};";
            await _control!.InvokeScript(script);
        }
        catch (Exception)
        {
            // Best-effort; zoom reapplication must never break navigation.
        }
    }

    private async Task ExtractFaviconAsync()
    {
        if (_control is null)
        {
            return;
        }

        try
        {
            var script = FaviconScript;
            var result = await _control!.InvokeScript(script);
            if (result is string s)
            {
                // InvokeScript returns the JSON-serialized completion value;
                // a string result comes back quoted, so strip the surrounding quotes.
                var trimmed = s.Trim('\'', '"');
                if (!string.IsNullOrEmpty(trimmed))
                {
                    var uri = new Uri(trimmed, UriKind.RelativeOrAbsolute);
                    OnFaviconChanged(uri);
                }
            }
        }
        catch (Exception)
        {
            // Favicon extraction is best-effort; failures must not break navigation.
        }
    }

    private void OnFaviconChanged(Uri? uri)
    {
        if (FaviconUrl == uri)
        {
            return;
        }

        FaviconUrl = uri;
        OnPropertyChanged(nameof(FaviconUrl));
    }

    private async Task<TimeSpan?> MeasureNavigationTimeAsync()
    {
        // Navigation timing is available via the NavigationCompleted event args,
        // but we also track how long a navigation took from start to completion.
        // This is a best-effort measurement.
        return null;
    }

    private static string? DecodeScriptResult(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return result;
        }

        try
        {
            return JsonSerializer.Deserialize<string>(result);
        }
        catch (JsonException)
        {
            return result;
        }
    }

    private static FindInPageResult ParseFindResult(string? result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return FindInPageResult.None;
        }

        try
        {
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;
            var count = root.GetProperty("count").GetInt32();
            var index = root.GetProperty("index").GetInt32();
            return new FindInPageResult(count, index);
        }
        catch (JsonException)
        {
            return FindInPageResult.None;
        }
    }

    private void SetCurrentUrl(string value)
    {
        if (_currentUrl == value)
        {
            return;
        }

        _currentUrl = value;
        OnPropertyChanged(nameof(CurrentUrl));
    }

    private void SetPageTitle(string value)
    {
        if (_pageTitle == value)
        {
            return;
        }

        _pageTitle = value;
        OnPropertyChanged(nameof(PageTitle));
    }

    private void SetIsLoading(bool value)
    {
        if (_isLoading == value)
        {
            return;
        }

        _isLoading = value;
        OnPropertyChanged(nameof(IsLoading));
    }

    private void SetNavigationState(BrowserNavigationState value)
    {
        if (_navigationState == value)
        {
            return;
        }

        _navigationState = value;
        OnPropertyChanged(nameof(NavigationState));
    }

    private void SyncCanNavigate()
    {
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
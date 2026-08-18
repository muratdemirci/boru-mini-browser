using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniBrowser.Core.Browser;
using MiniBrowser.Core.Models;
using MiniBrowser.Core.Services;
using MiniBrowser.Infrastructure.Persistence;
using MiniBrowser.Services.Bookmarks;
using MiniBrowser.Services.Downloads;
using MiniBrowser.Services.History;
using MiniBrowser.Services.Settings;

namespace MiniBrowser.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IBrowserEngineFactory _engineFactory;
    private readonly IHistoryService _historyService;
    private readonly IBookmarkService _bookmarkService;
    private readonly IDownloadService _downloadService;
    private readonly ISettingsService _settingsService;
    private TabViewModel? _activeTab;
    private bool _disposed;

    [ObservableProperty]
    private string windowTitle = "MiniBrowser";

    [ObservableProperty]
    private string addressText = string.Empty;

    [ObservableProperty]
    private bool isCurrentBookmarked;

    [ObservableProperty]
    private bool canBookmark;

    [ObservableProperty]
    private bool isFindBarVisible;

    [ObservableProperty]
    private string findText = string.Empty;

    [ObservableProperty]
    private string findStatus = string.Empty;

    [ObservableProperty]
    private bool isRestoringSession;

    public MainWindowViewModel(
        IBrowserEngineFactory engineFactory,
        IHistoryService historyService,
        IBookmarkService bookmarkService,
        IDownloadService downloadService,
        ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(engineFactory);
        ArgumentNullException.ThrowIfNull(historyService);
        ArgumentNullException.ThrowIfNull(bookmarkService);
        ArgumentNullException.ThrowIfNull(downloadService);
        ArgumentNullException.ThrowIfNull(settingsService);

        _engineFactory = engineFactory;
        _historyService = historyService;
        _bookmarkService = bookmarkService;
        _downloadService = downloadService;
        _settingsService = settingsService;

        History = new HistoryViewModel(historyService, url => _ = OpenUrlInActiveTab(url));
        Bookmarks = new BookmarksViewModel(bookmarkService, url => _ = OpenUrlInActiveTab(url));
        Downloads = new DownloadsViewModel(downloadService, url => _ = OpenUrlInActiveTab(url));
        Bookmarks.Changed += OnBookmarksChanged;
    }

    public ObservableCollection<TabViewModel> Tabs { get; } = [];

    public HistoryViewModel History { get; }

    public BookmarksViewModel Bookmarks { get; }

    public DownloadsViewModel Downloads { get; }

    public TabViewModel? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (ReferenceEquals(_activeTab, value))
            {
                return;
            }

if (_activeTab is not null)
                {
                    _activeTab.IsActive = false;
                    _activeTab.PropertyChanged -= OnActiveTabPropertyChanged;
                }

                _activeTab = value;
                OnPropertyChanged();

                if (_activeTab is not null)
                {
                    _activeTab.IsActive = true;
                    _activeTab.PropertyChanged += OnActiveTabPropertyChanged;
                    AddressText = _activeTab.Url;
                    WindowTitle = _activeTab.WindowTitle;

                    // Store the last active URL for session restore
                    _settingsService.Current.LastActiveUrl = _activeTab.Url;
                    _settingsService.Save();
                }
                else
                {
                    AddressText = string.Empty;
                    WindowTitle = "MiniBrowser";
                    _settingsService.Current.LastActiveUrl = null;
                    _settingsService.Save();
                }

                UpdateBookmarkAffordance();
        }
    }

    [RelayCommand]
    private void NewTab()
    {
        var tab = new TabViewModel(_engineFactory.Create(), HomepageUri);
        tab.Navigated += OnTabNavigated;
        tab.Engine.DownloadStarting += OnEngineDownloadStarting;
        Tabs.Add(tab);
        ActiveTab = tab;
    }
    [RelayCommand]
    private void CloseTab(TabViewModel? tab)
    {
        var target = tab ?? ActiveTab;
        if (target is null)
        {
            return;
        }

        var index = Tabs.IndexOf(target);
        if (index < 0)
        {
            return;
        }

        var wasActive = ReferenceEquals(target, ActiveTab);

        Tabs.Remove(target);

        if (Tabs.Count == 0)
        {
            // The browser always keeps at least one tab open.
            NewTab();
        }
        else if (wasActive)
        {
            ActiveTab = Tabs[Math.Min(index, Tabs.Count - 1)];
        }

        target.Navigated -= OnTabNavigated;
        target.Engine.DownloadStarting -= OnEngineDownloadStarting;
        target.Dispose();
    }

    [RelayCommand]
    private void SelectTab(TabViewModel? tab)
    {
        if (tab is not null)
        {
            ActiveTab = tab;
        }
    }

    [RelayCommand]
    private void NextTab()
    {
        if (ActiveTab is null || Tabs.Count < 2)
        {
            return;
        }

        var index = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(index + 1) % Tabs.Count];
    }

    [RelayCommand]
    private void PreviousTab()
    {
        if (ActiveTab is null || Tabs.Count < 2)
        {
            return;
        }

        var index = Tabs.IndexOf(ActiveTab);
        ActiveTab = Tabs[(index - 1 + Tabs.Count) % Tabs.Count];
    }

    [RelayCommand]
    private void SelectTabByIndex(int? index)
    {
        if (index is null)
        {
            return;
        }

        if (index.Value >= 0 && index.Value < Tabs.Count)
        {
            ActiveTab = Tabs[index.Value];
        }
    }

    [RelayCommand]
    private async Task NavigateAsync()
    {
        var input = AddressText;
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        Uri target;
        try
        {
            target = UrlNormalizer.Normalize(input);
        }
        catch (UriFormatException)
        {
            // Not a web address: fall back to the configured search engine.
            target = SearchEngineUrlBuilder.Build(_settingsService.Current.SearchEngine, input);
        }

        AddressText = target.AbsoluteUri;

        if (ActiveTab is not null)
        {
            await ActiveTab.NavigateAsync(target);
        }
    }

    [RelayCommand]
    private void GoBack() => ActiveTab?.GoBack();

    [RelayCommand]
    private void GoForward() => ActiveTab?.GoForward();

    [RelayCommand]
    private void Reload() => ActiveTab?.Reload();

    [RelayCommand]
    private void Stop() => ActiveTab?.Stop();

    [RelayCommand]
    private void OpenDevTools() => ActiveTab?.Engine.OpenDevTools();

    [RelayCommand]
    private void OpenFind()
    {
        IsFindBarVisible = true;
    }

    [RelayCommand]
    private async Task CloseFindAsync()
    {
        IsFindBarVisible = false;
        FindText = string.Empty;
        FindStatus = string.Empty;

        if (ActiveTab is not null)
        {
            await ActiveTab.ClearFindAsync();
        }
    }

    [RelayCommand]
    private async Task FindNextAsync()
    {
        if (string.IsNullOrEmpty(FindText) || ActiveTab is null)
        {
            return;
        }

        var result = await ActiveTab.FindAsync(FindText, forward: true);
        UpdateFindStatus(result);
    }

    [RelayCommand]
    private async Task FindPreviousAsync()
    {
        if (string.IsNullOrEmpty(FindText) || ActiveTab is null)
        {
            return;
        }

        var result = await ActiveTab.FindAsync(FindText, forward: false);
        UpdateFindStatus(result);
    }

    [RelayCommand]
    private async Task ZoomInAsync()
    {
        if (ActiveTab is null)
        {
            return;
        }

        await ActiveTab.SetZoomFactorAsync(ActiveTab.ZoomFactor + 0.25);
    }

    [RelayCommand]
    private async Task ZoomOutAsync()
    {
        if (ActiveTab is null)
        {
            return;
        }

        await ActiveTab.SetZoomFactorAsync(ActiveTab.ZoomFactor - 0.25);
    }

    [RelayCommand]
    private async Task ResetZoomAsync()
    {
        if (ActiveTab is null)
        {
            return;
        }

        await ActiveTab.SetZoomFactorAsync(1.0);
    }

    private void UpdateFindStatus(FindInPageResult result)
    {
        if (result.MatchCount == 0)
        {
            FindStatus = "No matches";
            return;
        }

        FindStatus = $"{result.CurrentMatchIndex + 1} of {result.MatchCount}";
    }

    [RelayCommand]
    private async Task ToggleBookmarkAsync()
    {
        var tab = ActiveTab;
        if (tab is null || string.IsNullOrEmpty(tab.Url))
        {
            return;
        }

        if (IsCurrentBookmarked)
        {
            await _bookmarkService.RemoveAsync(tab.Url);
        }
        else
        {
            await _bookmarkService.AddAsync(tab.Url, tab.PageTitle);
        }

        // Re-query so the state reflects what actually persisted (the service
        // silently no-ops when the database is unavailable).
        IsCurrentBookmarked = await _bookmarkService.IsBookmarkedAsync(tab.Url);
    }

    /// <summary>
    /// Opens a URL (e.g. from History or Bookmarks) in the active tab.
    /// </summary>
    public async Task OpenUrlInActiveTab(string url)
    {
        if (ActiveTab is null || string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        AddressText = uri.AbsoluteUri;
        await ActiveTab.NavigateAsync(uri);
    }

    /// <summary>
    /// Called once the main window is shown. Creates the initial tab according
    /// to the startup behavior setting.
    /// </summary>
    public Task InitializeAsync()
    {
        if (_settingsService.Current.StartupBehavior == StartupBehavior.NewTab)
        {
            // Restore the last actively viewed page, if any.
            if (!string.IsNullOrEmpty(_settingsService.Current.LastActiveUrl))
            {
                if (Uri.TryCreate(_settingsService.Current.LastActiveUrl, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    var tab = new TabViewModel(_engineFactory.Create(), uri);
                    tab.Navigated += OnTabNavigated;
                    tab.Engine.DownloadStarting += OnEngineDownloadStarting;
                    Tabs.Add(tab);
                    ActiveTab = tab;
                }
                else
                {
                    // Stored URL is invalid; start fresh.
                    var tab = new TabViewModel(_engineFactory.Create(), HomepageUri);
                    tab.Navigated += OnTabNavigated;
                    tab.Engine.DownloadStarting += OnEngineDownloadStarting;
                    Tabs.Add(tab);
                    ActiveTab = tab;
                }
            }
            else
            {
                // A fresh, empty tab instead of the homepage.
                var content = NewTabPage.Build(
                    _settingsService.Current.SearchEngine,
                    _settingsService.Current.Homepage);
                var tab = new TabViewModel(_engineFactory.Create(), newTabContent: content);
                tab.Navigated += OnTabNavigated;
                tab.Engine.DownloadStarting += OnEngineDownloadStarting;
                Tabs.Add(tab);
                ActiveTab = tab;
            }
        }
        else
        {
            NewTab();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves the current homepage from settings, falling back to a safe
    /// default when the stored value is not a valid web address.
    /// </summary>
    private Uri HomepageUri
    {
        get
        {
            var homepage = _settingsService.Current.Homepage;
            if (Uri.TryCreate(homepage, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return uri;
            }

            return new Uri("https://www.muratdemirci.org/");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_activeTab is not null)
        {
            _activeTab.PropertyChanged -= OnActiveTabPropertyChanged;
        }

        foreach (var tab in Tabs)
        {
            tab.Navigated -= OnTabNavigated;
            tab.Engine.DownloadStarting -= OnEngineDownloadStarting;
            tab.Dispose();
        }

        Bookmarks.Changed -= OnBookmarksChanged;
        Tabs.Clear();
        _activeTab = null;
    }

    private void OnBookmarksChanged()
    {
        UpdateBookmarkAffordance();
    }

    private void OnTabNavigated(object? sender, BrowserNavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess || e.Uri is null)
        {
            return;
        }

        _ = RecordHistoryAsync(e.Uri.AbsoluteUri, e.Title);
    }

    private void OnEngineDownloadStarting(object? sender, DownloadStartingEventArgs e)
    {
        _downloadService.HandleDownloadStarting(e);
    }

    private async Task RecordHistoryAsync(string url, string? title)
    {
        try
        {
            await _historyService.AddAsync(url, title);
        }
        catch (Exception)
        {
            // History is best-effort and must never interfere with browsing.
        }
    }

    private void OnActiveTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TabViewModel tab || !ReferenceEquals(tab, ActiveTab))
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(TabViewModel.Url):
                AddressText = tab.Url;
                UpdateBookmarkAffordance();
                break;

            case nameof(TabViewModel.WindowTitle):
                WindowTitle = tab.WindowTitle;
                break;
        }
    }

    private void UpdateBookmarkAffordance()
    {
        var url = ActiveTab?.Url;
        CanBookmark = !string.IsNullOrEmpty(url);

        if (string.IsNullOrEmpty(url))
        {
            IsCurrentBookmarked = false;
            return;
        }

        _ = RefreshBookmarkStateAsync(url);
    }

    private async Task RefreshBookmarkStateAsync(string url)
    {
        try
        {
            var bookmarked = await _bookmarkService.IsBookmarkedAsync(url);
            if (ActiveTab is not null && string.Equals(ActiveTab.Url, url, StringComparison.Ordinal))
            {
                IsCurrentBookmarked = bookmarked;
            }
        }
        catch (Exception)
        {
            // Bookmark state is best-effort; keep the current value.
        }
    }
}
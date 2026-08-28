using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniBrowser.Core.Models;
using MiniBrowser.Services.Settings;

namespace MiniBrowser.ViewModels;

/// <summary>
/// View model for the Settings window. Edits a working copy of the current
/// settings; Save validates and persists, Cancel discards the edits.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private BrowserSettings? _original;

    public SettingsViewModel(ISettingsService settingsService, IThemeService themeService)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(themeService);

        _settingsService = settingsService;
        _themeService = themeService;
        LoadFrom(_settingsService.Current);
    }

    public ObservableCollection<ThemePreference> Themes { get; } =
        [ThemePreference.System, ThemePreference.Light, ThemePreference.Dark];

    public ObservableCollection<SearchEnginePreference> SearchEngines { get; } =
        [SearchEnginePreference.Google, SearchEnginePreference.Bing, SearchEnginePreference.DuckDuckGo];

    public ObservableCollection<StartupBehavior> StartupBehaviors { get; } =
        [StartupBehavior.Homepage, StartupBehavior.NewTab];

    [ObservableProperty]
    private ThemePreference theme;

    [ObservableProperty]
    private string homepage = string.Empty;

    [ObservableProperty]
    private SearchEnginePreference searchEngine;

    [ObservableProperty]
    private string downloadDirectory = string.Empty;

    [ObservableProperty]
    private StartupBehavior startupBehavior;

    [ObservableProperty]
    private string? statusMessage;

    public bool IsAvailable => _settingsService.IsAvailable;

    /// <summary>Raised by the view after a successful save so the window can close.</summary>
    public event EventHandler? Saved;

    /// <summary>Raised by the view when the user cancels.</summary>
    public event EventHandler? Cancelled;

    private void LoadFrom(BrowserSettings settings)
    {
        _original = settings;
        Theme = settings.Theme;
        Homepage = settings.Homepage;
        SearchEngine = settings.SearchEngine;
        DownloadDirectory = settings.DownloadDirectory ?? string.Empty;
        StartupBehavior = settings.StartupBehavior;
        StatusMessage = _settingsService.IsAvailable ? null : $"Settings unavailable: {_settingsService.LastError}";
    }

    [RelayCommand]
    private void Save()
    {
        if (!Validate(out var error))
        {
            StatusMessage = error;
            return;
        }

        var target = _original ?? new BrowserSettings();
        target.Theme = Theme;
        target.Homepage = Uri.TryCreate(Homepage.Trim(), UriKind.Absolute, out var homepageUri)
            ? homepageUri.AbsoluteUri
            : Homepage.Trim();
        target.SearchEngine = SearchEngine;
        target.DownloadDirectory = string.IsNullOrWhiteSpace(DownloadDirectory)
            ? null
            : DownloadDirectory.Trim();
        target.StartupBehavior = StartupBehavior;

        _settingsService.Save();
        _themeService.Apply(Theme);
        StatusMessage = null;
        Saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ResetDefaults()
    {
        _settingsService.Reset();
        LoadFrom(_settingsService.Current);
        _themeService.Apply(Theme);
        StatusMessage = "Settings reset to defaults.";
    }

    private bool Validate(out string error)
    {
        var homepage = Homepage?.Trim() ?? string.Empty;
        if (homepage.Length == 0)
        {
            error = "Homepage cannot be empty.";
            return false;
        }

        if (!Uri.TryCreate(homepage, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            error = "Homepage must be a valid http:// or https:// address.";
            return false;
        }

        var directory = DownloadDirectory?.Trim();
        if (!string.IsNullOrEmpty(directory))
        {
            if (!Path.IsPathFullyQualified(directory) || directory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                error = "Download directory must be a valid absolute path.";
                return false;
            }

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch (Exception)
            {
                error = "Download directory is not writable.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }
}

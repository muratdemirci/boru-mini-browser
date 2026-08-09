namespace MiniBrowser.Core.Models;

/// <summary>UI theme preference: follow the system or force light/dark.</summary>
public enum ThemePreference
{
    System = 0,
    Light = 1,
    Dark = 2,
}

/// <summary>Which page the browser shows on startup.</summary>
public enum StartupBehavior
{
    Homepage = 0,
    NewTab = 1,
}

/// <summary>Supported web search engines for address-bar search fallback.</summary>
public enum SearchEnginePreference
{
    Google = 0,
    Bing = 1,
    DuckDuckGo = 2,
}

/// <summary>
/// Strongly typed browser preferences. This is the persistence contract for
/// <c>ISettingsService</c>; it is validated on load and defaults are used when
/// the persisted file is missing or invalid.
/// </summary>
public sealed class BrowserSettings
{
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>Homepage shown on startup and when a new tab is created.</summary>
    public string Homepage { get; set; } = "https://www.muratdemirci.org/";

    public SearchEnginePreference SearchEngine { get; set; } = SearchEnginePreference.Google;

    /// <summary>Optional custom download directory; null means the default.</summary>
    public string? DownloadDirectory { get; set; }

    public StartupBehavior StartupBehavior { get; set; } = StartupBehavior.Homepage;

    /// <summary>URL of the last actively viewed page; used on startup to restore</summary>
    public string? LastActiveUrl { get; set; }
}

using MiniBrowser.Core.Models;

namespace MiniBrowser.Services.Settings;

/// <summary>
/// Loads and persists browser preferences. The store is resilient: when the
/// settings file is missing or corrupted the browser still starts with defaults.
/// </summary>
public interface ISettingsService
{
    /// <summary>The current in-memory settings (never null).</summary>
    BrowserSettings Current { get; }

    /// <summary>True once settings were successfully loaded and are writable.</summary>
    bool IsAvailable { get; }

    /// <summary>Description of the last load/save failure, when any.</summary>
    string? LastError { get; }

    /// <summary>True when the persisted settings file is corrupted or invalid.</summary>
    bool NeedsReset { get; }

    /// <summary>Raised after settings are loaded, saved or reset.</summary>
    event EventHandler? SettingsChanged;

    /// <summary>Loads settings from the persistent store, falling back to defaults.</summary>
    void Load();

    /// <summary>Persists the current settings to the store.</summary>
    void Save();

    /// <summary>Restores all settings to their default values.</summary>
    void Reset();
}
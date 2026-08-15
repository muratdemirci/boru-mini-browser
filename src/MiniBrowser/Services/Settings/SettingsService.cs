using System.Text.Json;
using System.Text.Json.Serialization;
using MiniBrowser.Core.Models;

namespace MiniBrowser.Services.Settings;

/// <summary>
/// JSON-file-backed settings store living in the per-user application data
/// directory (alongside the SQLite database). Writes go through a temporary file
/// that is moved into place so a crash mid-write cannot corrupt the store.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    /// <summary>Default location of the settings file on disk.</summary>
    public static string DefaultSettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MiniBrowser",
        "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private BrowserSettings _current;
    private bool _available = true;
    private bool _needsReset = false;
    private string? _lastError;

    public SettingsService()
        : this(DefaultSettingsPath)
    {
    }

    /// <summary>Creates a service backed by a specific file (used by tests).</summary>
    public SettingsService(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        SettingsPath = settingsPath;
        _current = CreateDefaults();
    }

    /// <summary>Path of the settings file this service reads and writes.</summary>
    public string SettingsPath { get; }

    public BrowserSettings Current => _current;

    public bool IsAvailable => _available && !_needsReset;

    public bool NeedsReset => _needsReset;

    public string? LastError => _lastError;

    public event EventHandler? SettingsChanged;

    public void Load()
    {
        if (!File.Exists(SettingsPath))
        {
            // Nothing persisted yet: defaults are already in place.
            _available = true;
            _lastError = null;
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var loaded = JsonSerializer.Deserialize<BrowserSettings>(json, SerializerOptions);
            if (loaded is null)
            {
                throw new InvalidDataException("Settings file contains no settings.");
            }

            _current = ValidateAndCoerce(loaded);
            _available = true;
            _needsReset = false;
            _lastError = null;
        }
        catch (Exception ex)
        {
            _available = false;
            _needsReset = true;
            _lastError = ex.Message;
            _current = CreateDefaults();
        }

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(ValidateAndCoerce(_current), SerializerOptions);
            var temp = SettingsPath + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(SettingsPath))
            {
                File.Replace(temp, SettingsPath, null);
            }
            else
            {
                File.Move(temp, SettingsPath);
            }

            _available = true;
            _lastError = null;
        }
        catch (Exception ex)
        {
            _available = false;
            _lastError = ex.Message;
        }

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        _current = CreateDefaults();
        _available = true;
        _needsReset = false;
        _lastError = null;
        Save();
    }

    private static BrowserSettings CreateDefaults() => new();

    /// <summary>
    /// Normalises persisted values: coerce invalid enums, require a valid
    /// homepage, and drop a download directory that is not a plausible path.
    /// </summary>
    private static BrowserSettings ValidateAndCoerce(BrowserSettings settings)
    {
        settings.Theme = Enum.IsDefined(settings.Theme) ? settings.Theme : ThemePreference.System;
        settings.SearchEngine = Enum.IsDefined(settings.SearchEngine) ? settings.SearchEngine : SearchEnginePreference.Google;
        settings.StartupBehavior = Enum.IsDefined(settings.StartupBehavior) ? settings.StartupBehavior : StartupBehavior.Homepage;

        var homepage = settings.Homepage?.Trim() ?? string.Empty;
        if (!Uri.TryCreate(homepage, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            settings.Homepage = CreateDefaults().Homepage;
        }
        else
        {
            settings.Homepage = uri.AbsoluteUri;
        }

        var directory = settings.DownloadDirectory?.Trim();
        if (string.IsNullOrEmpty(directory))
        {
            settings.DownloadDirectory = null;
        }
        else if (!Path.IsPathFullyQualified(directory) || directory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            settings.DownloadDirectory = null;
        }

        return settings;
    }
}

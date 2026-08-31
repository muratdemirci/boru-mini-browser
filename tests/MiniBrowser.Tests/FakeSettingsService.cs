using MiniBrowser.Core.Models;
using MiniBrowser.Services.Settings;

namespace MiniBrowser.Tests;

/// <summary>
/// In-memory <see cref="ISettingsService"/> for view-model tests. Exposes a
/// mutable <see cref="BrowserSettings"/> instance without touching disk.
/// </summary>
internal sealed class FakeSettingsService : ISettingsService
{
    public FakeSettingsService()
    {
        Current = new BrowserSettings();
        NeedsReset = false;
    }

    public BrowserSettings Current { get; }

    public bool IsAvailable { get; private set; } = true;

    public bool NeedsReset { get; private set; }

    public string? LastError { get; private set; }

    public event EventHandler? SettingsChanged;

    public int LoadCalls { get; private set; }

    public int SaveCalls { get; private set; }

    public int ResetCalls { get; private set; }

    public void Load()
    {
        LoadCalls++;
        IsAvailable = true;
        NeedsReset = false;
        LastError = null;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Save()
    {
        SaveCalls++;
        IsAvailable = true;
        NeedsReset = false;
        LastError = null;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        ResetCalls++;
        Current.Theme = default;
        Current.Homepage = "https://www.google.com";
        Current.SearchEngine = default;
        Current.DownloadDirectory = null;
        Current.StartupBehavior = default;
        NeedsReset = false;
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}

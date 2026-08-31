using MiniBrowser.Core.Models;
using MiniBrowser.Services.Settings;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Tests;

/// <summary>
/// Phase 7: settings window view model behaviour — save validation, reset, and
/// the homepage/startup integration in the main window view model.
/// </summary>
public sealed class SettingsViewModelTests
{
    private readonly FakeSettingsService _settings = new();
    private readonly RecordingThemeService _theme = new();
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _viewModel = new SettingsViewModel(_settings, _theme);
    }

    private sealed class RecordingThemeService : IThemeService
    {
        public List<ThemePreference> Applied { get; } = [];

        public void Apply(ThemePreference preference) => Applied.Add(preference);
    }

    [Fact]
    public void Constructor_CopiesCurrentSettings()
    {
        _settings.Current.Theme = ThemePreference.Dark;
        _settings.Current.Homepage = "https://example.com";
        _settings.Current.SearchEngine = SearchEnginePreference.Bing;

        var vm = new SettingsViewModel(_settings, _theme);

        Assert.Equal(ThemePreference.Dark, vm.Theme);
        Assert.Equal("https://example.com", vm.Homepage);
        Assert.Equal(SearchEnginePreference.Bing, vm.SearchEngine);
    }

    [Fact]
    public void Save_PersistsEditsAndAppliesTheme()
    {
        var saved = false;
        _viewModel.Saved += (_, _) => saved = true;

        _viewModel.Theme = ThemePreference.Dark;
        _viewModel.Homepage = "https://duckduckgo.com";
        _viewModel.SearchEngine = SearchEnginePreference.DuckDuckGo;
        _viewModel.DownloadDirectory = Path.GetTempPath();
        _viewModel.StartupBehavior = StartupBehavior.NewTab;

        _viewModel.SaveCommand.Execute(null);

        Assert.True(saved);
        Assert.Equal(1, _settings.SaveCalls);
        Assert.Equal(ThemePreference.Dark, _settings.Current.Theme);
        Assert.Equal("https://duckduckgo.com/", _settings.Current.Homepage);
        Assert.Equal(SearchEnginePreference.DuckDuckGo, _settings.Current.SearchEngine);
        Assert.Equal(StartupBehavior.NewTab, _settings.Current.StartupBehavior);
        Assert.Equal([ThemePreference.Dark], _theme.Applied);
    }

    [Fact]
    public void Save_InvalidHomepage_ShowsErrorAndDoesNotSave()
    {
        var saved = false;
        _viewModel.Saved += (_, _) => saved = true;

        _viewModel.Homepage = "not a url";
        _viewModel.SaveCommand.Execute(null);

        Assert.False(saved);
        Assert.Equal(0, _settings.SaveCalls);
        Assert.False(string.IsNullOrEmpty(_viewModel.StatusMessage));
    }

    [Fact]
    public void Save_EmptyHomepage_ShowsError()
    {
        _viewModel.Homepage = "   ";
        _viewModel.SaveCommand.Execute(null);

        Assert.Equal(0, _settings.SaveCalls);
        Assert.False(string.IsNullOrEmpty(_viewModel.StatusMessage));
    }

    [Fact]
    public void Save_InvalidDownloadDirectory_ShowsError()
    {
        _viewModel.Homepage = "https://example.com";
        _viewModel.DownloadDirectory = "not\\a\\full:path";
        _viewModel.SaveCommand.Execute(null);

        Assert.Equal(0, _settings.SaveCalls);
        Assert.False(string.IsNullOrEmpty(_viewModel.StatusMessage));
    }

    [Fact]
    public void ResetDefaults_ResetsServiceAndAppliesTheme()
    {
        _settings.Current.Theme = ThemePreference.Dark;
        _settings.Save();

        _viewModel.ResetDefaultsCommand.Execute(null);

        Assert.Equal(ThemePreference.System, _settings.Current.Theme);
        Assert.Equal("https://www.google.com", _viewModel.Homepage);
        Assert.Equal([ThemePreference.System], _theme.Applied);
    }

    [Fact]
    public void Cancel_RaisesCancelled()
    {
        var cancelled = false;
        _viewModel.Cancelled += (_, _) => cancelled = true;

        _viewModel.CancelCommand.Execute(null);

        Assert.True(cancelled);
        Assert.Equal(0, _settings.SaveCalls);
    }
}

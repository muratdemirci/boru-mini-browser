using MiniBrowser.Core.Models;
using MiniBrowser.Core.Services;
using MiniBrowser.Services.Settings;

namespace MiniBrowser.Tests;

/// <summary>
/// Phase 7: settings persistence and validation plus the address-bar search
/// fallback URL builder.
/// </summary>
public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _path;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MiniBrowserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _path = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (Exception)
        {
        }
    }

    private SettingsService CreateService() => new(_path);

    [Fact]
    public void Load_MissingFile_KeepsDefaults()
    {
        var service = CreateService();
        service.Load();

        Assert.True(service.IsAvailable);
        Assert.Equal(ThemePreference.System, service.Current.Theme);
        Assert.Equal("https://www.muratdemirci.org/", service.Current.Homepage);
        Assert.Null(service.Current.DownloadDirectory);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var service = CreateService();
        service.Current.Theme = ThemePreference.Dark;
        service.Current.Homepage = "https://example.com";
        service.Current.SearchEngine = SearchEnginePreference.DuckDuckGo;
        service.Current.StartupBehavior = StartupBehavior.NewTab;
        service.Current.DownloadDirectory = _tempDir;
        service.Save();

        var reloaded = CreateService();
        reloaded.Load();

        Assert.True(reloaded.IsAvailable);
        Assert.Equal(ThemePreference.Dark, reloaded.Current.Theme);
        Assert.Equal("https://example.com/", reloaded.Current.Homepage);
        Assert.Equal(SearchEnginePreference.DuckDuckGo, reloaded.Current.SearchEngine);
        Assert.Equal(StartupBehavior.NewTab, reloaded.Current.StartupBehavior);
        Assert.Equal(_tempDir, reloaded.Current.DownloadDirectory);
    }

    [Fact]
    public void Load_CorruptFile_FallsBackToDefaults()
    {
        File.WriteAllText(_path, "{ not valid json !!!");

        var service = CreateService();
        service.Load();

        Assert.False(service.IsAvailable);
        Assert.NotNull(service.LastError);
        Assert.Equal(ThemePreference.System, service.Current.Theme);
        Assert.Equal("https://www.muratdemirci.org/", service.Current.Homepage);
    }

    [Fact]
    public void Load_InvalidHomepage_IsReplacedWithDefault()
    {
        File.WriteAllText(_path, """{"Homepage":"not a url","Theme":"Dark"}""");

        var service = CreateService();
        service.Load();

        Assert.Equal("https://www.muratdemirci.org/", service.Current.Homepage);
        Assert.Equal(ThemePreference.Dark, service.Current.Theme);
    }

    [Fact]
    public void Load_InvalidEnumValue_IsCoercedToDefault()
    {
        File.WriteAllText(_path, """{"Theme":99,"SearchEngine":42}""");

        var service = CreateService();
        service.Load();

        Assert.Equal(ThemePreference.System, service.Current.Theme);
        Assert.Equal(SearchEnginePreference.Google, service.Current.SearchEngine);
    }

    [Fact]
    public void Load_RelativeDownloadDirectory_IsDropped()
    {
        File.WriteAllText(_path, """{"DownloadDirectory":"..\\escape"}""");

        var service = CreateService();
        service.Load();

        Assert.Null(service.Current.DownloadDirectory);
    }

    [Fact]
    public void Reset_RestoresDefaultsAndPersists()
    {
        var service = CreateService();
        service.Current.Theme = ThemePreference.Dark;
        service.Save();

        service.Reset();

        Assert.Equal(ThemePreference.System, service.Current.Theme);
        Assert.Equal("https://www.muratdemirci.org/", service.Current.Homepage);

        var reloaded = CreateService();
        reloaded.Load();
        Assert.Equal(ThemePreference.System, reloaded.Current.Theme);
        Assert.Equal("https://www.muratdemirci.org/", reloaded.Current.Homepage);
    }

    [Fact]
    public void Save_RaisesSettingsChanged()
    {
        var service = CreateService();
        var raised = false;
        service.SettingsChanged += (_, _) => raised = true;

        service.Save();

        Assert.True(raised);
    }

    [Theory]
    [InlineData(SearchEnginePreference.Google, "mini browser", "https://www.muratdemirci.org/search?q=mini%20browser")]
    [InlineData(SearchEnginePreference.Bing, "mini browser", "https://www.bing.com/search?q=mini%20browser")]
    [InlineData(SearchEnginePreference.DuckDuckGo, "mini browser", "https://duckduckgo.com/?q=mini%20browser")]
    [InlineData(SearchEnginePreference.Google, "a/b?c&d", "https://www.muratdemirci.org/search?q=a%2Fb%3Fc%26d")]
    public void SearchEngineUrlBuilder_EncodesQuery(SearchEnginePreference engine, string query, string expected)
    {
        var uri = SearchEngineUrlBuilder.Build(engine, query);

        Assert.Equal(expected, uri.AbsoluteUri);
    }
}

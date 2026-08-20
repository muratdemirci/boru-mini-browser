using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniBrowser.Core.Browser;
using MiniBrowser.Infrastructure.Persistence;
using MiniBrowser.Infrastructure.WebView;
using MiniBrowser.Services.Bookmarks;
using MiniBrowser.Services.Downloads;
using MiniBrowser.Services.History;
using MiniBrowser.Services.Settings;
using MiniBrowser.ViewModels;
using MiniBrowser.Views;

namespace MiniBrowser;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = default!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The database is optional: if it cannot be created the browser still
            // starts and History/Bookmarks simply report unavailability.
            DatabaseInitializer.EnsureCreated();

            Services = ConfigureServices();

            var settings = Services.GetRequiredService<ISettingsService>();
            settings.Load();
            Services.GetRequiredService<IThemeService>().Apply(settings.Current.Theme);

            desktop.MainWindow = CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static Window CreateMainWindow()
    {
        var viewModel = Services.GetRequiredService<MainWindowViewModel>();
        return new MainWindow { DataContext = viewModel };
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddDbContextFactory<BrowserDbContext>(options =>
            options.UseSqlite(DatabaseInitializer.ConnectionString));

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<IBookmarkService, BookmarkService>();
        services.AddSingleton<IDownloadService, DownloadService>();

        services.AddSingleton<IBrowserEngineFactory, NativeWebViewEngineFactory>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
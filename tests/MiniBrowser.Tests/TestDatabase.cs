using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniBrowser.Infrastructure.Persistence;

namespace MiniBrowser.Tests;

/// <summary>
/// A temporary SQLite database (file-based, created in %TEMP%) used by the
/// history/bookmark service tests so the real user database is never touched.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public TestDatabase()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "MiniBrowserTests",
            $"{Guid.NewGuid():N}.db");

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);

        Factory = CreateFactory(Path);
        using var context = Factory.CreateDbContext();
        context.Database.EnsureCreated();
    }

    public string Path { get; }

    public IDbContextFactory<BrowserDbContext> Factory { get; }

    private static IDbContextFactory<BrowserDbContext> CreateFactory(string path)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<BrowserDbContext>(options => options.UseSqlite($"Data Source={path}"));
        return services.BuildServiceProvider().GetRequiredService<IDbContextFactory<BrowserDbContext>>();
    }

    public void Dispose()
    {
        try
        {
            File.Delete(Path);
        }
        catch
        {
            // Best-effort cleanup; a locked file must not fail a test run.
        }
    }
}

/// <summary>An IDbContextFactory that always fails, for resilience tests.</summary>
public sealed class ThrowingDbContextFactory : IDbContextFactory<BrowserDbContext>
{
    public BrowserDbContext CreateDbContext() => throw new InvalidOperationException("simulated database failure");

    public Task<BrowserDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("simulated database failure");
}
using Microsoft.EntityFrameworkCore;
using MiniBrowser.Infrastructure.Persistence;

namespace MiniBrowser.Infrastructure.Persistence;

/// <summary>
/// Convenience factory for creating <see cref="BrowserDbContext"/> instances from
/// a raw SQLite connection string, used outside of the DI container (startup).
/// </summary>
public static class BrowserDbContextFactory
{
    public static BrowserDbContext Create(string connectionString)
    {
        var options = new DbContextOptionsBuilder<BrowserDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new BrowserDbContext(options);
    }
}
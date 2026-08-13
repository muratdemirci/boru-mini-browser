using Microsoft.EntityFrameworkCore;
using MiniBrowser.Core.Models;
using MiniBrowser.Infrastructure.Persistence.Configurations;

namespace MiniBrowser.Infrastructure.Persistence;

public sealed class BrowserDbContext : DbContext
{
    public BrowserDbContext(DbContextOptions<BrowserDbContext> options)
        : base(options)
    {
    }

    public DbSet<HistoryEntry> HistoryEntries => Set<HistoryEntry>();

    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new HistoryEntryConfiguration());
        modelBuilder.ApplyConfiguration(new BookmarkConfiguration());
    }
}
namespace MiniBrowser.Infrastructure.Persistence;

/// <summary>
/// Owns the SQLite database location and initial schema creation.
/// The database lives under the per-user application data directory so it
/// survives application restarts and is never written next to the project.
/// </summary>
public static class DatabaseInitializer
{
    /// <summary>Full path of the SQLite database file.</summary>
    public static string DatabasePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MiniBrowser",
        "browser.db");

    /// <summary>EF Core SQLite connection string for <see cref="DatabasePath"/>.</summary>
    public static string ConnectionString => $"Data Source={DatabasePath}";

    /// <summary>
    /// Creates the database schema if the database file does not exist yet.
    /// Returns false (without throwing) when the database cannot be initialized;
    /// the browser must keep working in that case.
    /// </summary>
    public static bool EnsureCreated()
    {
        try
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var context = BrowserDbContextFactory.Create(ConnectionString);
            context.Database.EnsureCreated();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
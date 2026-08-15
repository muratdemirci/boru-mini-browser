using System.Text;

namespace MiniBrowser.Services.Downloads;

/// <summary>
/// Pure, testable helpers for turning an arbitrary file name (possibly attacker
/// controlled via Content-Disposition) into a safe, unique path inside the
/// download directory.
/// </summary>
public static class DownloadPathHelper
{
    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// Produces a safe file name that cannot escape its directory:
    /// strips any path, removes invalid Windows characters, trims trailing
    /// dots/spaces and guards against reserved device names.
    /// </summary>
    public static string SanitizeFileName(string? fileName)
    {
        var name = fileName ?? string.Empty;
        name = name.Trim();

        if (string.IsNullOrEmpty(name))
        {
            return "download";
        }

        // Never allow directory traversal or embedded separators.
        name = Path.GetFileName(name);
        if (string.IsNullOrEmpty(name))
        {
            return "download";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(invalid.Contains(c) ? '_' : c);
        }

        name = sb.ToString();
        name = name.Trim().TrimEnd('.', ' ');

        if (string.IsNullOrEmpty(name))
        {
            return "download";
        }

        var stem = Path.GetFileNameWithoutExtension(name);
        if (ReservedDeviceNames.Contains(stem))
        {
            name = "_" + name;
        }

        return name;
    }

    /// <summary>
    /// Returns a path in <paramref name="directory"/> that does not already
    /// exist, appending " (n)" before the extension for duplicates.
    /// </summary>
    public static string GenerateUniquePath(string directory, string fileName, Func<string, bool> exists)
    {
        ArgumentNullException.ThrowIfNull(exists);

        var safeName = SanitizeFileName(fileName);
        var candidate = Path.Combine(directory, safeName);

        if (!exists(candidate))
        {
            return candidate;
        }

        var stem = Path.GetFileNameWithoutExtension(safeName);
        var ext = Path.GetExtension(safeName);
        var n = 1;
        while (true)
        {
            candidate = Path.Combine(directory, $"{stem} ({n}){ext}");
            if (!exists(candidate))
            {
                return candidate;
            }

            n++;
        }
    }

    /// <summary>
    /// Resolves a file path and verifies it stays within <paramref name="directory"/>,
    /// blocking attempts that walk up the tree (e.g. "..\evil.exe").
    /// </summary>
    public static bool IsPathWithinDirectory(string directory, string candidatePath)
    {
        try
        {
            var dir = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var full = Path.GetFullPath(candidatePath);
            return full.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves the default download directory: %USERPROFILE%\Downloads with a
    /// safe fallback under LocalAppData when it cannot be used.
    /// </summary>
    public static string ResolveDownloadDirectory()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var primary = string.IsNullOrEmpty(profile)
            ? string.Empty
            : Path.Combine(profile, "Downloads");

        try
        {
            if (!string.IsNullOrEmpty(primary))
            {
                Directory.CreateDirectory(primary);
                return primary;
            }
        }
        catch (Exception)
        {
            // Fall through to the fallback location.
        }

        var fallback = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniBrowser", "Downloads");
        Directory.CreateDirectory(fallback);
        return fallback;
    }
}
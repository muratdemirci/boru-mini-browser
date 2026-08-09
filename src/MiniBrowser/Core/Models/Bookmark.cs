namespace MiniBrowser.Core.Models;

/// <summary>
/// A user-saved bookmark. Bookmarks are flat (no folder support yet).
/// </summary>
public sealed class Bookmark
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }
}
namespace MiniBrowser.Core.Models;

/// <summary>
/// A single visited page stored in the browser history.
/// </summary>
public sealed class HistoryEntry
{
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>Page title; may be null or empty when the page has no title.</summary>
    public string? Title { get; set; }

    public DateTime VisitedAt { get; set; }
}
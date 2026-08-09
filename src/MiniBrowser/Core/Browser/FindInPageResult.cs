namespace MiniBrowser.Core.Browser;

/// <summary>
/// Result of a find-in-page search: the number of matches found and the
/// zero-based index of the match that is currently highlighted.
/// </summary>
public readonly record struct FindInPageResult(int MatchCount, int CurrentMatchIndex)
{
    /// <summary>The result reported when no matches exist.</summary>
    public static FindInPageResult None { get; } = new(0, -1);

    public bool HasMatches => MatchCount > 0;
}

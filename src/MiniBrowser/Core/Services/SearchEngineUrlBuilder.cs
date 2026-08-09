using MiniBrowser.Core.Models;

namespace MiniBrowser.Core.Services;

/// <summary>
/// Builds a search-engine URL from a plain-text query. Used when the address
/// bar input is not a valid web address.
/// </summary>
public static class SearchEngineUrlBuilder
{
    /// <summary>
    /// Returns an absolute search URL for <paramref name="query"/> using the
    /// given engine. The query is percent-encoded so it is safe to embed.
    /// </summary>
    public static Uri Build(SearchEnginePreference engine, string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var encoded = Uri.EscapeDataString(query.Trim());
        var template = engine switch
        {
            SearchEnginePreference.Bing => "https://www.bing.com/search?q={0}",
            SearchEnginePreference.DuckDuckGo => "https://duckduckgo.com/?q={0}",
            _ => "https://www.muratdemirci.org/search?q={0}",
        };

        return new Uri(string.Format(template, encoded), UriKind.Absolute);
    }
}

using System.Globalization;
using System.Net;
using MiniBrowser.Core.Models;

namespace MiniBrowser.Core.Services;

/// <summary>
/// Builds the internal New Tab page shown for empty tabs. The page is rendered
/// via <c>NavigateToString</c>, so it carries no real URL; a small form posts
/// the query to the configured search engine and a link points at the homepage.
/// </summary>
public static class NewTabPage
{
    public static string Build(SearchEnginePreference engine, string homepage)
    {
        var searchTemplate = engine switch
        {
            SearchEnginePreference.Bing => "https://www.bing.com/search",
            SearchEnginePreference.DuckDuckGo => "https://duckduckgo.com/",
            _ => "https://www.muratdemirci.org/search",
        };

        var engineLabel = engine switch
        {
            SearchEnginePreference.Bing => "Bing",
            SearchEnginePreference.DuckDuckGo => "DuckDuckGo",
            _ => "Google",
        };

        var safeHomepage = string.IsNullOrWhiteSpace(homepage)
            ? "https://www.google.com"
            : homepage;

        var body = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>New Tab</title>
              <style>
                html, body { margin: 0; padding: 0; height: 100%; }
                body { font-family: system-ui, sans-serif; background: #f5f6f8; color: #202124; }
                .wrap { max-width: 640px; margin: 0 auto; padding: 96px 24px; text-align: center; }
                h1 { font-size: 40px; margin: 0 0 32px; font-weight: 300; letter-spacing: -1px; }
                form { display: flex; gap: 8px; }
                input[type=text] {
                  flex: 1; padding: 14px 16px; font-size: 16px; border: 1px solid #dadce0;
                  border-radius: 24px; outline: none;
                }
                input[type=text]:focus { border-color: #4285f4; }
                button {
                  padding: 0 24px; border: 0; border-radius: 24px; background: #4285f4;
                  color: #fff; font-size: 16px; cursor: pointer;
                }
                .links { margin-top: 40px; font-size: 14px; }
                .links a { color: #4285f4; text-decoration: none; margin: 0 12px; }
                .links a:hover { text-decoration: underline; }
              </style>
            </head>
            <body>
              <div class="wrap">
                <h1>MiniBrowser</h1>
                <form action="{{searchTemplate}}" method="get" autocomplete="off">
                  <input type="text" name="q" placeholder="Search the web with {{engineLabel}}" aria-label="Search">
                  <button type="submit">Search</button>
                </form>
                <div class="links">
                  <a href="{{WebUtility.HtmlEncode(safeHomepage)}}">Homepage</a>
                  <a href="https://github.com/">GitHub</a>
                  <a href="https://www.wikipedia.org/">Wikipedia</a>
                </div>
              </div>
            </body>
            </html>
            """;

        return body;
    }
}

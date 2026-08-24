using MiniBrowser.Core.Models;

namespace MiniBrowser.ViewModels;

/// <summary>Display model for a single bookmark.</summary>
public sealed class BookmarkItemViewModel
{
    public BookmarkItemViewModel(Bookmark bookmark)
    {
        Id = bookmark.Id;
        Url = bookmark.Url;
        Title = string.IsNullOrWhiteSpace(bookmark.Title) ? bookmark.Url : bookmark.Title;
        AutomationName = $"{Title} {Url}";
    }

    public int Id { get; }

    public string Url { get; }

    public string Title { get; }

    /// <summary>Combined title + URL, used for accessibility and UI automation.</summary>
    public string AutomationName { get; }
}
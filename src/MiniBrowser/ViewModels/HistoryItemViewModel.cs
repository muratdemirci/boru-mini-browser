using MiniBrowser.Core.Models;

namespace MiniBrowser.ViewModels;

/// <summary>Display model for a single history entry.</summary>
public sealed class HistoryItemViewModel
{
    public HistoryItemViewModel(HistoryEntry entry)
    {
        Id = entry.Id;
        Url = entry.Url;
        Title = string.IsNullOrWhiteSpace(entry.Title) ? entry.Url : entry.Title;
        VisitedAt = entry.VisitedAt;
        DisplayTime = entry.VisitedAt.ToLocalTime().ToString("HH:mm");
        AutomationName = $"{Title} {Url}";
    }

    public int Id { get; }

    public string Url { get; }

    public string Title { get; }

    public DateTime VisitedAt { get; }

    public string DisplayTime { get; }

    /// <summary>Combined title + URL, used for accessibility and UI automation.</summary>
    public string AutomationName { get; }
}

/// <summary>Display model for a date-bucketed history section.</summary>
public sealed class HistoryGroupViewModel
{
    public HistoryGroupViewModel(string title, IEnumerable<HistoryItemViewModel> items)
    {
        Title = title;
        Items = items.ToList();
    }

    public string Title { get; }

    public IReadOnlyList<HistoryItemViewModel> Items { get; }
}
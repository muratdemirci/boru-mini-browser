using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniBrowser.Services.History;

namespace MiniBrowser.ViewModels;

/// <summary>
/// View model for the History window: search, date-grouped list, and clear-with-
/// confirmation. Opening an entry navigates the active tab (via a callback).
/// </summary>
public partial class HistoryViewModel : ViewModelBase
{
    private readonly IHistoryService _service;
    private readonly Action<string> _openUrl;

    public HistoryViewModel(IHistoryService service, Action<string> openUrl)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(openUrl);

        _service = service;
        _openUrl = openUrl;
        StatusMessage = service.IsAvailable ? null : $"History is unavailable: {service.LastError}";
    }

    public ObservableCollection<HistoryGroupViewModel> Groups { get; } = [];

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    [ObservableProperty]
    private bool isClearConfirmVisible;

    public bool IsAvailable => _service.IsAvailable;

    partial void OnSearchTextChanged(string value)
    {
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (!_service.IsAvailable)
        {
            Groups.Clear();
            StatusMessage ??= "History is unavailable: the database could not be reached.";
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var query = SearchText?.Trim() ?? string.Empty;
            var items = await (query.Length == 0
                ? _service.GetRecentAsync(200)
                : _service.SearchAsync(query, 200));

            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);

            var groups = new List<HistoryGroupViewModel>
            {
                new("Today", items.Where(i => i.VisitedAt.ToLocalTime().Date == today).Select(i => new HistoryItemViewModel(i))),
                new("Yesterday", items.Where(i => i.VisitedAt.ToLocalTime().Date == yesterday).Select(i => new HistoryItemViewModel(i))),
                new("Earlier", items.Where(i => i.VisitedAt.ToLocalTime().Date < yesterday).Select(i => new HistoryItemViewModel(i))),
            }.Where(g => g.Items.Count > 0).ToList();

            Groups.Clear();
            foreach (var group in groups)
            {
                Groups.Add(group);
            }
        }
        catch (Exception)
        {
            Groups.Clear();
            StatusMessage = "History is unavailable: the database could not be reached.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Open(HistoryItemViewModel? item)
    {
        if (item is not null)
        {
            _openUrl(item.Url);
        }
    }

    [RelayCommand]
    private void ClearHistory() => IsClearConfirmVisible = true;

    [RelayCommand]
    private void CancelClear() => IsClearConfirmVisible = false;

    [RelayCommand]
    private async Task ConfirmClearAsync()
    {
        IsClearConfirmVisible = false;
        await _service.ClearAsync();
        await RefreshAsync();
    }
}
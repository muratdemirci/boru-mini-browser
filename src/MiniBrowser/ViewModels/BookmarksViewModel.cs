using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniBrowser.Services.Bookmarks;

namespace MiniBrowser.ViewModels;

/// <summary>
/// View model for the Bookmarks window: flat list of bookmarks with remove and
/// open-in-active-tab (via a callback).
/// </summary>
public partial class BookmarksViewModel : ViewModelBase
{
    private readonly IBookmarkService _service;
    private readonly Action<string> _openUrl;

    public BookmarksViewModel(IBookmarkService service, Action<string> openUrl)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(openUrl);

        _service = service;
        _openUrl = openUrl;
        StatusMessage = service.IsAvailable ? null : $"Bookmarks are unavailable: {service.LastError}";
    }

    public ObservableCollection<BookmarkItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? statusMessage;

    public bool IsAvailable => _service.IsAvailable;

    public async Task RefreshAsync()
    {
        if (!_service.IsAvailable)
        {
            Items.Clear();
            StatusMessage ??= "Bookmarks are unavailable: the database could not be reached.";
            return;
        }

        IsBusy = true;
        StatusMessage = null;

        try
        {
            var bookmarks = await _service.GetAllAsync();

            Items.Clear();
            foreach (var bookmark in bookmarks)
            {
                Items.Add(new BookmarkItemViewModel(bookmark));
            }
        }
        catch (Exception)
        {
            Items.Clear();
            StatusMessage = "Bookmarks are unavailable: the database could not be reached.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Raised after a bookmark was added or removed, so other views
    /// (e.g. the main window's star) can refresh their state.</summary>
    public event Action? Changed;

    [RelayCommand]
    private void Open(BookmarkItemViewModel? item)
    {
        if (item is not null)
        {
            _openUrl(item.Url);
        }
    }

    [RelayCommand]
    private async Task RemoveAsync(BookmarkItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        await _service.RemoveAsync(item.Url);
        await RefreshAsync();
        Changed?.Invoke();
    }
}
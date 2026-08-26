using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniBrowser.Core.Models;
using MiniBrowser.Services.Downloads;

namespace MiniBrowser.ViewModels;

/// <summary>
/// View model for the Downloads window. Backs onto <see cref="IDownloadService"/>
/// and exposes per-item commands for open/cancel/remove/retry.
/// </summary>
public partial class DownloadsViewModel : ViewModelBase
{
    private readonly IDownloadService _service;
    private readonly Action<string> _openUrl;

    public DownloadsViewModel(IDownloadService service, Action<string> openUrl)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(openUrl);

        _service = service;
        _openUrl = openUrl;

        _service.Downloads.CollectionChanged += OnDownloadsCollectionChanged;
        foreach (var item in _service.Downloads)
        {
            Items.Add(new DownloadItemViewModel(item));
        }

        StatusMessage = Items.Count == 0 ? "No downloads yet." : null;
    }

    public ObservableCollection<DownloadItemViewModel> Items { get; } = [];

    [ObservableProperty]
    private string? statusMessage;

    public string DownloadDirectory => _service.DownloadDirectory;

    [RelayCommand]
    private void Open(DownloadItemViewModel? item)
    {
        if (item is not null)
        {
            _service.OpenDownloadedFile(item.Id);
        }
    }

    [RelayCommand]
    private void ShowInFolder(DownloadItemViewModel? item)
    {
        if (item is not null)
        {
            _service.OpenDownloadFolder(item.Id);
        }
    }

    [RelayCommand]
    private void Cancel(DownloadItemViewModel? item)
    {
        if (item is not null)
        {
            _service.CancelDownload(item.Id);
        }
    }

    [RelayCommand]
    private void Remove(DownloadItemViewModel? item)
    {
        if (item is not null)
        {
            _service.RemoveDownload(item.Id);
        }
    }

    [RelayCommand]
    private void Retry(DownloadItemViewModel? item)
    {
        if (item is not null)
        {
            _service.RemoveDownload(item.Id);
            _openUrl(item.Url);
        }
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        _service.ClearCompletedDownloads();
    }

    private void OnDownloadsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            foreach (var item in e.NewItems?.Cast<DownloadItem>() ?? [])
            {
                Items.Add(new DownloadItemViewModel(item));
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Remove)
        {
            foreach (var item in e.OldItems?.Cast<DownloadItem>() ?? [])
            {
                var vm = Items.FirstOrDefault(i => i.Id == item.Id);
                if (vm is not null)
                {
                    Items.Remove(vm);
                }
            }
        }
        else if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            Items.Clear();
        }

        StatusMessage = Items.Count == 0 ? "No downloads yet." : null;
    }
}
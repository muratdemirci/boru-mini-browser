using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MiniBrowser.Core.Models;

namespace MiniBrowser.ViewModels;

/// <summary>
/// View model for a single row in the Downloads window. Wraps a
/// <see cref="DownloadItem"/> and reflects its progress/state changes.
/// </summary>
public sealed class DownloadItemViewModel : ViewModelBase
{
    private readonly DownloadItem _item;

    public DownloadItemViewModel(DownloadItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _item = item;
        _item.PropertyChanged += OnItemPropertyChanged;
    }

    public Guid Id => _item.Id;

    public string FileName => _item.FileName;

    public string Url => _item.Url;

    public string FilePath => _item.FilePath;

    public string StatusText => _item.StatusText;

    public string ProgressText => _item.ProgressText;

    public bool IsDeterminateProgress => _item.IsDeterminateProgress;

    public double? Progress => _item.Progress;

    public bool HasFailed => _item.HasFailed;

    public bool IsCompleted => _item.Status == DownloadStatus.Completed;

    public bool IsActive => _item.Status is DownloadStatus.Pending or DownloadStatus.Downloading;

    public bool CanOpen => _item.Status == DownloadStatus.Completed;

    public bool CanCancel => _item.Status is DownloadStatus.Pending or DownloadStatus.Downloading;

    public bool IsCancelOrRemoveVisible => _item.Status is DownloadStatus.Pending or DownloadStatus.Downloading;

    public string StartedAtText => _item.StartedAt.ToLocalTime().ToString("g");

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DownloadItem.Status):
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(HasFailed));
                OnPropertyChanged(nameof(IsCompleted));
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(CanOpen));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(IsCancelOrRemoveVisible));
                break;

            case nameof(DownloadItem.DownloadedBytes):
            case nameof(DownloadItem.TotalBytes):
                OnPropertyChanged(nameof(ProgressText));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(IsDeterminateProgress));
                break;
        }
    }
}
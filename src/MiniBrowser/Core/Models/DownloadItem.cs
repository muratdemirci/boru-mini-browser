using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MiniBrowser.Core.Models;

/// <summary>
/// A single download tracked by the browser. Mutable so progress and state
/// transitions can be observed by the UI.
/// </summary>
public sealed class DownloadItem : INotifyPropertyChanged
{
    private long _downloadedBytes;
    private long? _totalBytes;
    private DownloadStatus _status;
    private DateTimeOffset? _completedAt;
    private string? _errorMessage;

    public DownloadItem(string fileName, string url, string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FileName = fileName;
        Url = url;
        FilePath = filePath;
        StartedAt = DateTimeOffset.Now;
        Status = DownloadStatus.Pending;
    }

    /// <summary>Stable identifier within the session.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>File name that will be (or was) written to disk.</summary>
    public string FileName { get; }

    /// <summary>The URL the download originated from.</summary>
    public string Url { get; }

    /// <summary>Absolute path the file is saved to.</summary>
    public string FilePath { get; }

    /// <summary>When the download was registered.</summary>
    public DateTimeOffset StartedAt { get; }

    public long DownloadedBytes
    {
        get => _downloadedBytes;
        set
        {
            if (_downloadedBytes == value)
            {
                return;
            }

            _downloadedBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(IsDeterminateProgress));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    /// <summary>Total size in bytes, or null while it is unknown.</summary>
    public long? TotalBytes
    {
        get => _totalBytes;
        set
        {
            if (_totalBytes == value)
            {
                return;
            }

            _totalBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Progress));
            OnPropertyChanged(nameof(IsDeterminateProgress));
            OnPropertyChanged(nameof(ProgressText));
        }
    }

    /// <summary>0..100 when the total size is known; null otherwise.</summary>
    public double? Progress =>
        TotalBytes is > 0 ? Math.Min(100, DownloadedBytes * 100.0 / TotalBytes.Value) : null;

    /// <summary>True while progress is based on known total bytes.</summary>
    public bool IsDeterminateProgress => TotalBytes is > 0;

    public string ProgressText =>
        IsDeterminateProgress
            ? $"{Progress:0}% ({FormatBytes(DownloadedBytes)} / {FormatBytes(TotalBytes ?? 0)})"
            : FormatBytes(DownloadedBytes);

    public DownloadStatus Status
    {
        get => _status;
        set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(HasFailed));
        }
    }

    public string StatusText => Status switch
    {
        DownloadStatus.Pending => "Pending",
        DownloadStatus.Downloading => "Downloading",
        DownloadStatus.Completed => "Completed",
        DownloadStatus.Failed => "Failed",
        DownloadStatus.Cancelled => "Cancelled",
        _ => Status.ToString(),
    };

    /// <summary>Set when the download reached a terminal state.</summary>
    public DateTimeOffset? CompletedAt
    {
        get => _completedAt;
        set
        {
            if (_completedAt == value)
            {
                return;
            }

            _completedAt = value;
            OnPropertyChanged();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool HasFailed => Status == DownloadStatus.Failed;

    public event PropertyChangedEventHandler? PropertyChanged;

    public void MarkStarted()
    {
        if (Status == DownloadStatus.Pending)
        {
            Status = DownloadStatus.Downloading;
        }
    }

    public void MarkCompleted()
    {
        Status = DownloadStatus.Completed;
        CompletedAt = DateTimeOffset.Now;
    }

    public void MarkCancelled()
    {
        Status = DownloadStatus.Cancelled;
        CompletedAt = DateTimeOffset.Now;
    }

    public void MarkFailed(string? errorMessage = null)
    {
        Status = DownloadStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTimeOffset.Now;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{value:0.#} {units[unit]}";
    }
}
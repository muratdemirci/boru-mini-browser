using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using MiniBrowser.Core.Browser;
using MiniBrowser.Core.Models;
using MiniBrowser.Services.Settings;

namespace MiniBrowser.Services.Downloads;

/// <summary>
/// Runtime download manager. Files are written to <see cref="DownloadDirectory"/>;
/// paths are sanitised and uniquified so downloads can never escape the directory
/// or overwrite existing files.
/// </summary>
public sealed class DownloadService : IDownloadService
{
    private readonly Dictionary<Guid, IDownloadOperation> _operations = [];
    private readonly Dictionary<Guid, DownloadItem> _itemsById = [];

    public DownloadService()
    {
        DownloadDirectory = DownloadPathHelper.ResolveDownloadDirectory();
    }

    /// <summary>
    /// Creates a service writing to a specific directory (used by tests to avoid
    /// touching the user's real Downloads folder).
    /// </summary>
    public DownloadService(string downloadDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(downloadDirectory);
        Directory.CreateDirectory(downloadDirectory);
        DownloadDirectory = downloadDirectory;
    }

    /// <summary>
    /// Creates a service whose download directory comes from settings; falls back
    /// to the default location when no custom directory is configured.
    /// </summary>
    public DownloadService(ISettingsService settingsService)
    {
        ArgumentNullException.ThrowIfNull(settingsService);

        var configured = settingsService.Current.DownloadDirectory;
        DownloadDirectory = !string.IsNullOrWhiteSpace(configured)
            ? configured
            : DownloadPathHelper.ResolveDownloadDirectory();
    }

    public ObservableCollection<DownloadItem> Downloads { get; } = [];

    public string DownloadDirectory { get; }

    public void HandleDownloadStarting(DownloadStartingEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var suggestedPath = args.SuggestedResultFilePath;
        var suggestedName = string.IsNullOrEmpty(suggestedPath)
            ? "download"
            : Path.GetFileName(suggestedPath);

        var safeName = DownloadPathHelper.SanitizeFileName(suggestedName);
        var targetPath = DownloadPathHelper.GenerateUniquePath(
            DownloadDirectory,
            safeName,
            path => File.Exists(path) || Downloads.Any(d => PathEquals(d.FilePath, path)));

        // Apply the decisions back to the engine event.
        args.ResultFilePath = targetPath;
        args.Handled = true; // hide the default download dialog; we show our own UI.
        args.Cancel = false;

        var item = new DownloadItem(safeName, args.Uri?.AbsoluteUri ?? string.Empty, targetPath);
        _itemsById[item.Id] = item;
        _operations[item.Id] = args.Operation;
        Downloads.Insert(0, item);

        item.MarkStarted();
        SubscribeToOperation(item, args.Operation);
    }

    public void CancelDownload(Guid id)
    {
        if (!_itemsById.TryGetValue(id, out var item))
        {
            return;
        }

        if (item.Status is DownloadStatus.Downloading or DownloadStatus.Pending)
        {
            item.MarkCancelled();
            if (_operations.TryGetValue(id, out var operation))
            {
                operation.Cancel();
            }
        }
    }

    public void RemoveDownload(Guid id)
    {
        if (!_itemsById.Remove(id, out var item))
        {
            return;
        }

        if (_operations.Remove(id, out var operation))
        {
            operation.Dispose();
        }

        Downloads.Remove(item);
    }

    public void ClearCompletedDownloads()
    {
        var completed = Downloads.Where(d => d.Status is DownloadStatus.Completed
            or DownloadStatus.Failed
            or DownloadStatus.Cancelled).ToList();
        foreach (var item in completed)
        {
            RemoveDownload(item.Id);
        }
    }

    public void OpenDownloadedFile(Guid id)
    {
        if (!_itemsById.TryGetValue(id, out var item) || item.Status != DownloadStatus.Completed)
        {
            return;
        }

        if (!File.Exists(item.FilePath))
        {
            return;
        }

        OpenWithDefaultHandler(item.FilePath);
    }

    public void OpenDownloadFolder(Guid id)
    {
        if (!_itemsById.TryGetValue(id, out var item))
        {
            return;
        }

        var directory = Path.GetDirectoryName(item.FilePath);
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        OpenWithDefaultHandler(directory);
    }

    private void SubscribeToOperation(DownloadItem item, IDownloadOperation operation)
    {
        operation.BytesReceivedChanged += (_, _) =>
        {
            item.DownloadedBytes = operation.BytesReceived;
            if (operation.TotalBytesToReceive >= 0)
            {
                item.TotalBytes = operation.TotalBytesToReceive;
            }
        };

        operation.StateChanged += (_, _) => ApplyOperationState(item, operation);
    }

    private void ApplyOperationState(DownloadItem item, IDownloadOperation operation)
    {
        switch (operation.State)
        {
            case DownloadOperationState.Completed:
                item.DownloadedBytes = operation.BytesReceived;
                item.MarkCompleted();
                break;

            case DownloadOperationState.Interrupted:
                item.MarkFailed("Download interrupted.");
                break;
        }
    }

    private static bool PathEquals(string a, string b)
    {
        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void OpenWithDefaultHandler(string path)
    {
        try
        {
            var startInfo = new ProcessStartInfo(path) { UseShellExecute = true };
            Process.Start(startInfo);
        }
        catch (Exception)
        {
            // Opening is best-effort; failures are surfaced through the absence
            // of a launched app rather than crashing the browser.
        }
    }
}
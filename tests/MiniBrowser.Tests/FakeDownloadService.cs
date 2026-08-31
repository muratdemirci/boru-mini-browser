using System.Collections.ObjectModel;
using MiniBrowser.Core.Browser;
using MiniBrowser.Core.Models;
using MiniBrowser.Services.Downloads;

namespace MiniBrowser.Tests;

/// <summary>
/// In-memory <see cref="IDownloadService"/> for view-model tests. Mirrors the
/// production service's observable collection contract without touching disk.
/// </summary>
internal sealed class FakeDownloadService : IDownloadService
{
    public FakeDownloadService(string? downloadDirectory = null)
    {
        DownloadDirectory = downloadDirectory ?? Path.Combine(Path.GetTempPath(), "MiniBrowserTests", "Downloads");
        Directory.CreateDirectory(DownloadDirectory);
    }

    public ObservableCollection<DownloadItem> Downloads { get; } = [];

    public string DownloadDirectory { get; }

    public List<(Guid Id, string FileName, string Url, string FilePath)> Tracked { get; } = [];

    public int CancelCalls { get; private set; }

    public int RemoveCalls { get; private set; }

    public int ClearCompletedCalls { get; private set; }

    public int OpenFileCalls { get; private set; }

    public int OpenFolderCalls { get; private set; }

    public void HandleDownloadStarting(DownloadStartingEventArgs args)
    {
        var safe = DownloadPathHelper.SanitizeFileName(args.SuggestedResultFilePath);
        var path = Path.Combine(DownloadDirectory, safe);
        args.ResultFilePath = path;
        args.Handled = true;
        args.Cancel = false;

        var item = new DownloadItem(safe, args.Uri?.AbsoluteUri ?? string.Empty, path);
        Downloads.Insert(0, item);
        Tracked.Add((item.Id, item.FileName, item.Url, item.FilePath));
    }

    public void CancelDownload(Guid id)
    {
        CancelCalls++;
    }

    public void RemoveDownload(Guid id)
    {
        RemoveCalls++;
        var item = Downloads.FirstOrDefault(d => d.Id == id);
        if (item is not null)
        {
            Downloads.Remove(item);
        }
    }

    public void ClearCompletedDownloads()
    {
        ClearCompletedCalls++;
    }

    public void OpenDownloadedFile(Guid id)
    {
        OpenFileCalls++;
    }

    public void OpenDownloadFolder(Guid id)
    {
        OpenFolderCalls++;
    }
}
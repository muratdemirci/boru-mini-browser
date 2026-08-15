using System.Collections.ObjectModel;
using MiniBrowser.Core.Browser;
using MiniBrowser.Core.Models;

namespace MiniBrowser.Services.Downloads;

/// <summary>
/// Manages the browser's downloads at runtime. Downloads are tracked in memory
/// for the current session; metadata is not persisted.
/// </summary>
public interface IDownloadService
{
    /// <summary>All downloads for the current session, newest first.</summary>
    ObservableCollection<DownloadItem> Downloads { get; }

    /// <summary>Directory downloaded files are written to.</summary>
    string DownloadDirectory { get; }

    /// <summary>
    /// Called when the engine reports a download starting. The implementation
    /// sanitises the target path, sets the final <see cref="DownloadStartingEventArgs"/>
    /// properties, tracks the item and subscribes to progress/state events.
    /// </summary>
    void HandleDownloadStarting(DownloadStartingEventArgs args);

    void CancelDownload(Guid id);

    void RemoveDownload(Guid id);

    void ClearCompletedDownloads();

    /// <summary>Opens a completed download with the OS default handler.</summary>
    void OpenDownloadedFile(Guid id);

    /// <summary>Opens the folder containing a download in the shell.</summary>
    void OpenDownloadFolder(Guid id);
}
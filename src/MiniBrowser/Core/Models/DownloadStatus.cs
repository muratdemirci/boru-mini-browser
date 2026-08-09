namespace MiniBrowser.Core.Models;

/// <summary>
/// Lifecycle state of a single download in the Downloads manager.
/// </summary>
public enum DownloadStatus
{
    Pending,
    Downloading,
    Completed,
    Failed,
    Cancelled,
}
namespace MiniBrowser.Core.Browser;

/// <summary>
/// Carries the details of a native download that is about to start. The host can
/// cancel it, change where the file is saved, and hide the default download
/// dialog by setting the corresponding properties before the handler returns.
/// </summary>
public sealed class DownloadStartingEventArgs : EventArgs
{
    public DownloadStartingEventArgs(
        string? uri,
        string? mimeType,
        string? suggestedResultFilePath,
        IDownloadOperation operation)
    {
        Operation = operation;
        Uri = uri is null ? null : Uri.TryCreate(uri, UriKind.Absolute, out var parsed) ? parsed : null;
        MimeType = mimeType;
        SuggestedResultFilePath = suggestedResultFilePath;
        ResultFilePath = suggestedResultFilePath;
    }

    public IDownloadOperation Operation { get; }

    public Uri? Uri { get; }

    public string? MimeType { get; }

    /// <summary>The default path (directory + file name) the engine would use.</summary>
    public string? SuggestedResultFilePath { get; }

    /// <summary>Override path the file will be saved to. Defaults to the suggested path.</summary>
    public string? ResultFilePath { get; set; }

    /// <summary>When true the default download dialog is hidden.</summary>
    public bool Handled { get; set; }

    /// <summary>When true the download is cancelled.</summary>
    public bool Cancel { get; set; }
}
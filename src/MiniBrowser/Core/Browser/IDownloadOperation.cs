namespace MiniBrowser.Core.Browser;

/// <summary>
/// State of an underlying native download operation (WebView2 download).
/// </summary>
public enum DownloadOperationState
{
    InProgress = 0,
    Interrupted = 1,
    Completed = 2,
}

/// <summary>
/// Abstraction over the engine's native download operation, exposing progress
/// and state transitions without leaking WebView2 COM details. The engine
/// supplies the implementation; view models and services only depend on this.
/// </summary>
public interface IDownloadOperation : IDisposable
{
    /// <summary>The URI the download originated from.</summary>
    Uri? Uri { get; }

    /// <summary>Content type of the response, if known.</summary>
    string? MimeType { get; }

    /// <summary>Total bytes to receive, or -1 while unknown.</summary>
    long TotalBytesToReceive { get; }

    /// <summary>Bytes received so far.</summary>
    long BytesReceived { get; }

    DownloadOperationState State { get; }

    /// <summary>Raised as bytes are received.</summary>
    event EventHandler? BytesReceivedChanged;

    /// <summary>Raised when the download state changes (e.g. completed/interrupted).</summary>
    event EventHandler? StateChanged;

    /// <summary>Cancels the download if it is still in progress.</summary>
    void Cancel();
}
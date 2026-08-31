using MiniBrowser.Core.Browser;

namespace MiniBrowser.Tests;

/// <summary>
/// Test double for <see cref="IDownloadOperation"/> that lets tests drive
/// progress and state transitions and observe cancellation.
/// </summary>
internal sealed class FakeDownloadOperation : IDownloadOperation
{
    private long _bytesReceived;
    private long _totalBytes = -1;
    private DownloadOperationState _state;

    public Uri? Uri { get; set; }

    public string? MimeType { get; set; }

    public long TotalBytesToReceive
    {
        get => _totalBytes;
        set
        {
            _totalBytes = value;
            BytesReceivedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public long BytesReceived
    {
        get => _bytesReceived;
        set
        {
            _bytesReceived = value;
            BytesReceivedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public DownloadOperationState State
    {
        get => _state;
        set
        {
            _state = value;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public int CancelCalls { get; private set; }

    public bool IsDisposed { get; private set; }

    public event EventHandler? BytesReceivedChanged;

    public event EventHandler? StateChanged;

    public void Cancel() => CancelCalls++;

    public void Dispose() => IsDisposed = true;
}
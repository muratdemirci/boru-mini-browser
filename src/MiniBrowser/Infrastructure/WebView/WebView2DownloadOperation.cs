using System.Runtime.InteropServices;
using MiniBrowser.Core.Browser;

namespace MiniBrowser.Infrastructure.WebView;

/// <summary>
/// <see cref="IDownloadOperation"/> implementation backed by the WebView2
/// <c>ICoreWebView2DownloadOperation</c> COM object. Progress and state changes
/// are surfaced as CLR events and marshalled to the thread that created the
/// operation (the UI thread).
///
/// This type is WebView2 (Windows) specific. CA1416 is suppressed here because
/// the whole application runs on Windows only.
/// </summary>
internal sealed class WebView2DownloadOperation : IDownloadOperation
{
    private readonly IntPtr _operation;
    private readonly IntPtr _bytesReceivedHandlerPtr;
    private readonly IntPtr _stateChangedHandlerPtr;
    private readonly long _bytesReceivedToken;
    private readonly long _stateChangedToken;
    private bool _disposed;

    public WebView2DownloadOperation(IntPtr operation)
    {
        _operation = operation;

        _bytesReceivedHandlerPtr = WebView2EventHandlers.CreateBytesReceivedChangedHandler(RaiseBytesReceivedChanged);
        _stateChangedHandlerPtr = WebView2EventHandlers.CreateStateChangedHandler(RaiseStateChanged);

        int hr = WebView2Com.OperationAddBytesReceivedChanged(_operation, _bytesReceivedHandlerPtr, out _bytesReceivedToken);
        if (hr < 0)
        {
            throw new COMException($"add_BytesReceivedChanged failed (0x{hr:X8})");
        }

        hr = WebView2Com.OperationAddStateChanged(_operation, _stateChangedHandlerPtr, out _stateChangedToken);
        if (hr < 0)
        {
            throw new COMException($"add_StateChanged failed (0x{hr:X8})");
        }
    }

    public Uri? Uri
    {
        get
        {
            var s = WebView2Com.OperationGetUri(_operation);
            return Uri.TryCreate(s, UriKind.Absolute, out var uri) ? uri : null;
        }
    }

    public string? MimeType
    {
        get
        {
            var mime = WebView2Com.OperationGetMimeType(_operation);
            return string.IsNullOrEmpty(mime) ? null : mime;
        }
    }

    public long TotalBytesToReceive => WebView2Com.OperationGetTotalBytesToReceive(_operation);

    public long BytesReceived => WebView2Com.OperationGetBytesReceived(_operation);

    public DownloadOperationState State => (DownloadOperationState)WebView2Com.OperationGetState(_operation);

    public event EventHandler? BytesReceivedChanged;

    public event EventHandler? StateChanged;

    public void Cancel()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        WebView2Com.OperationCancel(_operation);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Remove event handlers (best-effort) and release the operation reference.
        WebView2Com.OperationRemoveBytesReceivedChanged(_operation, _bytesReceivedToken);
        WebView2Com.OperationRemoveStateChanged(_operation, _stateChangedToken);

        WebView2Com.Release(_bytesReceivedHandlerPtr);
        WebView2Com.Release(_stateChangedHandlerPtr);
        WebView2Com.Release(_operation);
    }

    private void RaiseBytesReceivedChanged() => BytesReceivedChanged?.Invoke(this, EventArgs.Empty);

    private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
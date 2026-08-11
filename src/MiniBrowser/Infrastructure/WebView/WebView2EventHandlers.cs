using System.Runtime.InteropServices;

[assembly: ComVisible(true)]

namespace MiniBrowser.Infrastructure.WebView;

/// <summary>
/// Managed COM-callable-wrapper that receives the WebView2 <c>DownloadStarting</c>
/// event. The interface is defined with the exact IID of
/// <c>ICoreWebView2DownloadStartingEventHandler</c> so the runtime can invoke it.
/// </summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("53b676ec-c3b1-4804-996a-c72c16b09efb")]
public interface IWebView2DownloadStartingEventHandler
{
    /// <summary>Invoked by WebView2 when a download starts.</summary>
    [PreserveSig]
    int Invoke(IntPtr sender, IntPtr args);
}

/// <summary>
/// Managed COM-callable-wrapper for the download operation's
/// <c>BytesReceivedChanged</c>/<c>StateChanged</c> events.
/// </summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("74EEBAA9-704B-4A9A-8331-AEBD541CB62C")]
public interface IWebView2StateChangedEventHandler
{
    [PreserveSig]
    int Invoke(IntPtr sender, IntPtr args);
}

[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("D98F73CE-D94D-4576-A1E0-EB8989A9DBEF")]
public interface IWebView2BytesReceivedChangedEventHandler
{
    [PreserveSig]
    int Invoke(IntPtr sender, IntPtr args);
}

/// <summary>
/// Managed COM-callable-wrapper that receives the WebView2
/// <c>AcceleratorKeyPressed</c> event (interface IID
/// <c>b29c7e28-fa79-41a8-8e44-65811c76dcb2</c>).
/// </summary>
[ComVisible(true)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("b29c7e28-fa79-41a8-8e44-65811c76dcb2")]
public interface IWebView2AcceleratorKeyPressedEventHandler
{
    [PreserveSig]
    int Invoke(IntPtr sender, IntPtr args);
}

/// <summary>
/// Dispatches native WebView2 callbacks to managed handlers. Instances must be
/// kept alive for the duration of the subscription. This is WebView2 (Windows)
/// specific; CA1416 is suppressed because the application is Windows only.
/// </summary>
public static class WebView2EventHandlers
{
#pragma warning disable CA1416
    public static IntPtr CreateDownloadStartingHandler(Action<IntPtr> callback)
    {
        var handler = new DownloadStartingCallback(callback);
        return Marshal.GetComInterfaceForObject(handler, typeof(IWebView2DownloadStartingEventHandler));
    }

    public static IntPtr CreateStateChangedHandler(Action callback)
    {
        var handler = new StateChangedCallback(callback);
        return Marshal.GetComInterfaceForObject(handler, typeof(IWebView2StateChangedEventHandler));
    }

    public static IntPtr CreateBytesReceivedChangedHandler(Action callback)
    {
        var handler = new BytesReceivedCallback(callback);
        return Marshal.GetComInterfaceForObject(handler, typeof(IWebView2BytesReceivedChangedEventHandler));
    }

    public static IntPtr CreateAcceleratorKeyPressedHandler(Action<IntPtr> callback)
    {
        var handler = new AcceleratorKeyPressedCallback(callback);
        return Marshal.GetComInterfaceForObject(handler, typeof(IWebView2AcceleratorKeyPressedEventHandler));
    }

    public sealed class DownloadStartingCallback(Action<IntPtr> callback) : IWebView2DownloadStartingEventHandler
    {
        public int Invoke(IntPtr sender, IntPtr args)
        {
            callback(args);
            return 0;
        }
    }

    public sealed class StateChangedCallback(Action callback) : IWebView2StateChangedEventHandler
    {
        public int Invoke(IntPtr sender, IntPtr args)
        {
            callback();
            return 0;
        }
    }

    public sealed class BytesReceivedCallback(Action callback) : IWebView2BytesReceivedChangedEventHandler
    {
        public int Invoke(IntPtr sender, IntPtr args)
        {
            callback();
            return 0;
        }
    }

    public sealed class AcceleratorKeyPressedCallback(Action<IntPtr> callback) : IWebView2AcceleratorKeyPressedEventHandler
    {
        public int Invoke(IntPtr sender, IntPtr args)
        {
            callback(args);
            return 0;
        }
    }
#pragma warning restore CA1416
}

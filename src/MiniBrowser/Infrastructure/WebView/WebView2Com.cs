using System.Runtime.InteropServices;

namespace MiniBrowser.Infrastructure.WebView;

/// <summary>
/// Minimal WebView2 COM interop needed for downloads and DevTools. Raw COM
/// pointers obtained from Avalonia's <see cref="Avalonia.Platform.IWindowsWebView2PlatformHandle"/>
/// are invoked through their vtables via function pointers.
///
/// Vtable slot indices are verified against webview2-sys 0.1.1 and the official
/// WebView2 IDL (SDK 1.0.1108.44+). The WebView2 runtime keeps these layouts
/// stable, so the indices do not depend on the runtime version installed.
///
/// This type is WebView2 (Windows) specific. CA1416 is suppressed here because
/// the whole application runs on Windows only.
/// </summary>
internal static class WebView2Com
{
    private const int IUnknownSlotCount = 3;

    // Interface IDs (verified against the WebView2 reference).
    public static readonly Guid IID_ICoreWebView2 = new("76eceacb-0462-4d94-ac3d-163136c8e710");
    public static readonly Guid IID_ICoreWebView2_4 = new("20D02D59-6DF2-42DC-BD06-F98A694B1302");

    private static IntPtr ReadVTableSlot(IntPtr obj, int slot)
    {
        var vtable = Marshal.ReadIntPtr(obj);
        return Marshal.ReadIntPtr(vtable, (IUnknownSlotCount + slot) * IntPtr.Size);
    }

    private static IntPtr ReadAbsoluteVTableSlot(IntPtr obj, int absoluteSlot)
    {
        var vtable = Marshal.ReadIntPtr(obj);
        return Marshal.ReadIntPtr(vtable, absoluteSlot * IntPtr.Size);
    }

    public static int QueryInterface(IntPtr obj, Guid riid, out IntPtr ppv)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<QueryInterfaceFn>(ReadAbsoluteVTableSlot(obj, 0));
        return fn(obj, riid, out ppv);
    }

    public static void Release(IntPtr obj)
    {
        if (obj == IntPtr.Zero)
        {
            return;
        }

        var fn = Marshal.GetDelegateForFunctionPointer<ReleaseFn>(ReadAbsoluteVTableSlot(obj, 2));
        fn(obj);
    }

    // ---- ICoreWebView2 (base): 58 methods after IUnknown ----
    // OpenDevToolsWindow is method index 48 (verified at runtime).

    public static int OpenDevToolsWindow(IntPtr coreWebView2)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<NoArgFn>(ReadVTableSlot(coreWebView2, 48));
        return fn(coreWebView2);
    }

    // ---- ICoreWebView2_4 (extends _3 _2 base): add_DownloadStarting ----
    // base 58 + _2 7 + _3 5 + _4: add_FrameCreated(0), remove_FrameCreated(1),
    // add_DownloadStarting(2), remove_DownloadStarting(3)
    // => add_DownloadStarting absolute slot = 3 + 58 + 7 + 5 + 2 = 75.

    public static int AddDownloadStarting(IntPtr coreWebView2, IntPtr handler, out long token)
    {
        token = 0;
        if (QueryInterface(coreWebView2, IID_ICoreWebView2_4, out var p4) < 0)
        {
            return -1;
        }

        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<AddHandlerFn>(
                ReadVTableSlot(p4, 75 - IUnknownSlotCount));
            return fn(p4, handler, out token);
        }
        finally
        {
            Release(p4);
        }
    }

    // ---- ICoreWebView2Controller (22 methods after IUnknown) ----
    // Controller interface IID: 4d00c0d1-9434-4eb6-8078-8697a560334f
    // add_AcceleratorKeyPressed is method index 16, remove is index 17.
    // The controller pointer is obtained directly from
    // IWindowsWebView2PlatformHandle.CoreWebView2Controller (no QI needed).

    public static int ControllerAddAcceleratorKeyPressed(IntPtr controller, IntPtr handler, out long token)
    {
        token = 0;
        var fn = Marshal.GetDelegateForFunctionPointer<AddHandlerFn>(ReadVTableSlot(controller, 16));
        return fn(controller, handler, out token);
    }

    public static int ControllerRemoveAcceleratorKeyPressed(IntPtr controller, long token)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<RemoveHandlerFn>(ReadVTableSlot(controller, 17));
        return fn(controller, token);
    }

    // ---- ICoreWebView2AcceleratorKeyPressedEventArgs (6 methods after IUnknown) ----
    // 0 GetKeyEventKind, 1 GetVirtualKey, 2 GetKeyEventLParam,
    // 3 GetPhysicalKeyStatus, 4 GetHandled, 5 PutHandled

    public static int AccelArgsGetKeyEventKind(IntPtr args)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetIntFn>(ReadVTableSlot(args, 0));
        fn(args, out var value);
        return value;
    }

    public static int AccelArgsGetVirtualKey(IntPtr args)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetIntFn>(ReadVTableSlot(args, 1));
        fn(args, out var value);
        return value;
    }

    public static void AccelArgsPutHandled(IntPtr args, bool handled)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<PutBoolFn>(ReadVTableSlot(args, 5));
        fn(args, handled ? 1 : 0);
    }

    // ---- ICoreWebView2DownloadStartingEventArgs (8 methods after IUnknown) ----
    // 0 GetDownloadOperation, 1 GetCancel, 2 PutCancel, 3 GetResultFilePath,
    // 4 PutResultFilePath, 5 GetHandled, 6 PutHandled, 7 GetDeferral

    public static IntPtr ArgsGetDownloadOperation(IntPtr args)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetPtrFn>(ReadVTableSlot(args, 0));
        fn(args, out var value);
        return value;
    }

    public static string ArgsGetResultFilePath(IntPtr args)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetStringFn>(ReadVTableSlot(args, 3));
        fn(args, out var value);
        return ReadAndFreeString(value);
    }

    public static void ArgsPutResultFilePath(IntPtr args, string path)
    {
        var ptr = Marshal.StringToCoTaskMemUni(path);
        try
        {
            var fn = Marshal.GetDelegateForFunctionPointer<PutStringFn>(ReadVTableSlot(args, 4));
            fn(args, ptr);
        }
        finally
        {
            Marshal.FreeCoTaskMem(ptr);
        }
    }

    public static void ArgsPutHandled(IntPtr args, bool handled)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<PutBoolFn>(ReadVTableSlot(args, 6));
        fn(args, handled ? 1 : 0);
    }

    public static void ArgsPutCancel(IntPtr args, bool cancel)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<PutBoolFn>(ReadVTableSlot(args, 2));
        fn(args, cancel ? 1 : 0);
    }

    // ---- ICoreWebView2DownloadOperation (17 methods after IUnknown) ----
    // 0 AddBytesReceivedChanged, 4 AddStateChanged, 6 GetUri, 8 GetMimeType,
    // 9 GetTotalBytesToReceive, 10 GetBytesReceived, 12 GetResultFilePath,
    // 13 GetState, 14 GetInterruptReason, 15 Cancel

    public static int OperationAddBytesReceivedChanged(IntPtr op, IntPtr handler, out long token)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<AddHandlerFn>(ReadVTableSlot(op, 0));
        return fn(op, handler, out token);
    }

    public static int OperationAddStateChanged(IntPtr op, IntPtr handler, out long token)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<AddHandlerFn>(ReadVTableSlot(op, 4));
        return fn(op, handler, out token);
    }

    public static int OperationRemoveBytesReceivedChanged(IntPtr op, long token)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<RemoveHandlerFn>(ReadVTableSlot(op, 1));
        return fn(op, token);
    }

    public static int OperationRemoveStateChanged(IntPtr op, long token)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<RemoveHandlerFn>(ReadVTableSlot(op, 5));
        return fn(op, token);
    }

    public static string OperationGetUri(IntPtr op)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetStringFn>(ReadVTableSlot(op, 6));
        fn(op, out var value);
        return ReadAndFreeString(value);
    }

    public static string OperationGetMimeType(IntPtr op)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetStringFn>(ReadVTableSlot(op, 8));
        fn(op, out var value);
        return ReadAndFreeString(value);
    }

    public static long OperationGetTotalBytesToReceive(IntPtr op)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetLongFn>(ReadVTableSlot(op, 9));
        fn(op, out var value);
        return value;
    }

    public static long OperationGetBytesReceived(IntPtr op)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetLongFn>(ReadVTableSlot(op, 10));
        fn(op, out var value);
        return value;
    }

    public static int OperationGetState(IntPtr op)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<GetIntFn>(ReadVTableSlot(op, 13));
        fn(op, out var value);
        return value;
    }

    public static void OperationCancel(IntPtr op)
    {
        var fn = Marshal.GetDelegateForFunctionPointer<NoArgFn>(ReadVTableSlot(op, 15));
        fn(op);
    }

    private static string ReadAndFreeString(IntPtr value)
    {
        if (value == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            return Marshal.PtrToStringUni(value) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeCoTaskMem(value);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int QueryInterfaceFn(IntPtr self, Guid riid, out IntPtr ppv);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int ReleaseFn(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int NoArgFn(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int AddHandlerFn(IntPtr self, IntPtr handler, out long token);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int RemoveHandlerFn(IntPtr self, long token);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetPtrFn(IntPtr self, out IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetStringFn(IntPtr self, out IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int PutStringFn(IntPtr self, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int PutBoolFn(IntPtr self, int value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetLongFn(IntPtr self, out long value);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetIntFn(IntPtr self, out int value);
}
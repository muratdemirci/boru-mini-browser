using MiniBrowser.Core.Browser;

namespace MiniBrowser.Infrastructure.WebView;

/// <summary>
/// Creates <see cref="NativeWebViewEngine"/> instances for each browser tab.
/// </summary>
public sealed class NativeWebViewEngineFactory : IBrowserEngineFactory
{
    public IBrowserEngine Create() => new NativeWebViewEngine();
}
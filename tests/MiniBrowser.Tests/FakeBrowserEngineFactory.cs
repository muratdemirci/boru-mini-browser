using MiniBrowser.Core.Browser;

namespace MiniBrowser.Tests;

internal sealed class FakeBrowserEngineFactory : IBrowserEngineFactory
{
    public List<FakeBrowserEngine> Created { get; } = [];

    public IBrowserEngine Create()
    {
        var engine = new FakeBrowserEngine();
        Created.Add(engine);
        return engine;
    }
}
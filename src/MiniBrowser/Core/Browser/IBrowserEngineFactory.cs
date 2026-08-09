namespace MiniBrowser.Core.Browser;

/// <summary>
/// Creates independent <see cref="IBrowserEngine"/> instances, one per browser tab.
/// Every tab owns its own engine so navigation state is never shared between tabs.
/// </summary>
public interface IBrowserEngineFactory
{
    IBrowserEngine Create();
}
namespace MiniBrowser.Core.Browser;

/// <summary>
/// High-level lifecycle state of the currently displayed document.
/// Drives UI affordances such as the loading indicator and the error overlay.
/// </summary>
public enum BrowserNavigationState
{
    /// <summary>Nothing has been loaded yet.</summary>
    Idle,

    /// <summary>A navigation is in flight.</summary>
    Loading,

    /// <summary>The current document loaded successfully.</summary>
    Loaded,

    /// <summary>The last navigation failed and an error state is shown.</summary>
    Error,
}
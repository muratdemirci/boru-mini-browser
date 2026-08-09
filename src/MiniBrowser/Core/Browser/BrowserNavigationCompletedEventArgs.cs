namespace MiniBrowser.Core.Browser;

public sealed class BrowserNavigationCompletedEventArgs : EventArgs
{
    public BrowserNavigationCompletedEventArgs(Uri? uri, bool isSuccess, string? title = null)
    {
        Uri = uri;
        IsSuccess = isSuccess;
        Title = title;
    }

    public Uri? Uri { get; }

    public bool IsSuccess { get; }

    /// <summary>Document title after a successful load; null/empty on failure.</summary>
    public string? Title { get; }
}
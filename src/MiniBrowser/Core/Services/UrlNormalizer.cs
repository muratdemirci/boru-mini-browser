namespace MiniBrowser.Core.Services;

/// <summary>
/// Converts raw address-bar input into a navigable HTTP(S) URI.
/// </summary>
public static class UrlNormalizer
{
    public static Uri Normalize(string input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        var trimmed = input.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute) && IsHttp(absolute))
        {
            return absolute;
        }

        var schemeEnd = trimmed.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd > 0)
        {
            // Input carries an explicit, non-HTTP scheme (e.g. ftp://) that we do not support.
            throw new UriFormatException($"Unsupported URI scheme: '{trimmed[..schemeEnd]}'.");
        }

        if (Uri.TryCreate($"https://{trimmed}", UriKind.Absolute, out var https) && IsHttp(https))
        {
            return https;
        }

        throw new UriFormatException($"'{input}' is not a valid web address.");
    }

    private static bool IsHttp(Uri uri)
        => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
}
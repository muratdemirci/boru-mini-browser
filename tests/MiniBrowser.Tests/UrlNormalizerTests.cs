using MiniBrowser.Core.Services;

namespace MiniBrowser.Tests;

public class UrlNormalizerTests
{
    [Theory]
    [InlineData("https://www.muratdemirci.org", "https://www.muratdemirci.org/")]
    [InlineData("http://example.com", "http://example.com/")]
    [InlineData("github.com", "https://github.com/")]
    [InlineData("www.github.com", "https://www.github.com/")]
    [InlineData("  https://github.com  ", "https://github.com/")]
    public void Normalize_ProducesAbsoluteHttpUri(string input, string expected)
    {
        var uri = UrlNormalizer.Normalize(input);

        Assert.Equal(expected, uri.AbsoluteUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_EmptyOrWhitespace_Throws(string input)
    {
        Assert.Throws<ArgumentException>(() => UrlNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("ftp://example.com")]
    [InlineData("ht!tp://broken")]
    public void Normalize_Invalid_ThrowsUriFormatException(string input)
    {
        Assert.Throws<UriFormatException>(() => UrlNormalizer.Normalize(input));
    }
}
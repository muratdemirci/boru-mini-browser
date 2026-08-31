using MiniBrowser.Services.Downloads;

namespace MiniBrowser.Tests;

/// <summary>
/// Phase 6: safe file-name and path handling for downloads.
/// </summary>
public sealed class DownloadPathHelperTests
{
    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData("file with spaces.txt", "file with spaces.txt")]
    [InlineData("../../malicious.exe", "malicious.exe")]
    [InlineData(@"C:\Users\Evil\payload.zip", "payload.zip")]
    [InlineData("a<b>c:d\"e/f\\g|h?i*j", "g_h_i_j")]
    [InlineData("..\\..\\evil.exe", "evil.exe")]
    [InlineData(null, "download")]
    [InlineData("", "download")]
    [InlineData("   ", "download")]
    [InlineData("trailing.....", "trailing")]
    [InlineData("dot. space . ", "dot. space")]
    [InlineData("CON.txt", "_CON.txt")]
    [InlineData("nul", "_nul")]
    [InlineData("COM1.dat", "_COM1.dat")]
    public void SanitizeFileName_ProducesSafeNames(string? input, string expected)
    {
        Assert.Equal(expected, DownloadPathHelper.SanitizeFileName(input));
    }

    [Theory]
    [InlineData("../../malicious.exe")]
    [InlineData(@"..\..\malicious.exe")]
    [InlineData("C:\\Windows\\System32\\evil.exe")]
    [InlineData("..\\")]
    [InlineData("...")]
    public void SanitizeFileName_NeverContainsDirectorySeparators(string input)
    {
        var result = DownloadPathHelper.SanitizeFileName(input);
        Assert.DoesNotContain(Path.DirectorySeparatorChar, result);
        Assert.DoesNotContain(Path.AltDirectorySeparatorChar, result);
    }

    [Fact]
    public void GenerateUniquePath_FirstCandidate_IsCombinedPath()
    {
        var result = DownloadPathHelper.GenerateUniquePath(@"C:\dl", "file.pdf", _ => false);
        Assert.Equal(@"C:\dl\file.pdf", result);
    }

    [Fact]
    public void GenerateUniquePath_DeduplicatesExistingFile()
    {
        var exists = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\dl\file.pdf" };
        var result = DownloadPathHelper.GenerateUniquePath(@"C:\dl", "file.pdf", exists.Contains);
        Assert.Equal(@"C:\dl\file (1).pdf", result);
    }

    [Fact]
    public void GenerateUniquePath_DeduplicatesUntilFree()
    {
        var exists = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\dl\file.pdf", @"C:\dl\file (1).pdf", @"C:\dl\file (2).pdf",
        };
        var result = DownloadPathHelper.GenerateUniquePath(@"C:\dl", "file.pdf", exists.Contains);
        Assert.Equal(@"C:\dl\file (3).pdf", result);
    }

    [Fact]
    public void GenerateUniquePath_NoExtension_DeduplicatesWithSuffix()
    {
        var exists = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\dl\readme" };
        var result = DownloadPathHelper.GenerateUniquePath(@"C:\dl", "readme", exists.Contains);
        Assert.Equal(@"C:\dl\readme (1)", result);
    }

    [Theory]
    [InlineData(@"C:\Users\Me\Downloads", @"C:\Users\Me\Downloads\file.pdf", true)]
    [InlineData(@"C:\Users\Me\Downloads", @"C:\Users\Me\Downloads\..\Downloads\file.pdf", true)]
    [InlineData(@"C:\Users\Me\Downloads", @"C:\Users\Me\Downloads\sub\file.pdf", true)]
    [InlineData(@"C:\Users\Me\Downloads", @"C:\Users\Me\Downloads2\file.pdf", false)]
    [InlineData(@"C:\Users\Me\Downloads", @"C:\Users\Me\Downloads\..\file.pdf", false)]
    [InlineData(@"C:\Users\Me\Downloads", @"C:\file.pdf", false)]
    [InlineData(@"C:\Users\Me\Downloads", "", false)]
    public void IsPathWithinDirectory_BlocksEscapes(string directory, string candidate, bool expected)
    {
        Assert.Equal(expected, DownloadPathHelper.IsPathWithinDirectory(directory, candidate));
    }
}
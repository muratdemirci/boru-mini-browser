using MiniBrowser.Core.Browser;
using MiniBrowser.Core.Models;
using MiniBrowser.Services.Downloads;

namespace MiniBrowser.Tests;

/// <summary>
/// Phase 6: <see cref="DownloadService"/> behavior — path sanitisation, state
/// transitions, cancellation and collection management.
/// </summary>
public sealed class DownloadServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DownloadService _service;

    public DownloadServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MiniBrowserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _service = new DownloadService(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (Exception)
        {
        }
    }

    private (DownloadStartingEventArgs Args, FakeDownloadOperation Op) StartDownload(
        string? uri = "https://example.com/file.pdf",
        string? suggested = @"C:\Users\Me\Downloads\file.pdf")
    {
        var op = new FakeDownloadOperation { Uri = uri is null ? null : new Uri(uri) };
        var args = new DownloadStartingEventArgs(uri, "application/pdf", suggested, op);
        _service.HandleDownloadStarting(args);
        return (args, op);
    }

    [Fact]
    public void HandleDownloadStarting_SetsHandledResultPathAndRegistersItem()
    {
        var (args, _) = StartDownload();

        Assert.True(args.Handled);
        Assert.False(args.Cancel);
        Assert.NotNull(args.ResultFilePath);

        var item = Assert.Single(_service.Downloads);
        Assert.Equal(DownloadStatus.Downloading, item.Status);
        Assert.Equal("https://example.com/file.pdf", item.Url);
        Assert.Equal("file.pdf", item.FileName);
    }

    [Fact]
    public void HandleDownloadStarting_SanitizesTraversalAndStaysInDirectory()
    {
        var (args, _) = StartDownload(
            suggested: @"..\..\evil.exe");

        Assert.True(DownloadPathHelper.IsPathWithinDirectory(_tempDir, args.ResultFilePath!));
        Assert.EndsWith("evil.exe", args.ResultFilePath);
        Assert.True(DownloadPathHelper.IsPathWithinDirectory(_tempDir, _service.Downloads[0].FilePath));
    }

    [Fact]
    public void HandleDownloadStarting_DeduplicatesCollisionsAgainstDisk()
    {
        var (args, _) = StartDownload();
        var first = args.ResultFilePath!;
        File.WriteAllText(first, "x");

        var (second, _) = StartDownload();

        Assert.NotEqual(first, second.ResultFilePath);
        Assert.StartsWith("file (1).pdf", Path.GetFileName(second.ResultFilePath!));
    }

    [Fact]
    public void ProgressEvents_UpdateItemBytes()
    {
        var (_, op) = StartDownload();
        var item = _service.Downloads[0];

        op.TotalBytesToReceive = 1000;
        op.BytesReceived = 250;

        Assert.Equal(250, item.DownloadedBytes);
        Assert.Equal(1000, item.TotalBytes);
        Assert.Equal(25, item.Progress);
    }

    [Fact]
    public void CompletedState_MarksItemCompleted()
    {
        var (_, op) = StartDownload();
        var item = _service.Downloads[0];

        op.BytesReceived = 1000;
        op.State = DownloadOperationState.Completed;

        Assert.Equal(DownloadStatus.Completed, item.Status);
        Assert.NotNull(item.CompletedAt);
    }

    [Fact]
    public void InterruptedState_MarksItemFailed()
    {
        var (_, op) = StartDownload();
        var item = _service.Downloads[0];

        op.State = DownloadOperationState.Interrupted;

        Assert.Equal(DownloadStatus.Failed, item.Status);
        Assert.True(item.HasFailed);
    }

    [Fact]
    public void CancelDownload_StopsActiveDownloadAndCancelsOperation()
    {
        var (_, op) = StartDownload();
        var id = _service.Downloads[0].Id;

        _service.CancelDownload(id);

        Assert.Equal(DownloadStatus.Cancelled, _service.Downloads[0].Status);
        Assert.Equal(1, op.CancelCalls);
    }

    [Fact]
    public void CancelDownload_DoesNothingForCompletedDownload()
    {
        var (_, op) = StartDownload();
        var id = _service.Downloads[0].Id;
        op.State = DownloadOperationState.Completed;

        _service.CancelDownload(id);

        Assert.Equal(DownloadStatus.Completed, _service.Downloads[0].Status);
        Assert.Equal(0, op.CancelCalls);
    }

    [Fact]
    public void RemoveDownload_RemovesItemAndDisposesOperation()
    {
        var (_, op) = StartDownload();
        var id = _service.Downloads[0].Id;

        _service.RemoveDownload(id);

        Assert.Empty(_service.Downloads);
        Assert.True(op.IsDisposed);
    }

    [Fact]
    public void ClearCompletedDownloads_RemovesOnlyTerminalItems()
    {
        var (_, op1) = StartDownload();
        var (_, op2) = StartDownload();
        op2.State = DownloadOperationState.Completed;
        var (_, op3) = StartDownload();
        op3.State = DownloadOperationState.Interrupted;
        var (_, op4) = StartDownload();

        var active = _service.Downloads.Where(d => d.Status is DownloadStatus.Pending or DownloadStatus.Downloading).ToList();

        _service.ClearCompletedDownloads();

        Assert.Equal(active.Count, _service.Downloads.Count);
        Assert.All(_service.Downloads, d => Assert.True(d.Status is DownloadStatus.Pending or DownloadStatus.Downloading));
    }
}
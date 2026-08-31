using MiniBrowser.Core.Browser;
using MiniBrowser.Services.Downloads;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Tests;

/// <summary>
/// Phase 6: <see cref="DownloadsViewModel"/> mirrors the service collection and
/// routes per-item commands.
/// </summary>
public sealed class DownloadsViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDownloadService _service;
    private readonly DownloadsViewModel _viewModel;
    private readonly List<string> _openedUrls = [];

    public DownloadsViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MiniBrowserTests", Guid.NewGuid().ToString("N"));
        _service = new DownloadService(_tempDir);
        _viewModel = new DownloadsViewModel(_service, url => _openedUrls.Add(url));
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

    private void StartDownload(string? uri = "https://example.com/file.pdf")
    {
        var op = new FakeDownloadOperation { Uri = uri is null ? null : new Uri(uri) };
        _service.HandleDownloadStarting(new DownloadStartingEventArgs(uri, null, @"C:\dl\file.pdf", op));
    }

    [Fact]
    public void NewDownload_AppearsInViewModel()
    {
        StartDownload();

        var item = Assert.Single(_viewModel.Items);
        Assert.Equal("file.pdf", item.FileName);
        Assert.Equal("Downloading", item.StatusText);
        Assert.True(item.CanCancel);
        Assert.False(item.CanOpen);
    }

    [Fact]
    public void CompletedDownload_ReflectsStateAndEnablesOpen()
    {
        var op = new FakeDownloadOperation { Uri = new Uri("https://example.com/file.pdf") };
        _service.HandleDownloadStarting(new DownloadStartingEventArgs("https://example.com/file.pdf", null, @"C:\dl\file.pdf", op));
        op.BytesReceived = 100;
        op.State = DownloadOperationState.Completed;

        var item = Assert.Single(_viewModel.Items);
        Assert.Equal("Completed", item.StatusText);
        Assert.True(item.CanOpen);
        Assert.False(item.CanCancel);
    }

    [Fact]
    public void Remove_RemovesFromServiceAndViewModel()
    {
        StartDownload();
        var id = _viewModel.Items[0].Id;

        _viewModel.RemoveCommand.Execute(_viewModel.Items[0]);

        Assert.Empty(_viewModel.Items);
        Assert.Empty(_service.Downloads);
        Assert.True(id != Guid.Empty);
    }

    [Fact]
    public void Retry_RemovesItemAndOpensUrl()
    {
        var op = new FakeDownloadOperation { Uri = new Uri("https://example.com/file.pdf") };
        _service.HandleDownloadStarting(new DownloadStartingEventArgs("https://example.com/file.pdf", null, @"C:\dl\file.pdf", op));
        op.State = DownloadOperationState.Interrupted;

        var item = Assert.Single(_viewModel.Items);
        Assert.True(item.HasFailed);

        _viewModel.RetryCommand.Execute(item);

        Assert.Empty(_viewModel.Items);
        Assert.Equal("https://example.com/file.pdf", Assert.Single(_openedUrls));
    }

    [Fact]
    public void Cancel_RoutesToService()
    {
        StartDownload();
        var item = _viewModel.Items[0];

        _viewModel.CancelCommand.Execute(item);

        Assert.Equal(Core.Models.DownloadStatus.Cancelled, _service.Downloads[0].Status);
    }

    [Fact]
    public void ClearCompleted_RemovesCompletedOnly()
    {
        StartDownload();

        var op = new FakeDownloadOperation { Uri = new Uri("https://example.com/other.pdf") };
        _service.HandleDownloadStarting(new DownloadStartingEventArgs("https://example.com/other.pdf", null, @"C:\dl\other.pdf", op));
        op.State = DownloadOperationState.Completed;

        Assert.Equal(2, _viewModel.Items.Count);

        _viewModel.ClearCompletedCommand.Execute(null);

        var item = Assert.Single(_viewModel.Items);
        Assert.Equal("file.pdf", item.FileName);
    }

    [Fact]
    public void EmptyService_ShowsStatusMessage()
    {
        Assert.Equal("No downloads yet.", _viewModel.StatusMessage);
    }
}
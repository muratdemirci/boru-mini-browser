using MiniBrowser.ViewModels;

namespace MiniBrowser.Tests;

public class MainWindowTabManagementTests
{
    private readonly FakeBrowserEngineFactory _factory;
    private readonly MainWindowViewModel _viewModel;

    public MainWindowTabManagementTests()
    {
        _factory = new FakeBrowserEngineFactory();
        _viewModel = new MainWindowViewModel(
            _factory,
            new FakeHistoryService(),
            new FakeBookmarkService(),
            new FakeDownloadService(),
            new FakeSettingsService());
    }

    [Fact]
    public void NewTab_AddsTabAndMakesItActive()
    {
        _viewModel.NewTabCommand.Execute(null);

        Assert.Single(_viewModel.Tabs);
        Assert.NotNull(_viewModel.ActiveTab);
        Assert.True(_viewModel.ActiveTab.IsActive);
        Assert.Same(_viewModel.ActiveTab, _viewModel.Tabs[0]);
    }

    [Fact]
    public void NewTab_CreatesIndependentEngines()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        Assert.Equal(2, _factory.Created.Count);
        Assert.NotSame(_factory.Created[0], _factory.Created[1]);
        Assert.Same(_viewModel.Tabs[0].Engine, _factory.Created[0]);
        Assert.Same(_viewModel.Tabs[1].Engine, _factory.Created[1]);
    }

    [Fact]
    public void NewTab_DoesNotDeactivatePreviousActiveTabImmediately()
    {
        _viewModel.NewTabCommand.Execute(null);
        var first = _viewModel.ActiveTab!;

        _viewModel.NewTabCommand.Execute(null);

        Assert.False(first.IsActive);
        Assert.True(_viewModel.ActiveTab!.IsActive);
    }

    [Fact]
    public void SelectTab_SwitchesActiveTab()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        var middle = _viewModel.Tabs[1];
        _viewModel.SelectTabCommand.Execute(middle);

        Assert.Same(middle, _viewModel.ActiveTab);
        Assert.True(middle.IsActive);
        Assert.False(_viewModel.Tabs[0].IsActive);
    }

    [Fact]
    public void CloseTab_NonActive_KeepsActiveTab()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        var active = _viewModel.ActiveTab!;
        var other = _viewModel.Tabs[0];

        _viewModel.CloseTabCommand.Execute(other);

        Assert.Single(_viewModel.Tabs);
        Assert.Same(active, _viewModel.ActiveTab);
        Assert.True(other.Engine is FakeBrowserEngine { IsDisposed: true });
    }

    [Fact]
    public void CloseTab_Active_ActivatesRightNeighbor()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[0]);
        _viewModel.CloseTabCommand.Execute(_viewModel.Tabs[0]);

        Assert.Equal(2, _viewModel.Tabs.Count);
        Assert.Same(_viewModel.Tabs[0], _viewModel.ActiveTab);
    }

    [Fact]
    public void CloseTab_ActiveLast_ActivatesLeftNeighbor()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        _viewModel.CloseTabCommand.Execute(_viewModel.Tabs[2]);

        Assert.Equal(2, _viewModel.Tabs.Count);
        Assert.Same(_viewModel.Tabs[1], _viewModel.ActiveTab);
    }

    [Fact]
    public void CloseTab_LastRemainingTab_CreatesNewHomeTab()
    {
        _viewModel.NewTabCommand.Execute(null);
        var only = _viewModel.ActiveTab!;

        _viewModel.CloseTabCommand.Execute(only);

        Assert.Single(_viewModel.Tabs);
        Assert.NotSame(only, _viewModel.Tabs[0]);
        Assert.NotNull(_viewModel.ActiveTab);
        Assert.Equal("https://www.muratdemirci.org/", _viewModel.Tabs[0].Url);
        Assert.True(only.Engine is FakeBrowserEngine { IsDisposed: true });
    }

    [Fact]
    public void CloseTab_NullParameter_ClosesActiveTab()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        var active = _viewModel.ActiveTab!;

        _viewModel.CloseTabCommand.Execute(null);

        Assert.DoesNotContain(active, _viewModel.Tabs);
    }

    [Fact]
    public void NextTab_WrapsAround()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        var first = _viewModel.Tabs[0];
        var second = _viewModel.Tabs[1];

        _viewModel.NextTabCommand.Execute(null);
        Assert.Same(first, _viewModel.ActiveTab);

        _viewModel.NextTabCommand.Execute(null);
        Assert.Same(second, _viewModel.ActiveTab);
    }

    [Fact]
    public void PreviousTab_WrapsAround()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        var last = _viewModel.Tabs[2];

        _viewModel.PreviousTabCommand.Execute(null);
        Assert.Same(_viewModel.Tabs[1], _viewModel.ActiveTab);

        _viewModel.PreviousTabCommand.Execute(null);
        Assert.Same(_viewModel.Tabs[0], _viewModel.ActiveTab);

        _viewModel.PreviousTabCommand.Execute(null);
        Assert.Same(last, _viewModel.ActiveTab);
    }

    [Fact]
    public void NextTab_WithSingleTab_DoesNothing()
    {
        _viewModel.NewTabCommand.Execute(null);
        var active = _viewModel.ActiveTab!;

        _viewModel.NextTabCommand.Execute(null);
        Assert.Same(active, _viewModel.ActiveTab);
    }

    [Fact]
    public void SelectTabByIndex_SelectsRequestedTab()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        _viewModel.SelectTabByIndexCommand.Execute(1);
        Assert.Same(_viewModel.Tabs[1], _viewModel.ActiveTab);

        _viewModel.SelectTabByIndexCommand.Execute(0);
        Assert.Same(_viewModel.Tabs[0], _viewModel.ActiveTab);
    }

    [Fact]
    public void SelectTabByIndex_OutOfRange_DoesNothing()
    {
        _viewModel.NewTabCommand.Execute(null);
        var active = _viewModel.ActiveTab!;

        _viewModel.SelectTabByIndexCommand.Execute(5);
        Assert.Same(active, _viewModel.ActiveTab);

        _viewModel.SelectTabByIndexCommand.Execute(-1);
        Assert.Same(active, _viewModel.ActiveTab);
    }

    [Fact]
    public void SwitchTabs_SyncsAddressText()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[0]);
        ((FakeBrowserEngine)_viewModel.Tabs[0].Engine).CurrentUrl = "https://first.example/";
        _viewModel.SelectTabCommand.Execute(_viewModel.Tabs[1]);
        ((FakeBrowserEngine)_viewModel.Tabs[1].Engine).CurrentUrl = "https://second.example/";

        Assert.Equal("https://second.example/", _viewModel.AddressText);
    }

    [Fact]
    public void Dispose_DisposesAllEngines()
    {
        _viewModel.NewTabCommand.Execute(null);
        _viewModel.NewTabCommand.Execute(null);

        _viewModel.Dispose();

        Assert.All(_factory.Created, e => Assert.True(e.IsDisposed));
    }
}
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Views;

public partial class MainWindow : Window
{
    private DownloadsWindow? _downloadsWindow;
    private SettingsWindow? _settingsWindow;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
        Closed += OnClosed;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var viewModel = DataContext as MainWindowViewModel;

        if (e.Key == Key.F12)
        {
            viewModel?.OpenDevToolsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.I && e.KeyModifiers.HasFlag(KeyModifiers.Control)
            && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            viewModel?.OpenDevToolsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.L && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            FocusAddressBar();
            e.Handled = true;
            return;
        }

        if (viewModel is not null)
        {
            if (e.Key == Key.F && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                viewModel.OpenFindCommand.Execute(null);
                FindBox.Focus();
                FindBox.SelectAll();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F3)
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    viewModel.FindPreviousCommand.Execute(null);
                }
                else
                {
                    viewModel.FindNextCommand.Execute(null);
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.OemPlus && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                viewModel.ZoomInCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.OemMinus && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                viewModel.ZoomOutCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.D0 && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                viewModel.ResetZoomCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.T && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                viewModel.NewTabCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.W && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                viewModel.CloseTabCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    viewModel.PreviousTabCommand.Execute(null);
                }
                else
                {
                    viewModel.NextTabCommand.Execute(null);
                }

                e.Handled = true;
                return;
            }

            if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                viewModel.SelectTabByIndexCommand.Execute((int)(e.Key - Key.D1));
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Left && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                viewModel.GoBackCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Right && e.KeyModifiers.HasFlag(KeyModifiers.Alt))
            {
                viewModel.GoForwardCommand.Execute(null);
                e.Handled = true;
                return;
            }

if (e.Key == Key.F5 || (e.Key == Key.R && e.KeyModifiers.HasFlag(KeyModifiers.Control)))
            {
                viewModel.ReloadCommand.Execute(null);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F3)
            {
                if (viewModel is not null && viewModel.IsFindBarVisible)
                {
                    if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        viewModel.FindPreviousCommand.Execute(null);
                    }
                    else
                    {
                        viewModel.FindNextCommand.Execute(null);
                    }

                    e.Handled = true;
                    return;
                }

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                if (viewModel.IsFindBarVisible)
                {
                    viewModel.CloseFindCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                viewModel.StopCommand.Execute(null);
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    private void FocusAddressBar()
    {
        AddressBox.Focus();
        AddressBox.SelectAll();
    }

    private void OnAddressGotFocus(object? sender, FocusChangedEventArgs e)
    {
        AddressBox.SelectAll();
    }

    private void OnAddressKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnOpenHistory(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var window = new HistoryWindow { DataContext = viewModel.History };
        window.ShowDialog(this);
    }

    private void OnOpenBookmarks(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var window = new BookmarksWindow { DataContext = viewModel.Bookmarks };
        window.ShowDialog(this);
    }

    private void OnOpenDownloads(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (_downloadsWindow is { IsVisible: true } existing)
        {
            existing.Activate();
            return;
        }

        var window = new DownloadsWindow { DataContext = viewModel.Downloads };
        _downloadsWindow = window;
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_downloadsWindow, window))
            {
                _downloadsWindow = null;
            }
        };
        window.Show(this);
    }

    private void OnOpenSettings(object? sender, RoutedEventArgs e)
    {
        if (_settingsWindow is { IsVisible: true } existing)
        {
            existing.Activate();
            return;
        }

        var viewModel = App.Services.GetRequiredService<SettingsViewModel>();
        var window = new SettingsWindow { DataContext = viewModel };
        _settingsWindow = window;

        viewModel.Saved += (_, _) => window.Close();
        viewModel.Cancelled += (_, _) => window.Close();
        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_settingsWindow, window))
            {
                _settingsWindow = null;
            }
        };

        window.Show(this);
    }

    private void OnCloseWindow(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
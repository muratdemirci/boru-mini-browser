using Avalonia.Controls;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Views;

public partial class HistoryWindow : Window
{
    public HistoryWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is HistoryViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}
using Avalonia.Controls;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Views;

public partial class BookmarksWindow : Window
{
    public BookmarksWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (DataContext is BookmarksViewModel viewModel)
        {
            await viewModel.RefreshAsync();
        }
    }
}
using Avalonia.Controls;
using MiniBrowser.Infrastructure.WebView;
using MiniBrowser.ViewModels;

namespace MiniBrowser.Controls;

public partial class BrowserView : UserControl
{
    public BrowserView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is TabViewModel tab && tab.Engine is NativeWebViewEngine nativeEngine)
        {
            nativeEngine.Attach(WebViewControl);
            tab.OnWebViewAttached();
        }
    }
}
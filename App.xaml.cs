using BitacoraGrabacion.Services;
using Microsoft.UI.Xaml;

namespace BitacoraGrabacion;

public partial class App : Application
{
    public static DataService Data { get; } = new();

    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    public static Window? MainAppWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainAppWindow = _window;
        _window.Activate();
    }
}

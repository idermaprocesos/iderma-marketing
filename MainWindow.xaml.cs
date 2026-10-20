using BitacoraGrabacion.Pages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace BitacoraGrabacion;

public sealed partial class MainWindow : Window
{
    private bool _ajustandoMenu;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        Iderma.Shell.BotonLauncher.Colocar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.SetIcon("img/logorecord.ico");
        AppWindow.Resize(new SizeInt32(1280, 840));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Maximize();
        }
    }

    public void NavigateTo(Type pageType, object? parameter = null)
    {
        NavFrame.Navigate(pageType, parameter);
        AjustarMenu(pageType, parameter);
    }

    private void AjustarMenu(Type pageType, object? parameter)
    {
        _ajustandoMenu = true;
        try
        {
            if (pageType == typeof(HomePage))
            {
                SelectMenu("bitacora");
            }
            else if (pageType == typeof(NuevaGrabacionPage) && parameter is not Guid)
            {
                SelectMenu("nueva");
            }
            else if (pageType == typeof(ReportesPage))
            {
                SelectMenu("reportes");
            }
            else if (pageType == typeof(SettingsPage))
            {
                NavView.SelectedItem = NavView.SettingsItem;
            }
        }
        finally
        {
            _ajustandoMenu = false;
        }
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (NavFrame.CanGoBack)
        {
            NavFrame.GoBack();
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_ajustandoMenu)
        {
            return;
        }

        if (args.IsSettingsSelected)
        {
            NavFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "bitacora":
                    NavFrame.Navigate(typeof(HomePage));
                    break;
                case "nueva":
                    NavFrame.Navigate(typeof(NuevaGrabacionPage));
                    break;
                case "reportes":
                    NavFrame.Navigate(typeof(ReportesPage));
                    break;
                default:
                    throw new InvalidOperationException($"Elemento de navegación desconocido: {item.Tag}");
            }
        }
    }

    private void SelectMenu(string tag)
    {
        foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>())
        {
            if (string.Equals(item.Tag as string, tag, StringComparison.Ordinal))
            {
                item.IsSelected = true;
                break;
            }
        }
    }
}

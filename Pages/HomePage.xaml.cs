using System.Collections.ObjectModel;
using BitacoraGrabacion.Models;
using BitacoraGrabacion.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;

namespace BitacoraGrabacion.Pages;

public sealed partial class HomePage : Page
{
    private readonly ObservableCollection<RegistroGrabacion> _lluvia = [];
    private readonly ObservableCollection<RegistroGrabacion> _grabando = [];
    private readonly ObservableCollection<RegistroGrabacion> _editando = [];
    private readonly ObservableCollection<RegistroGrabacion> _revision = [];
    private readonly ObservableCollection<RegistroGrabacion> _completadas = [];
    private bool _ignorarClick;
    private bool _ignorarPorArrastre;
    private bool _procesandoSoltar;

    public HomePage()
    {
        InitializeComponent();
        ListaLluvia.ItemsSource = _lluvia;
        ListaGrabando.ItemsSource = _grabando;
        ListaEditando.ItemsSource = _editando;
        ListaRevision.ItemsSource = _revision;
        ListaCompletadas.ItemsSource = _completadas;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        CargarRegistros();
    }

    private void CargarRegistros()
    {
        var registros = App.Data.GetRegistros(BusquedaBox.Text)
            .OrderBy(r => r.FechaLimiteActual ?? DateTimeOffset.MaxValue)
            .ThenByDescending(r => r.Fecha)
            .ToList();

        LlenarColumna(_lluvia, ListaLluvia, VacioLluvia, TituloLluvia, "Lluvia de ideas", registros, EstadosProduccion.Lluvia);
        LlenarColumna(_grabando, ListaGrabando, VacioGrabando, TituloGrabando, "Grabando", registros, EstadosProduccion.Grabando);
        LlenarColumna(_editando, ListaEditando, VacioEditando, TituloEditando, "Editando", registros, EstadosProduccion.Editando);
        LlenarColumna(_revision, ListaRevision, VacioRevision, TituloRevision, "En revisión", registros, EstadosProduccion.Revision);

        var listaAbierta = ListaCompletadas.Visibility == Visibility.Visible;
        _completadas.Clear();
        foreach (var registro in registros.Where(r => r.EstadoNormalizado == EstadosProduccion.Completado).OrderByDescending(r => r.Fecha))
        {
            _completadas.Add(registro);
        }

        var hayCompletadas = _completadas.Count > 0;
        var soloCompletadas = hayCompletadas && registros.All(r => r.EstadoNormalizado == EstadosProduccion.Completado);
        TituloCompletadas.Text = $"Completadas ({_completadas.Count})";
        VerCompletadasButton.Visibility = hayCompletadas ? Visibility.Visible : Visibility.Collapsed;
        if (!hayCompletadas)
        {
            ListaCompletadas.Visibility = Visibility.Collapsed;
            VerCompletadasButton.Content = "Ver";
        }
        else if (listaAbierta || (soloCompletadas && !string.IsNullOrWhiteSpace(BusquedaBox.Text)))
        {
            ListaCompletadas.Visibility = Visibility.Visible;
            VerCompletadasButton.Content = "Ocultar";
        }

        var hayRegistros = registros.Count > 0;
        var hayEnBitacora = App.Data.Store.Registros.Count > 0;
        EmptyText.Text = hayEnBitacora
            ? "Ninguna grabación coincide con la búsqueda."
            : "Aún no hay registros. Crea la primera grabación o agrega cargos y nombres en Configuración.";
        EmptyState.Visibility = hayRegistros ? Visibility.Collapsed : Visibility.Visible;
        Tablero.Visibility = hayRegistros ? Visibility.Visible : Visibility.Collapsed;
        ZonaCompletar.Visibility = hayRegistros ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void LlenarColumna(
        ObservableCollection<RegistroGrabacion> destino,
        ListView lista,
        TextBlock vacio,
        TextBlock titulo,
        string nombre,
        IEnumerable<RegistroGrabacion> registros,
        string estado)
    {
        destino.Clear();
        foreach (var registro in registros.Where(r => r.EstadoNormalizado == estado))
        {
            destino.Add(registro);
        }

        titulo.Text = $"{nombre} ({destino.Count})";
        lista.Visibility = Visibility.Visible;
        vacio.Visibility = destino.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Tarjeta_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        _ignorarPorArrastre = true;
        if (sender is not FrameworkElement { Tag: string idTexto } || !Guid.TryParse(idTexto, out _))
        {
            args.Cancel = true;
            return;
        }

        args.Data.SetText(idTexto);
        args.Data.RequestedOperation = DataPackageOperation.Move;
        args.AllowedOperations = DataPackageOperation.Move;
    }

    private async void Tarjeta_DropCompleted(UIElement sender, DropCompletedEventArgs args)
    {
        await Task.Delay(400);
        _ignorarPorArrastre = false;
    }

    private void Columna_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
        e.Handled = true;
        if (sender is not FrameworkElement { Tag: string estado })
        {
            return;
        }

        e.DragUIOverride.IsCaptionVisible = true;
        e.DragUIOverride.IsGlyphVisible = false;
        e.DragUIOverride.Caption = estado == EstadosProduccion.Completado
            ? "Marcar como completada"
            : $"Mover a {EstadosProduccion.Texto(estado)}";
    }

    private async void Columna_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (_procesandoSoltar || sender is not FrameworkElement { Tag: string estado })
        {
            return;
        }

        if (!e.DataView.Contains(StandardDataFormats.Text))
        {
            return;
        }

        _procesandoSoltar = true;
        try
        {
            var idTexto = await e.DataView.GetTextAsync();
            if (Guid.TryParse(idTexto, out var id))
            {
                MoverAEstado(id, estado);
            }
        }
        finally
        {
            _procesandoSoltar = false;
        }
    }

    private void MoverAEstado(Guid id, string estado)
    {
        if (App.Data.GetRegistro(id) is not { } registro)
        {
            return;
        }

        var normalizado = EstadosProduccion.Normalizar(estado);
        if (registro.EstadoNormalizado == normalizado)
        {
            return;
        }

        registro.Estado = normalizado;
        App.Data.GuardarRegistro(registro);
        CargarRegistros();
    }

    private void Completar_Click(object sender, RoutedEventArgs e)
    {
        _ignorarClick = true;
        if (sender is Button button && Guid.TryParse(button.Tag?.ToString(), out var id))
        {
            MoverAEstado(id, EstadosProduccion.Completado);
        }
    }

    private void VerCompletadas_Click(object sender, RoutedEventArgs e)
    {
        var abierta = ListaCompletadas.Visibility == Visibility.Visible;
        ListaCompletadas.Visibility = abierta ? Visibility.Collapsed : Visibility.Visible;
        VerCompletadasButton.Content = abierta ? "Ver" : "Ocultar";
    }

    private void BusquedaBox_TextChanged(object sender, TextChangedEventArgs e) => CargarRegistros();

    private void NuevaGrabacion_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainAppWindow is MainWindow window)
        {
            window.NavigateTo(typeof(NuevaGrabacionPage));
        }
    }

    private void Tarjeta_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (VieneDeBoton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (_ignorarClick || _ignorarPorArrastre)
        {
            _ignorarClick = false;
            _ignorarPorArrastre = false;
            e.Handled = true;
            return;
        }

        if (sender is FrameworkElement elemento &&
            Guid.TryParse(elemento.Tag?.ToString(), out var id) &&
            App.MainAppWindow is MainWindow window)
        {
            e.Handled = true;
            window.NavigateTo(typeof(NuevaGrabacionPage), id);
        }
    }

    private static bool VieneDeBoton(DependencyObject? origen)
    {
        while (origen is not null)
        {
            if (origen is Button or Border { CanDrag: true })
            {
                return true;
            }

            origen = VisualTreeHelper.GetParent(origen);
        }

        return false;
    }

    private async void Exportar_Click(object sender, RoutedEventArgs e)
    {
        _ignorarClick = true;
        if (sender is not Button button ||
            !Guid.TryParse(button.Tag?.ToString(), out var id) ||
            App.Data.GetRegistro(id) is not { } registro ||
            App.MainAppWindow is not MainWindow window)
        {
            return;
        }

        await InformeExport.GuardarAsync(window, $"informe-{registro.Titulo}", ReporteService.CrearHtmlIndividual(registro));
    }

    private async void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        _ignorarClick = true;
        if (sender is not Button button || !Guid.TryParse(button.Tag?.ToString(), out var id))
        {
            return;
        }

        var dialogo = new ContentDialog
        {
            Title = "Eliminar registro",
            Content = "Esta entrada se quitará de la bitácora. Esta acción no se puede deshacer.",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialogo.ShowAsync() == ContentDialogResult.Primary)
        {
            App.Data.EliminarRegistro(id);
            CargarRegistros();
        }
    }
}

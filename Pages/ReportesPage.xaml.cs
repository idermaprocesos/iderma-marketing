using System.Collections.ObjectModel;
using BitacoraGrabacion.Models;
using BitacoraGrabacion.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace BitacoraGrabacion.Pages;

public sealed partial class ReportesPage : Page
{
    private readonly ObservableCollection<RegistroGrabacion> _grabaciones = [];
    private readonly ObservableCollection<CargoReporte> _cargos = [];
    private RangoPeriodo? _rango;
    private bool _listo;
    private bool _ignorarClick;

    public ReportesPage()
    {
        InitializeComponent();
        GrabacionesList.ItemsSource = _grabaciones;
        CargosList.ItemsSource = _cargos;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ReferenciaPicker.Date = DateTimeOffset.Now;
        _listo = true;
        Actualizar();
    }

    private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_listo)
        {
            Actualizar();
        }
    }

    private void ReferenciaPicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (_listo)
        {
            Actualizar();
        }
    }

    private void Actualizar()
    {
        var referencia = ReferenciaPicker.Date ?? DateTimeOffset.Now;
        _rango = ReporteService.CalcularPeriodo(TipoSeleccionado(), referencia);
        RangoBlock.Text = _rango.Etiqueta;

        var registros = ReporteService.Filtrar(App.Data.GetRegistros(), _rango);
        TotalBlock.Text = registros.Count.ToString();

        _grabaciones.Clear();
        foreach (var registro in registros)
        {
            _grabaciones.Add(registro);
        }

        var cargos = ReporteService.PorCargo(registros);
        _cargos.Clear();
        foreach (var cargo in cargos)
        {
            _cargos.Add(cargo);
        }

        GrabacionesList.Visibility = _grabaciones.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        GrabacionesVacio.Visibility = _grabaciones.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CargosList.Visibility = _cargos.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        CargosVacio.Visibility = _cargos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void GrabacionesList_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (_ignorarClick)
        {
            _ignorarClick = false;
            return;
        }

        if (e.ClickedItem is RegistroGrabacion registro && App.MainAppWindow is MainWindow window)
        {
            window.NavigateTo(typeof(NuevaGrabacionPage), registro.Id);
        }
    }

    private async void Exportar_Click(object sender, RoutedEventArgs e)
    {
        if (_rango is null)
        {
            Actualizar();
        }

        if (App.MainAppWindow is not MainWindow window || _rango is null)
        {
            return;
        }

        var html = ReporteService.CrearHtml(_rango, [.. _grabaciones], [.. _cargos]);
        if (await InformeExport.GuardarAsync(window, $"reporte-bitacora-{DateTime.Now:yyyy-MM-dd}", html))
        {
            AvisoBar.Severity = InfoBarSeverity.Success;
            AvisoBar.Message = "Se exportó el informe con el logo de Iderma, las grabaciones y el desglose por cargo.";
            AvisoBar.IsOpen = true;
        }
    }

    private async void ExportarIndividual_Click(object sender, RoutedEventArgs e)
    {
        _ignorarClick = true;
        if (sender is not Button button ||
            !Guid.TryParse(button.Tag?.ToString(), out var id) ||
            App.Data.GetRegistro(id) is not { } registro ||
            App.MainAppWindow is not MainWindow window)
        {
            return;
        }

        if (await InformeExport.GuardarAsync(window, $"informe-{registro.Titulo}", ReporteService.CrearHtmlIndividual(registro)))
        {
            AvisoBar.Severity = InfoBarSeverity.Success;
            AvisoBar.Message = "Se exportó el informe de esa grabación.";
            AvisoBar.IsOpen = true;
        }
    }

    private TipoPeriodo TipoSeleccionado()
    {
        if (PeriodoCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag switch
            {
                "quincena" => TipoPeriodo.Quincena,
                "tres" => TipoPeriodo.TresSemanas,
                "mes" => TipoPeriodo.Mes,
                _ => TipoPeriodo.Semana
            };
        }

        return TipoPeriodo.Semana;
    }
}

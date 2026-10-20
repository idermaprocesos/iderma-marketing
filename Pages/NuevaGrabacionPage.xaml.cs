using System.Collections.ObjectModel;
using BitacoraGrabacion.Models;
using BitacoraGrabacion.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace BitacoraGrabacion.Pages;

public sealed partial class NuevaGrabacionPage : Page
{
    private readonly ObservableCollection<SelectableItem> _equipos = [];
    private readonly ObservableCollection<ParticipanteFila> _participantes = [];
    private Guid? _registroId;
    private IReadOnlyList<CatalogItem> _cargos = [];
    private IReadOnlyList<Persona> _nombres = [];

    public NuevaGrabacionPage()
    {
        InitializeComponent();
        EquiposGrid.ItemsSource = _equipos;
        ParticipantesList.ItemsSource = _participantes;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        CargarCatalogos();

        if (e.Parameter is Guid id && App.Data.GetRegistro(id) is { } registro)
        {
            _registroId = id;
            TituloPagina.Text = registro.Titulo;
            SubtituloPagina.Text = registro.EstadoNormalizado == EstadosProduccion.Completado
                ? "Esta grabación está completada y no aparece en el tablero. Elige otra etapa para devolverla."
                : $"Etapa actual: {registro.EstadoTexto}. Cambia el estado, las fechas límite o márcala como completada para sacarla del tablero.";
            ExportarButton.Visibility = Visibility.Visible;
            CargarRegistro(registro);
        }
        else
        {
            _registroId = null;
            TituloPagina.Text = "Nueva grabación";
            SubtituloPagina.Text = "Elige la etapa de la producción, pon las fechas límite y registra a las personas de la sesión.";
            ExportarButton.Visibility = Visibility.Collapsed;
            FechaPicker.Date = DateTimeOffset.Now;
            HoraPicker.Time = DateTimeOffset.Now.TimeOfDay;
            EstadoLluvia.IsChecked = true;
            EstadoCompletado.IsChecked = false;
            LimiteLluviaPicker.Date = null;
            LimiteGrabandoPicker.Date = null;
            LimiteEditandoPicker.Date = null;
            LimiteRevisionPicker.Date = null;
            _participantes.Clear();
            AgregarFila();
        }
    }

    private void Estado_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string clave })
        {
            return;
        }

        ResaltarEtapa(clave);
    }

    private void ResaltarEtapa(string clave)
    {
        Marcar(TarjetaLluvia, clave == EstadosProduccion.Lluvia);
        Marcar(TarjetaGrabando, clave == EstadosProduccion.Grabando);
        Marcar(TarjetaEditando, clave == EstadosProduccion.Editando);
        Marcar(TarjetaRevision, clave == EstadosProduccion.Revision);
        Marcar(TarjetaCompletado, clave == EstadosProduccion.Completado);
    }

    private static void Marcar(Border tarjeta, bool activa)
    {
        tarjeta.BorderBrush = (Brush)Application.Current.Resources[
            activa ? "AccentFillColorDefaultBrush" : "CardStrokeColorDefaultBrush"];
        tarjeta.BorderThickness = new Thickness(activa ? 2 : 1);
    }

    private string EstadoSeleccionado()
    {
        if (EstadoCompletado.IsChecked == true)
        {
            return EstadosProduccion.Completado;
        }

        if (EstadoGrabando.IsChecked == true)
        {
            return EstadosProduccion.Grabando;
        }

        if (EstadoEditando.IsChecked == true)
        {
            return EstadosProduccion.Editando;
        }

        if (EstadoRevision.IsChecked == true)
        {
            return EstadosProduccion.Revision;
        }

        return EstadosProduccion.Lluvia;
    }

    private void CargarCatalogos()
    {
        _cargos = App.Data.Store.Roles.OrderBy(r => r.Nombre).ToList();
        _nombres = App.Data.Store.Personas.OrderBy(p => p.Nombre).ToList();
        CamaraCombo.ItemsSource = App.Data.Store.Camaras.OrderBy(c => c.Nombre).ToList();

        _equipos.Clear();
        foreach (var equipo in App.Data.Store.Equipos.OrderBy(e => e.Nombre))
        {
            _equipos.Add(new SelectableItem { Item = equipo });
        }
    }

    private void CargarRegistro(RegistroGrabacion registro)
    {
        TituloBox.Text = registro.Titulo;
        SeleccionarEstado(registro.EstadoNormalizado);
        LimiteLluviaPicker.Date = registro.LimiteLluvia;
        LimiteGrabandoPicker.Date = registro.LimiteGrabando;
        LimiteEditandoPicker.Date = registro.LimiteEditando;
        LimiteRevisionPicker.Date = registro.LimiteRevision;
        FechaPicker.Date = registro.Fecha;
        HoraPicker.Time = registro.Fecha.TimeOfDay;
        DuracionBox.Text = registro.Duracion ?? string.Empty;
        NotasBox.Text = registro.Notas ?? string.Empty;
        CamaraCombo.SelectedItem = App.Data.Store.Camaras.FirstOrDefault(c => c.Id == registro.CamaraId);

        registro.AsegurarParticipantes();
        _participantes.Clear();
        foreach (var participante in registro.Participantes)
        {
            AgregarFila(participante);
        }

        if (_participantes.Count == 0)
        {
            AgregarFila();
        }

        foreach (var equipo in _equipos)
        {
            equipo.IsSelected = registro.EquipoIds.Contains(equipo.Item.Id) ||
                                registro.Equipos.Contains(equipo.Nombre, StringComparer.CurrentCultureIgnoreCase);
        }

        EquiposGrid.ItemsSource = null;
        EquiposGrid.ItemsSource = _equipos;
    }

    private void AgregarParticipante_Click(object sender, RoutedEventArgs e) => AgregarFila();

    private void QuitarParticipante_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !Guid.TryParse(button.Tag?.ToString(), out var id))
        {
            return;
        }

        var fila = _participantes.FirstOrDefault(p => p.Id == id);
        if (fila is not null)
        {
            _participantes.Remove(fila);
        }

        if (_participantes.Count == 0)
        {
            AgregarFila();
        }
    }

    private void AgregarFila(ParticipanteGrabacion? participante = null)
    {
        var fila = new ParticipanteFila
        {
            Cargos = _cargos,
            Nombres = _nombres
        };

        if (participante is not null)
        {
            fila.CargoSeleccionado =
                _cargos.FirstOrDefault(c => c.Id == participante.CargoId) ??
                _cargos.FirstOrDefault(c => string.Equals(c.Nombre, participante.Cargo, StringComparison.CurrentCultureIgnoreCase));
            if (fila.EsInvitado || ParticipanteFila.EsCargoDeInvitado(participante.Cargo))
            {
                fila.NombreInvitado = participante.Nombre;
            }
            else
            {
                fila.PersonaSeleccionada =
                    _nombres.FirstOrDefault(p => p.Id == participante.PersonaId) ??
                    _nombres.FirstOrDefault(p => string.Equals(p.Nombre, participante.Nombre, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        _participantes.Add(fila);
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TituloBox.Text))
        {
            MostrarAviso("Escribe el título o nombre del contenido.", InfoBarSeverity.Warning);
            return;
        }

        var participantes = _participantes
            .Where(f => f.PersonaSeleccionada is not null ||
                        f.CargoSeleccionado is not null ||
                        !string.IsNullOrWhiteSpace(f.NombreInvitado))
            .Select(f =>
            {
                var nombre = f.EsInvitado
                    ? f.NombreInvitado.Trim()
                    : f.PersonaSeleccionada?.Nombre ?? string.Empty;
                return new ParticipanteGrabacion
                {
                    PersonaId = f.EsInvitado ? null : f.PersonaSeleccionada?.Id,
                    Nombre = nombre,
                    CargoId = f.CargoSeleccionado?.Id,
                    Cargo = f.CargoSeleccionado?.Nombre ?? string.Empty
                };
            })
            .Where(p => !string.IsNullOrWhiteSpace(p.Nombre) || !string.IsNullOrWhiteSpace(p.Cargo))
            .ToList();

        if (participantes.Count == 0)
        {
            MostrarAviso("Agrega al menos una persona con cargo y nombre. Si falta alguien, agrégalo en Configuración.", InfoBarSeverity.Warning);
            return;
        }

        var fecha = FechaPicker.Date;
        var hora = HoraPicker.Time;
        var fechaHora = new DateTimeOffset(fecha.Year, fecha.Month, fecha.Day, hora.Hours, hora.Minutes, hora.Seconds, fecha.Offset);
        var camara = CamaraCombo.SelectedItem as CatalogItem;
        var equipos = _equipos.Where(x => x.IsSelected).ToList();
        var primero = participantes[0];
        var segundo = participantes.Count > 1 ? participantes[1] : null;

        var registro = new RegistroGrabacion
        {
            Id = _registroId ?? Guid.NewGuid(),
            Titulo = TituloBox.Text.Trim(),
            Fecha = fechaHora,
            Duracion = TextoOpcional(DuracionBox.Text),
            Participantes = participantes,
            QuienRolId = primero.CargoId,
            QuienRol = primero.Cargo,
            QuienPersonaId = primero.PersonaId,
            QuienPersona = primero.Nombre,
            QuienGraboId = primero.PersonaId,
            QuienGrabo = primero.Texto,
            ConQuienRolId = segundo?.CargoId,
            ConQuienRol = segundo?.Cargo ?? string.Empty,
            ConQuienPersonaId = segundo?.PersonaId,
            ConQuienPersona = segundo?.Nombre ?? string.Empty,
            ConQuienId = segundo?.PersonaId,
            ConQuien = segundo?.Texto ?? string.Empty,
            CamaraId = camara?.Id,
            Camara = camara?.Nombre ?? string.Empty,
            EquipoIds = equipos.Select(x => x.Item.Id).ToList(),
            Equipos = equipos.Select(x => x.Nombre).ToList(),
            Notas = TextoOpcional(NotasBox.Text),
            Estado = EstadoSeleccionado(),
            LimiteLluvia = LimiteLluviaPicker.Date,
            LimiteGrabando = LimiteGrabandoPicker.Date,
            LimiteEditando = LimiteEditandoPicker.Date,
            LimiteRevision = LimiteRevisionPicker.Date,
            Creado = App.Data.GetRegistro(_registroId ?? Guid.Empty)?.Creado ?? DateTimeOffset.Now
        };

        App.Data.GuardarRegistro(registro);

        if (App.MainAppWindow is MainWindow window)
        {
            window.NavigateTo(typeof(HomePage));
        }
    }

    private async void Exportar_Click(object sender, RoutedEventArgs e)
    {
        if (_registroId is not Guid id || App.Data.GetRegistro(id) is not { } registro)
        {
            MostrarAviso("Guarda la grabación antes de exportar el informe.", InfoBarSeverity.Warning);
            return;
        }

        if (App.MainAppWindow is not MainWindow window)
        {
            return;
        }

        var html = ReporteService.CrearHtmlIndividual(registro);
        if (await InformeExport.GuardarAsync(window, $"informe-{registro.Titulo}", html))
        {
            MostrarAviso("Se exportó el informe de esta grabación con el logo de Iderma.", InfoBarSeverity.Success);
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainAppWindow is MainWindow window)
        {
            window.NavigateTo(typeof(HomePage));
        }
    }

    private void IrConfiguracion_Click(object sender, RoutedEventArgs e)
    {
        if (App.MainAppWindow is MainWindow window)
        {
            window.NavigateTo(typeof(SettingsPage));
        }
    }

    private void SeleccionarEstado(string estado)
    {
        var opcion = estado switch
        {
            EstadosProduccion.Grabando => EstadoGrabando,
            EstadosProduccion.Editando => EstadoEditando,
            EstadosProduccion.Revision => EstadoRevision,
            EstadosProduccion.Completado => EstadoCompletado,
            _ => EstadoLluvia
        };

        opcion.IsChecked = true;
        ResaltarEtapa(estado);
    }

    private void MostrarAviso(string mensaje, InfoBarSeverity severidad)
    {
        AvisoBar.Severity = severidad;
        AvisoBar.Message = mensaje;
        AvisoBar.IsOpen = true;
    }

    private static string? TextoOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

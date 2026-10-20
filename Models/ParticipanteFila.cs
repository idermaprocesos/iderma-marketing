using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;

namespace BitacoraGrabacion.Models;

public sealed class ParticipanteFila : INotifyPropertyChanged
{
    private CatalogItem? _cargoSeleccionado;
    private Persona? _personaSeleccionada;
    private string _nombreInvitado = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string IdTexto => Id.ToString();
    public IReadOnlyList<CatalogItem> Cargos { get; init; } = [];
    public IReadOnlyList<Persona> Nombres { get; init; } = [];

    public CatalogItem? CargoSeleccionado
    {
        get => _cargoSeleccionado;
        set
        {
            if (_cargoSeleccionado == value)
            {
                return;
            }

            _cargoSeleccionado = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EsInvitado));
            OnPropertyChanged(nameof(ComboNombreVisibility));
            OnPropertyChanged(nameof(CajaInvitadoVisibility));

            if (EsInvitado)
            {
                PersonaSeleccionada = null;
            }
            else
            {
                NombreInvitado = string.Empty;
            }
        }
    }

    public Persona? PersonaSeleccionada
    {
        get => _personaSeleccionada;
        set
        {
            if (_personaSeleccionada == value)
            {
                return;
            }

            _personaSeleccionada = value;
            OnPropertyChanged();
        }
    }

    public string NombreInvitado
    {
        get => _nombreInvitado;
        set
        {
            var texto = value ?? string.Empty;
            if (_nombreInvitado == texto)
            {
                return;
            }

            _nombreInvitado = texto;
            OnPropertyChanged();
        }
    }

    public bool EsInvitado => EsCargoDeInvitado(_cargoSeleccionado?.Nombre);

    public Visibility ComboNombreVisibility => EsInvitado ? Visibility.Collapsed : Visibility.Visible;

    public Visibility CajaInvitadoVisibility => EsInvitado ? Visibility.Visible : Visibility.Collapsed;

    public static bool EsCargoDeInvitado(string? cargo) =>
        !string.IsNullOrWhiteSpace(cargo) &&
        (cargo.Contains("talento", StringComparison.CurrentCultureIgnoreCase) ||
         cargo.Contains("presentador", StringComparison.CurrentCultureIgnoreCase) ||
         cargo.Contains("invitado", StringComparison.CurrentCultureIgnoreCase));

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? nombre = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombre));
}

using System.Globalization;
using Microsoft.UI.Xaml;

namespace BitacoraGrabacion.Models;

public class RegistroGrabacion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdTexto => Id.ToString();
    public string Titulo { get; set; } = string.Empty;
    public DateTimeOffset Fecha { get; set; } = DateTimeOffset.Now;
    public string? Duracion { get; set; }
    public Guid? QuienRolId { get; set; }
    public string QuienRol { get; set; } = string.Empty;
    public Guid? QuienPersonaId { get; set; }
    public string QuienPersona { get; set; } = string.Empty;
    public Guid? QuienGraboId { get; set; }
    public string QuienGrabo { get; set; } = string.Empty;
    public Guid? ConQuienRolId { get; set; }
    public string ConQuienRol { get; set; } = string.Empty;
    public Guid? ConQuienPersonaId { get; set; }
    public string ConQuienPersona { get; set; } = string.Empty;
    public Guid? ConQuienId { get; set; }
    public string ConQuien { get; set; } = string.Empty;
    public List<ParticipanteGrabacion> Participantes { get; set; } = [];
    public string? OtrasPersonas { get; set; }
    public Guid? CamaraId { get; set; }
    public string Camara { get; set; } = string.Empty;
    public List<Guid> EquipoIds { get; set; } = [];
    public List<string> Equipos { get; set; } = [];
    public string? Notas { get; set; }
    public string Estado { get; set; } = EstadosProduccion.Lluvia;
    public DateTimeOffset? LimiteLluvia { get; set; }
    public DateTimeOffset? LimiteGrabando { get; set; }
    public DateTimeOffset? LimiteEditando { get; set; }
    public DateTimeOffset? LimiteRevision { get; set; }
    public DateTimeOffset Creado { get; set; } = DateTimeOffset.Now;

    private static readonly CultureInfo Cultura = new("es-MX");

    public string FechaTexto => Fecha.ToString("d MMM yyyy, HH:mm", Cultura);

    public string EstadoNormalizado => EstadosProduccion.Normalizar(Estado);

    public string EstadoTexto => EstadosProduccion.Texto(Estado);

    public DateTimeOffset? FechaLimiteActual => EstadoNormalizado switch
    {
        EstadosProduccion.Grabando => LimiteGrabando,
        EstadosProduccion.Editando => LimiteEditando,
        EstadosProduccion.Revision => LimiteRevision,
        EstadosProduccion.Completado => null,
        _ => LimiteLluvia
    };

    public bool EstaVencida =>
        EstadoNormalizado != EstadosProduccion.Completado &&
        FechaLimiteActual is { } fecha &&
        fecha.Date < DateTimeOffset.Now.Date;

    public Visibility CompletarVisibility =>
        EstadoNormalizado == EstadosProduccion.Completado ? Visibility.Collapsed : Visibility.Visible;

    public string FechaLimiteTexto
    {
        get
        {
            if (EstadoNormalizado == EstadosProduccion.Completado)
            {
                return "Completada";
            }

            if (FechaLimiteActual is not { } fecha)
            {
                return "Sin fecha límite";
            }

            var texto = fecha.ToString("d MMM yyyy", Cultura);
            return EstaVencida ? $"Venció el {texto}" : $"Límite {texto}";
        }
    }

    public Visibility LimiteVencidaVisibility => EstaVencida ? Visibility.Visible : Visibility.Collapsed;

    public Visibility LimiteNormalVisibility => EstaVencida ? Visibility.Collapsed : Visibility.Visible;

    public string LimiteLluviaTexto => TextoFecha(LimiteLluvia);

    public string LimiteGrabandoTexto => TextoFecha(LimiteGrabando);

    public string LimiteEditandoTexto => TextoFecha(LimiteEditando);

    public string LimiteRevisionTexto => TextoFecha(LimiteRevision);

    private static string TextoFecha(DateTimeOffset? fecha) =>
        fecha?.ToString("d MMM yyyy", Cultura) ?? string.Empty;

    public string PersonasTexto
    {
        get
        {
            AsegurarParticipantes();
            var partes = Participantes
                .Select(p => p.Texto)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();

            if (!string.IsNullOrWhiteSpace(OtrasPersonas))
            {
                partes.Add(OtrasPersonas);
            }

            return partes.Count == 0 ? "Sin personas registradas" : string.Join("  ·  ", partes);
        }
    }

    public void AsegurarParticipantes()
    {
        if (Participantes.Count > 0)
        {
            return;
        }

        AgregarSiHay(QuienPersonaId ?? QuienGraboId, QuienPersona, QuienRolId, QuienRol, QuienGrabo);
        AgregarSiHay(ConQuienPersonaId ?? ConQuienId, ConQuienPersona, ConQuienRolId, ConQuienRol, ConQuien);
    }

    private void AgregarSiHay(Guid? personaId, string? nombre, Guid? cargoId, string? cargo, string? texto)
    {
        var nombreFinal = string.IsNullOrWhiteSpace(nombre) ? ExtraerNombre(texto) : nombre.Trim();
        var cargoFinal = cargo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombreFinal) && string.IsNullOrWhiteSpace(cargoFinal) && personaId is null)
        {
            return;
        }

        Participantes.Add(new ParticipanteGrabacion
        {
            PersonaId = personaId,
            Nombre = nombreFinal,
            CargoId = cargoId,
            Cargo = cargoFinal
        });
    }

    private static string ExtraerNombre(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return string.Empty;
        }

        var corte = texto.IndexOf(" (", StringComparison.Ordinal);
        return corte > 0 ? texto[..corte] : texto.Trim();
    }

    public string EquipoTexto
    {
        get
        {
            var partes = new List<string>();
            if (!string.IsNullOrWhiteSpace(Camara))
            {
                partes.Add($"Cámara: {Camara}");
            }

            if (Equipos.Count > 0)
            {
                partes.Add(string.Join(", ", Equipos));
            }

            return partes.Count == 0 ? "Sin equipo registrado" : string.Join("  ·  ", partes);
        }
    }
}

namespace BitacoraGrabacion.Models;

public class ParticipanteGrabacion
{
    public Guid? PersonaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid? CargoId { get; set; }
    public string Cargo { get; set; } = string.Empty;

    public string Texto
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Nombre) && string.IsNullOrWhiteSpace(Cargo))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(Cargo))
            {
                return Nombre;
            }

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                return Cargo;
            }

            return $"{Nombre} ({Cargo})";
        }
    }
}

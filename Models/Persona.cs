namespace BitacoraGrabacion.Models;

public class Persona
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdTexto => Id.ToString();
    public string Nombre { get; set; } = string.Empty;
    public Guid? RolId { get; set; }
    public string? Rol { get; set; }

    public string NombreConRol =>
        string.IsNullOrWhiteSpace(Rol) ? Nombre : $"{Nombre}  ·  {Rol}";
}

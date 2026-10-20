namespace BitacoraGrabacion.Models;

public class CatalogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdTexto => Id.ToString();
    public string Nombre { get; set; } = string.Empty;
    public string? Detalle { get; set; }
}

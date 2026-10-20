namespace BitacoraGrabacion.Models;

public class AppStore
{
    public List<CatalogItem> Responsables { get; set; } = [];
    public List<CatalogItem> Roles { get; set; } = [];
    public List<Persona> Personas { get; set; } = [];
    public List<CatalogItem> Camaras { get; set; } = [];
    public List<CatalogItem> Equipos { get; set; } = [];
    public List<RegistroGrabacion> Registros { get; set; } = [];
}

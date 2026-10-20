namespace BitacoraGrabacion.Models;

public class SelectableItem
{
    public CatalogItem Item { get; init; } = new();
    public bool IsSelected { get; set; }

    public string Nombre => Item.Nombre;
}

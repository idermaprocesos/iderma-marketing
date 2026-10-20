namespace BitacoraGrabacion.Models;

public class CargoReporte
{
    public string Cargo { get; set; } = string.Empty;
    public int Grabaciones { get; set; }
    public int Participaciones { get; set; }
    public string Personas { get; set; } = string.Empty;
    public string GrabacionesTexto => Grabaciones == 1 ? "1 grabación" : $"{Grabaciones} grabaciones";
}

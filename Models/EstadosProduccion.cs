namespace BitacoraGrabacion.Models;

public static class EstadosProduccion
{
    public const string Lluvia = "lluvia";
    public const string Grabando = "grabando";
    public const string Editando = "editando";
    public const string Revision = "revision";
    public const string Completado = "completado";

    public static string Texto(string? estado) => Normalizar(estado) switch
    {
        Grabando => "Grabando",
        Editando => "Editando",
        Revision => "En revisión",
        Completado => "Completado",
        _ => "Lluvia de ideas"
    };

    public static string Normalizar(string? estado) => estado switch
    {
        Grabando => Grabando,
        Editando => Editando,
        Revision => Revision,
        Completado => Completado,
        _ => Lluvia
    };
}

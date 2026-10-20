using System.Globalization;
using System.Text;
using BitacoraGrabacion.Models;

namespace BitacoraGrabacion.Services;

public enum TipoPeriodo
{
    Semana,
    Quincena,
    TresSemanas,
    Mes
}

public sealed class RangoPeriodo
{
    public DateTimeOffset Inicio { get; init; }
    public DateTimeOffset Fin { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Etiqueta { get; init; } = string.Empty;
}

public static class ReporteService
{
    private static readonly CultureInfo Cultura = new("es-MX");

    public static RangoPeriodo CalcularPeriodo(TipoPeriodo tipo, DateTimeOffset referencia)
    {
        var fecha = referencia.Date;
        DateTime inicio;
        DateTime fin;
        string nombre;

        switch (tipo)
        {
            case TipoPeriodo.Semana:
                var desdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
                inicio = fecha.AddDays(-desdeLunes);
                fin = inicio.AddDays(6);
                nombre = "Semana";
                break;
            case TipoPeriodo.Quincena:
                if (fecha.Day <= 15)
                {
                    inicio = new DateTime(fecha.Year, fecha.Month, 1);
                    fin = new DateTime(fecha.Year, fecha.Month, 15);
                    nombre = "Primera quincena";
                }
                else
                {
                    inicio = new DateTime(fecha.Year, fecha.Month, 16);
                    fin = new DateTime(fecha.Year, fecha.Month, DateTime.DaysInMonth(fecha.Year, fecha.Month));
                    nombre = "Segunda quincena";
                }
                break;
            case TipoPeriodo.TresSemanas:
                fin = fecha;
                inicio = fecha.AddDays(-20);
                nombre = "3 semanas";
                break;
            default:
                inicio = new DateTime(fecha.Year, fecha.Month, 1);
                fin = new DateTime(fecha.Year, fecha.Month, DateTime.DaysInMonth(fecha.Year, fecha.Month));
                nombre = "Mes";
                break;
        }

        return new RangoPeriodo
        {
            Inicio = new DateTimeOffset(inicio, referencia.Offset),
            Fin = new DateTimeOffset(fin.AddDays(1).AddTicks(-1), referencia.Offset),
            Nombre = nombre,
            Etiqueta = $"{nombre}: {inicio.ToString("d MMM yyyy", Cultura)} – {fin.ToString("d MMM yyyy", Cultura)}"
        };
    }

    public static IReadOnlyList<RegistroGrabacion> Filtrar(IEnumerable<RegistroGrabacion> registros, RangoPeriodo rango)
    {
        return registros
            .Where(r => r.Fecha >= rango.Inicio && r.Fecha <= rango.Fin)
            .OrderByDescending(r => r.Fecha)
            .ToList();
    }

    public static IReadOnlyList<CargoReporte> PorCargo(IEnumerable<RegistroGrabacion> registros)
    {
        var grupos = new Dictionary<string, (HashSet<Guid> Grabaciones, int Participaciones, HashSet<string> Personas)>(
            StringComparer.CurrentCultureIgnoreCase);

        foreach (var registro in registros)
        {
            registro.AsegurarParticipantes();
            foreach (var participante in registro.Participantes)
            {
                var cargo = string.IsNullOrWhiteSpace(participante.Cargo) ? "Sin cargo" : participante.Cargo.Trim();
                if (!grupos.TryGetValue(cargo, out var datos))
                {
                    datos = ([], 0, new HashSet<string>(StringComparer.CurrentCultureIgnoreCase));
                }

                datos.Grabaciones.Add(registro.Id);
                datos.Participaciones++;
                if (!string.IsNullOrWhiteSpace(participante.Nombre))
                {
                    datos.Personas.Add(participante.Nombre.Trim());
                }

                grupos[cargo] = datos;
            }
        }

        return grupos
            .Select(g => new CargoReporte
            {
                Cargo = g.Key,
                Grabaciones = g.Value.Grabaciones.Count,
                Participaciones = g.Value.Participaciones,
                Personas = g.Value.Personas.Count == 0
                    ? string.Empty
                    : string.Join(", ", g.Value.Personas.OrderBy(n => n))
            })
            .OrderByDescending(c => c.Grabaciones)
            .ThenBy(c => c.Cargo)
            .ToList();
    }

    public static string CrearHtml(RangoPeriodo rango, IReadOnlyList<RegistroGrabacion> grabaciones, IReadOnlyList<CargoReporte> cargos)
    {
        var sb = IniciarDocumento("Reporte de bitácora", rango.Etiqueta, "Bitácora de grabación");
        sb.AppendLine($"<p class=\"total\">Total de grabaciones: {grabaciones.Count}</p>");

        sb.AppendLine("<h2>Reporte de grabaciones</h2>");
        sb.AppendLine("<table><thead><tr><th>Fecha</th><th>Título</th><th>Etapa</th><th>Fecha límite</th><th>Personas</th><th>Cámara</th><th>Equipo</th><th>Notas</th></tr></thead><tbody>");
        if (grabaciones.Count == 0)
        {
            sb.AppendLine("<tr><td colspan=\"8\">No hay grabaciones en este periodo.</td></tr>");
        }
        else
        {
            foreach (var registro in grabaciones)
            {
                sb.AppendLine("<tr>" +
                    $"<td>{Html(registro.FechaTexto)}</td>" +
                    $"<td>{Html(registro.Titulo)}</td>" +
                    $"<td>{Html(registro.EstadoTexto)}</td>" +
                    $"<td>{Html(registro.FechaLimiteTexto)}</td>" +
                    $"<td>{Html(registro.PersonasTexto)}</td>" +
                    $"<td>{Html(registro.Camara)}</td>" +
                    $"<td>{Html(string.Join(", ", registro.Equipos))}</td>" +
                    $"<td>{Html(registro.Notas)}</td></tr>");
            }
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("<h2>Reporte por cargo</h2>");
        sb.AppendLine("<table><thead><tr><th>Cargo</th><th>Grabaciones</th><th>Participaciones</th><th>Personas</th></tr></thead><tbody>");
        if (cargos.Count == 0)
        {
            sb.AppendLine("<tr><td colspan=\"4\">No hay cargos en este periodo.</td></tr>");
        }
        else
        {
            foreach (var cargo in cargos)
            {
                sb.AppendLine("<tr>" +
                    $"<td>{Html(cargo.Cargo)}</td>" +
                    $"<td>{cargo.Grabaciones.ToString(Cultura)}</td>" +
                    $"<td>{cargo.Participaciones.ToString(Cultura)}</td>" +
                    $"<td>{Html(cargo.Personas)}</td></tr>");
            }
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public static string CrearHtmlIndividual(RegistroGrabacion registro)
    {
        registro.AsegurarParticipantes();
        var sb = IniciarDocumento(registro.Titulo, registro.FechaTexto, "Informe de grabación");

        sb.AppendLine("<h2>Datos de la sesión</h2>");
        sb.AppendLine("<table class=\"ficha\"><tbody>");
        Fila(sb, "Título", registro.Titulo);
        Fila(sb, "Fecha", registro.FechaTexto);
        Fila(sb, "Etapa", registro.EstadoTexto);
        Fila(sb, "Límite de lluvia de ideas", registro.LimiteLluviaTexto);
        Fila(sb, "Límite de grabación", registro.LimiteGrabandoTexto);
        Fila(sb, "Límite de edición", registro.LimiteEditandoTexto);
        Fila(sb, "Límite de revisión", registro.LimiteRevisionTexto);
        Fila(sb, "Duración", registro.Duracion);
        Fila(sb, "Cámara", registro.Camara);
        Fila(sb, "Equipo", string.Join(", ", registro.Equipos));
        Fila(sb, "Notas", registro.Notas);
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("<h2>Personas</h2>");
        sb.AppendLine("<table><thead><tr><th>Cargo</th><th>Nombre</th></tr></thead><tbody>");
        if (registro.Participantes.Count == 0)
        {
            sb.AppendLine("<tr><td colspan=\"2\">Sin personas registradas.</td></tr>");
        }
        else
        {
            foreach (var persona in registro.Participantes)
            {
                sb.AppendLine($"<tr><td>{Html(persona.Cargo)}</td><td>{Html(persona.Nombre)}</td></tr>");
            }
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static StringBuilder IniciarDocumento(string titulo, string meta, string encabezado)
    {
        var logo = LogoEnBase64();
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"es\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\" />");
        sb.AppendLine($"<title>{Html(titulo)} — Iderma Capilar</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body{font-family:'Segoe UI',Calibri,Arial,sans-serif;color:#1b2a4a;margin:32px;background:#fff;}");
        sb.AppendLine(".encabezado{text-align:center;border-bottom:3px solid #c4a35a;padding-bottom:20px;margin-bottom:28px;}");
        sb.AppendLine(".encabezado img{max-height:140px;max-width:420px;object-fit:contain;}");
        sb.AppendLine("h1{font-size:22px;margin:16px 0 4px;color:#1b2a4a;}");
        sb.AppendLine("h2{font-size:16px;color:#1b2a4a;border-left:4px solid #c4a35a;padding-left:10px;margin:28px 0 12px;}");
        sb.AppendLine(".meta{color:#5a6577;margin:0;}");
        sb.AppendLine(".total{font-size:18px;font-weight:600;margin-top:8px;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;margin-top:8px;font-size:13px;}");
        sb.AppendLine("th{background:#1b2a4a;color:#fff;text-align:left;padding:8px 10px;}");
        sb.AppendLine("td{border-bottom:1px solid #e4e7ec;padding:8px 10px;vertical-align:top;}");
        sb.AppendLine("tr:nth-child(even) td{background:#f7f4ee;}");
        sb.AppendLine("table.ficha td:first-child{font-weight:600;width:180px;}");
        sb.AppendLine("@media print{body{margin:12px;} .encabezado img{max-height:110px;}}");
        sb.AppendLine("</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<header class=\"encabezado\">");
        if (!string.IsNullOrEmpty(logo))
        {
            sb.AppendLine($"<img src=\"data:image/jpeg;base64,{logo}\" alt=\"Iderma Capilar\" />");
        }

        sb.AppendLine($"<h1>{Html(encabezado)}</h1>");
        sb.AppendLine($"<p class=\"meta\">{Html(meta)}</p>");
        sb.AppendLine("</header>");
        return sb;
    }

    private static void Fila(StringBuilder sb, string etiqueta, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return;
        }

        sb.AppendLine($"<tr><td>{Html(etiqueta)}</td><td>{Html(valor)}</td></tr>");
    }

    private static string LogoEnBase64()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "img", "IDERMA LOGO.jpg");
        if (!File.Exists(ruta))
        {
            ruta = Path.Combine(AppContext.BaseDirectory, "IDERMA LOGO.jpg");
        }

        return File.Exists(ruta) ? Convert.ToBase64String(File.ReadAllBytes(ruta)) : string.Empty;
    }

    private static string Html(string? valor) =>
        System.Net.WebUtility.HtmlEncode(valor ?? string.Empty);
}

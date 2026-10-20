using System.Text.Json;
using BitacoraGrabacion.Models;
using Windows.Storage;

namespace BitacoraGrabacion.Services;

public sealed class DataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;

    public AppStore Store { get; private set; } = new();

    public DataService()
    {
        _path = ResolveDataPath();
        Load();
    }

    public IEnumerable<RegistroGrabacion> GetRegistros(string? busqueda = null)
    {
        foreach (var registro in Store.Registros)
        {
            registro.AsegurarParticipantes();
        }

        var query = Store.Registros.OrderByDescending(r => r.Fecha);
        if (string.IsNullOrWhiteSpace(busqueda))
        {
            return query;
        }

        var termino = busqueda.Trim();
        return query.Where(r =>
            Contiene(r.Titulo, termino) ||
            r.Participantes.Any(p => Contiene(p.Nombre, termino) || Contiene(p.Cargo, termino) || Contiene(p.Texto, termino)) ||
            Contiene(r.QuienGrabo, termino) ||
            Contiene(r.QuienPersona, termino) ||
            Contiene(r.QuienRol, termino) ||
            Contiene(r.ConQuien, termino) ||
            Contiene(r.ConQuienPersona, termino) ||
            Contiene(r.ConQuienRol, termino) ||
            Contiene(r.Camara, termino) ||
            r.Equipos.Any(e => Contiene(e, termino)) ||
            Contiene(r.Notas, termino) ||
            Contiene(r.OtrasPersonas, termino) ||
            Contiene(r.EstadoTexto, termino));
    }

    public RegistroGrabacion? GetRegistro(Guid id)
    {
        var registro = Store.Registros.FirstOrDefault(r => r.Id == id);
        registro?.AsegurarParticipantes();
        return registro;
    }

    public void GuardarRegistro(RegistroGrabacion registro)
    {
        var existente = GetRegistro(registro.Id);
        if (existente is null)
        {
            Store.Registros.Add(registro);
        }
        else
        {
            var indice = Store.Registros.IndexOf(existente);
            Store.Registros[indice] = registro;
        }

        Save();
    }

    public void EliminarRegistro(Guid id)
    {
        Store.Registros.RemoveAll(r => r.Id == id);
        Save();
    }

    public CatalogItem AgregarCatalogo(List<CatalogItem> lista, string nombre, string? detalle = null)
    {
        var item = new CatalogItem
        {
            Nombre = nombre.Trim(),
            Detalle = string.IsNullOrWhiteSpace(detalle) ? null : detalle.Trim()
        };
        lista.Add(item);
        Save();
        return item;
    }

    public void EliminarCatalogo(List<CatalogItem> lista, Guid id)
    {
        lista.RemoveAll(i => i.Id == id);
        Save();
    }

    public Persona AgregarPersona(string nombre, CatalogItem? rol = null)
    {
        var persona = new Persona
        {
            Nombre = nombre.Trim(),
            RolId = rol?.Id,
            Rol = rol?.Nombre
        };
        Store.Personas.Add(persona);
        Save();
        return persona;
    }

    public void EliminarPersona(Guid id)
    {
        Store.Personas.RemoveAll(p => p.Id == id);
        Save();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(Store, JsonOptions));
    }

    private void Load()
    {
        if (!File.Exists(_path))
        {
            Store = CrearCatalogoInicial();
            Save();
            return;
        }

        try
        {
            var json = File.ReadAllText(_path);
            Store = JsonSerializer.Deserialize<AppStore>(json, JsonOptions) ?? CrearCatalogoInicial();
            AsegurarCatalogosMinimos();
        }
        catch (JsonException)
        {
            Store = CrearCatalogoInicial();
            Save();
        }
    }

    private void AsegurarCatalogosMinimos()
    {
        var cambio = false;
        if (Store.Roles.Count == 0)
        {
            Store.Roles = Store.Responsables.Count > 0
                ? [.. Store.Responsables]
                : [];
            cambio = true;
        }

        foreach (var rol in RolesIniciales())
        {
            if (!Store.Roles.Any(r => string.Equals(r.Nombre, rol.Nombre, StringComparison.CurrentCultureIgnoreCase)))
            {
                Store.Roles.Add(rol);
                cambio = true;
            }
        }

        foreach (var persona in PersonasIniciales(Store.Roles))
        {
            if (!Store.Personas.Any(p => string.Equals(p.Nombre, persona.Nombre, StringComparison.CurrentCultureIgnoreCase)))
            {
                Store.Personas.Add(persona);
                cambio = true;
            }
        }

        foreach (var persona in Store.Personas)
        {
            if (persona.RolId is null && !string.IsNullOrWhiteSpace(persona.Rol))
            {
                var rol = Store.Roles.FirstOrDefault(r =>
                    string.Equals(r.Nombre, persona.Rol, StringComparison.CurrentCultureIgnoreCase));
                if (rol is not null)
                {
                    persona.RolId = rol.Id;
                    cambio = true;
                }
            }
        }

        if (Store.Camaras.Count == 0)
        {
            Store.Camaras = CamarasIniciales();
            cambio = true;
        }

        if (Store.Equipos.Count == 0)
        {
            Store.Equipos = EquiposIniciales();
            cambio = true;
        }

        if (cambio)
        {
            Save();
        }
    }

    private static AppStore CrearCatalogoInicial()
    {
        var roles = RolesIniciales();
        return new()
        {
            Roles = roles,
            Personas = PersonasIniciales(roles),
            Camaras = CamarasIniciales(),
            Equipos = EquiposIniciales()
        };
    }

    private static List<CatalogItem> RolesIniciales() =>
    [
        new() { Nombre = "Operador de cámara" },
        new() { Nombre = "Productor" },
        new() { Nombre = "Director" },
        new() { Nombre = "Talento / Presentador" },
        new() { Nombre = "Asistente de producción" },
        new() { Nombre = "Editor" }
    ];

    private static List<Persona> PersonasIniciales(IReadOnlyList<CatalogItem> roles)
    {
        CatalogItem? Rol(string nombre) =>
            roles.FirstOrDefault(r => string.Equals(r.Nombre, nombre, StringComparison.Ordinal));

        Persona Nueva(string nombre, string rolNombre)
        {
            var rol = Rol(rolNombre);
            return new Persona
            {
                Nombre = nombre,
                RolId = rol?.Id,
                Rol = rol?.Nombre ?? rolNombre
            };
        }

        return
        [
            Nueva("Ana Morales", "Operador de cámara"),
            Nueva("Luis Hernández", "Operador de cámara"),
            Nueva("Mariana Castillo", "Productor"),
            Nueva("Diego Ramírez", "Director"),
            Nueva("Sofía Vega", "Talento / Presentador"),
            Nueva("Carlos Mendoza", "Talento / Presentador"),
            Nueva("Valeria Ríos", "Asistente de producción"),
            Nueva("Andrés Peña", "Editor"),
            Nueva("Elena Torres", "Operador de cámara"),
            Nueva("Pablo Jiménez", "Productor"),
            Nueva("Lucía Navarro", "Talento / Presentador")
        ];
    }

    private static List<CatalogItem> CamarasIniciales() =>
    [
        new() { Nombre = "Sony FX3" },
        new() { Nombre = "Canon EOS R6" },
        new() { Nombre = "iPhone 15 Pro" },
        new() { Nombre = "Cámara web" }
    ];

    private static List<CatalogItem> EquiposIniciales() =>
    [
        new() { Nombre = "Micrófono de solapa" },
        new() { Nombre = "Micrófono shotgun" },
        new() { Nombre = "Trípode" },
        new() { Nombre = "Gimbal" },
        new() { Nombre = "Luz LED" },
        new() { Nombre = "Monitor de campo" },
        new() { Nombre = "Grabadora de audio" },
        new() { Nombre = "Auriculares" }
    ];

    private static string ResolveDataPath()
    {
        try
        {
            return Path.Combine(ApplicationData.Current.LocalFolder.Path, "bitacora.json");
        }
        catch (Exception)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BitacoraGrabacion");
            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "bitacora.json");
        }
    }

    private static bool Contiene(string? valor, string termino) =>
        !string.IsNullOrWhiteSpace(valor) &&
        valor.Contains(termino, StringComparison.CurrentCultureIgnoreCase);
}

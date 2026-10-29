using System.Net;
using System.Text.RegularExpressions;
using InmoArq.Web;
using InmoArq.Web.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InmoArq.Tests;

/// <summary>Levanta la aplicación real en un puerto libre con una base de datos SQLite propia.</summary>
public sealed class ServidorPrueba : IDisposable
{
    public static readonly Dictionary<string, (string Email, string Clave)> Credenciales =
        DbInicializador.UsuariosDemo.ToDictionary(u => u.Rol, u => (u.Email, u.Clave));

    private readonly WebApplication _app;
    private readonly string _rutaBd;

    public ServidorPrueba()
    {
        _rutaBd = Path.Combine(Path.GetTempPath(), $"inmoarq-prueba-{Guid.NewGuid():N}.db");
        var rutaBd = _rutaBd;
        _app = Program.Construir(Array.Empty<string>(), b =>
        {
            b.WebHost.UseUrls("http://127.0.0.1:0");
            b.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseDatos:Proveedor"] = "Sqlite",
                ["BaseDatos:DatosDemo"] = "true",
                ["ConnectionStrings:Default"] = $"Data Source={rutaBd}",
            });
            b.Logging.ClearProviders();
        });
        _app.StartAsync().GetAwaiter().GetResult();
        var direcciones = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
        Direccion = new Uri(direcciones.First());
    }

    public Uri Direccion { get; }

    public ClienteWeb NuevoCliente() => new(Direccion);

    /// <summary>Consulta directa a la base de datos para verificar resultados.</summary>
    public T Bd<T>(Func<AppDbContext, T> consulta)
    {
        using var scope = _app.Services.CreateScope();
        return consulta(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public void Cambiar(Action<AppDbContext> cambio)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        cambio(db);
        db.SaveChanges();
    }

    public int IdUsuario(string email) => Bd(db => db.Usuarios.Single(u => u.Email == email).Id);

    public int IdRol(string nombre) => Bd(db => db.Roles.Single(r => r.Nombre == nombre).Id);

    public string RolDe(string email) => Bd(db =>
        (from u in db.Usuarios join r in db.Roles on u.RolId equals r.Id where u.Email == email select r.Nombre).Single());

    public void Dispose()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
        try { File.Delete(_rutaBd); } catch { /* el archivo temporal se puede quedar */ }
    }
}

public sealed record Respuesta(int Estado, string? Ubicacion, string Html);

/// <summary>Navegador simulado: guarda cookies, no sigue redirecciones y agrega el token CSRF.</summary>
public sealed class ClienteWeb : IDisposable
{
    private static readonly Regex Token = new("name=\"__RequestVerificationToken\"[^>]*?value=\"([^\"]+)\"");
    private readonly HttpClient _http;

    public ClienteWeb(Uri direccion)
    {
        var manejador = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true, CookieContainer = new CookieContainer() };
        _http = new HttpClient(manejador) { BaseAddress = direccion };
    }

    public async Task<Respuesta> GetAsync(string ruta)
    {
        using var r = await _http.GetAsync(ruta);
        return await LeerAsync(r);
    }

    public async Task<string> TokenAsync(string pagina)
    {
        var r = await GetAsync(pagina);
        var m = Token.Match(r.Html);
        if (!m.Success)
            throw new InvalidOperationException($"La página {pagina} (HTTP {r.Estado}) no tiene token CSRF.");
        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    /// <summary>Envía un formulario con token CSRF tomado de <paramref name="paginaToken"/>.</summary>
    public async Task<Respuesta> PostAsync(string ruta, IEnumerable<KeyValuePair<string, string>>? campos = null,
        string paginaToken = "/")
    {
        var lista = new List<KeyValuePair<string, string>>(campos ?? Enumerable.Empty<KeyValuePair<string, string>>())
        {
            new("__RequestVerificationToken", await TokenAsync(paginaToken)),
        };
        return await EnviarAsync(ruta, lista);
    }

    public Task<Respuesta> PostSinTokenAsync(string ruta, IEnumerable<KeyValuePair<string, string>> campos) =>
        EnviarAsync(ruta, campos);

    public Task<Respuesta> LoginAsync(string email, string clave, string? returnUrl = null) =>
        PostAsync("/Cuenta/Login", Campos(("Email", email), ("Password", clave), ("ReturnUrl", returnUrl ?? "")), "/Cuenta/Login");

    public async Task LoginComoAsync(string rol)
    {
        var (email, clave) = ServidorPrueba.Credenciales[rol];
        var r = await LoginAsync(email, clave);
        Assert.Equal(302, r.Estado);
    }

    public Task<Respuesta> LogoutAsync() => PostAsync("/Cuenta/Logout");

    public Task<Respuesta> RegistrarAsync(string nombre = "María Pérez", string email = "maria@correo.com",
        string clave = "Clave2026", string? confirmacion = null) =>
        PostAsync("/Cuenta/Registro",
            Campos(("Nombre", nombre), ("Email", email), ("Password", clave), ("Confirmacion", confirmacion ?? clave)),
            "/Cuenta/Registro");

    public static List<KeyValuePair<string, string>> Campos(params (string Clave, string Valor)[] campos) =>
        campos.Select(c => new KeyValuePair<string, string>(c.Clave, c.Valor)).ToList();

    private async Task<Respuesta> EnviarAsync(string ruta, IEnumerable<KeyValuePair<string, string>> campos)
    {
        using var contenido = new FormUrlEncodedContent(campos);
        using var r = await _http.PostAsync(ruta, contenido);
        return await LeerAsync(r);
    }

    private static async Task<Respuesta> LeerAsync(HttpResponseMessage r) =>
        new((int)r.StatusCode, r.Headers.Location?.ToString(), await r.Content.ReadAsStringAsync());

    public void Dispose() => _http.Dispose();
}

/// <summary>Cada prueba arranca con un servidor y una base de datos nuevos.</summary>
public abstract class PruebaBase : IDisposable
{
    protected readonly ServidorPrueba Servidor;
    protected readonly ClienteWeb Cliente;

    protected PruebaBase()
    {
        Servidor = new ServidorPrueba();
        Cliente = Servidor.NuevoCliente();
    }

    public void Dispose()
    {
        Cliente.Dispose();
        Servidor.Dispose();
    }
}

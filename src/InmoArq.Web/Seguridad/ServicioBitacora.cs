using InmoArq.Web.Data;
using InmoArq.Web.Models;

namespace InmoArq.Web.Seguridad;

/// <summary>Registra en la bitácora quién hizo qué, cuándo y desde dónde.</summary>
public class ServicioBitacora
{
    private readonly AppDbContext _db;
    private readonly UsuarioActual _actual;
    private readonly IHttpContextAccessor _http;

    public ServicioBitacora(AppDbContext db, UsuarioActual actual, IHttpContextAccessor http)
    {
        _db = db;
        _actual = actual;
        _http = http;
    }

    public async Task RegistrarAsync(string evento, string detalle = "", int? usuarioId = null, string? email = null)
    {
        if (usuarioId is null && _actual.Usuario is not null)
        {
            usuarioId = _actual.Usuario.Id;
            email ??= _actual.Usuario.Email;
        }

        _db.Bitacora.Add(new RegistroBitacora
        {
            Fecha = DateTime.UtcNow,
            UsuarioId = usuarioId,
            Email = email,
            Evento = evento,
            Detalle = detalle,
            Ip = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
        });
        await _db.SaveChangesAsync();
    }
}

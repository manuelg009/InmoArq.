using System.Security.Claims;
using InmoArq.Web.Data;
using InmoArq.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InmoArq.Web.Seguridad;

/// <summary>
/// Usuario de la petición actual con su rol y permisos.
/// Se lee de la base de datos en cada petición: así un cambio de rol o de permisos
/// aplica de inmediato y una cuenta desactivada pierde la sesión en su siguiente clic.
/// </summary>
public class UsuarioActual
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private bool _cargado;
    private HashSet<string> _permisos = new();

    public UsuarioActual(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public Usuario? Usuario { get; private set; }
    public string? NombreRol { get; private set; }
    public IReadOnlyCollection<string> Permisos => _permisos;
    public bool Autenticado => Usuario is not null;

    public bool Puede(string permiso) => _permisos.Contains(permiso);

    public IEnumerable<Modulo> ModulosVisibles => Modulos.Todos.Where(m => Puede(m.Permiso));

    public async Task CargarAsync(ClaimsPrincipal? principal = null)
    {
        if (_cargado)
            return;
        _cargado = true;

        principal ??= _http.HttpContext?.User;
        var valor = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(valor, out var id))
            return;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null || !usuario.Activo)
            return;

        Usuario = usuario;
        NombreRol = await _db.Roles.Where(r => r.Id == usuario.RolId).Select(r => r.Nombre).FirstOrDefaultAsync();
        var codigos = await (from rp in _db.RolPermisos
                             join p in _db.Permisos on rp.PermisoId equals p.Id
                             where rp.RolId == usuario.RolId
                             select p.Codigo).ToListAsync();
        _permisos = codigos.ToHashSet();
    }
}

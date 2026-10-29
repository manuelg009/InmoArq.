using InmoArq.Web.Data;
using InmoArq.Web.Models;
using InmoArq.Web.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InmoArq.Web.Controllers;

/// <summary>HU-03 Asignación de roles y permisos · HU-05 Trazabilidad.</summary>
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UsuarioActual _actual;
    private readonly ServicioBitacora _bitacora;

    public AdminController(AppDbContext db, UsuarioActual actual, ServicioBitacora bitacora)
    {
        _db = db;
        _actual = actual;
        _bitacora = bitacora;
    }

    // ------------------------------------------------------------ usuarios

    [HttpGet]
    [Authorize(Policy = Permisos.UsuariosGestionar)]
    public async Task<IActionResult> Usuarios()
    {
        var filas = await (from u in _db.Usuarios
                           join r in _db.Roles on u.RolId equals r.Id
                           orderby u.Id
                           select new FilaUsuario(u, r.Nombre)).ToListAsync();
        ViewBag.Roles = await _db.Roles.OrderBy(r => r.Id).ToListAsync();
        return View(filas);
    }

    [HttpPost]
    [Authorize(Policy = Permisos.UsuariosGestionar)]
    public async Task<IActionResult> CambiarRol(int id, int rolId)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        var nuevo = await _db.Roles.FirstOrDefaultAsync(r => r.Id == rolId);

        if (usuario is null || nuevo is null)
            TempData["Error"] = "Usuario o rol no válido.";
        else if (usuario.Id == _actual.Usuario!.Id)
            TempData["Error"] = "No puedes cambiar tu propio rol.";
        else if (nuevo.Id == usuario.RolId)
            TempData["Aviso"] = "El usuario ya tiene ese rol.";
        else
        {
            var anterior = await _db.Roles.Where(r => r.Id == usuario.RolId).Select(r => r.Nombre).FirstAsync();
            if (anterior == Seguridad.Roles.Administrador && !await QuedanOtrosAdminsActivos(usuario.Id))
                TempData["Error"] = "Debe quedar al menos un administrador activo.";
            else
            {
                usuario.RolId = nuevo.Id;
                await _db.SaveChangesAsync();
                await _bitacora.RegistrarAsync(Eventos.CambioRol, $"{usuario.Email}: {anterior} → {nuevo.Nombre}");
                TempData["Exito"] = $"Rol de {usuario.Nombre} cambiado a {nuevo.Nombre}.";
            }
        }
        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [Authorize(Policy = Permisos.UsuariosGestionar)]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null)
            TempData["Error"] = "Usuario no encontrado.";
        else if (usuario.Id == _actual.Usuario!.Id)
            TempData["Error"] = "No puedes desactivar tu propia cuenta.";
        else if (usuario.Activo && await EsAdmin(usuario) && !await QuedanOtrosAdminsActivos(usuario.Id))
            TempData["Error"] = "Debe quedar al menos un administrador activo.";
        else
        {
            usuario.Activo = !usuario.Activo;
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            await _db.SaveChangesAsync();
            await _bitacora.RegistrarAsync(usuario.Activo ? Eventos.UsuarioActivado : Eventos.UsuarioDesactivado, usuario.Email);
            TempData["Exito"] = $"Cuenta de {usuario.Nombre} {(usuario.Activo ? "activada" : "desactivada")}.";
        }
        return RedirectToAction(nameof(Usuarios));
    }

    private async Task<bool> EsAdmin(Usuario usuario) =>
        await _db.Roles.AnyAsync(r => r.Id == usuario.RolId && r.Nombre == Seguridad.Roles.Administrador);

    private async Task<bool> QuedanOtrosAdminsActivos(int excluirId) =>
        await (from u in _db.Usuarios
               join r in _db.Roles on u.RolId equals r.Id
               where r.Nombre == Seguridad.Roles.Administrador && u.Activo && u.Id != excluirId
               select u.Id).AnyAsync();

    // ---------------------------------------------------- roles y permisos

    [HttpGet]
    [Authorize(Policy = Permisos.RolesGestionar)]
    public async Task<IActionResult> Roles()
    {
        var roles = await _db.Roles.OrderBy(r => r.Id).ToListAsync();
        var conteo = await _db.Usuarios.GroupBy(u => u.RolId)
            .Select(g => new { RolId = g.Key, Total = g.Count() }).ToListAsync();
        var asignados = await (from rp in _db.RolPermisos
                               join p in _db.Permisos on rp.PermisoId equals p.Id
                               select new { rp.RolId, p.Codigo }).ToListAsync();

        var vm = new RolesVM
        {
            Roles = roles.Select(r => new FilaRol(r, conteo.FirstOrDefault(c => c.RolId == r.Id)?.Total ?? 0)).ToList(),
            Permisos = await _db.Permisos.OrderBy(p => p.Id).ToListAsync(),
            Asignados = asignados.Select(a => (a.RolId, a.Codigo)).ToHashSet(),
        };
        return View(vm);
    }

    [HttpPost]
    [ActionName("Roles")]
    [Authorize(Policy = Permisos.RolesGestionar)]
    public async Task<IActionResult> GuardarPermisos()
    {
        var roles = await _db.Roles.ToListAsync();
        var permisos = await _db.Permisos.ToDictionaryAsync(p => p.Codigo, p => p.Id);
        var cambios = 0;

        foreach (var rol in roles)
        {
            var marcados = Request.Form[$"rol-{rol.Id}"]
                .Where(c => c is not null && permisos.ContainsKey(c)).Select(c => c!).ToHashSet();
            if (rol.Nombre == Seguridad.Roles.Administrador)
                marcados.UnionWith(Seguridad.Roles.CriticosAdmin);

            var actuales = await (from rp in _db.RolPermisos
                                  join p in _db.Permisos on rp.PermisoId equals p.Id
                                  where rp.RolId == rol.Id
                                  select p.Codigo).ToListAsync();
            var agregados = marcados.Except(actuales).OrderBy(c => c).ToList();
            var quitados = actuales.Except(marcados).OrderBy(c => c).ToList();
            if (agregados.Count == 0 && quitados.Count == 0)
                continue;

            foreach (var codigo in agregados)
                _db.RolPermisos.Add(new RolPermiso { RolId = rol.Id, PermisoId = permisos[codigo] });
            foreach (var codigo in quitados)
            {
                var permisoId = permisos[codigo];
                var fila = await _db.RolPermisos.FirstAsync(rp => rp.RolId == rol.Id && rp.PermisoId == permisoId);
                _db.RolPermisos.Remove(fila);
            }
            await _db.SaveChangesAsync();
            cambios++;

            var detalle = rol.Nombre;
            if (agregados.Count > 0) detalle += " · +" + string.Join(", +", agregados);
            if (quitados.Count > 0) detalle += " · −" + string.Join(", −", quitados);
            await _bitacora.RegistrarAsync(Eventos.CambioPermisos, detalle);
        }

        if (cambios > 0) TempData["Exito"] = "Permisos actualizados.";
        else TempData["Aviso"] = "No hubo cambios.";
        return RedirectToAction(nameof(Roles));
    }

    [HttpPost]
    [Authorize(Policy = Permisos.RolesGestionar)]
    public async Task<IActionResult> NuevoRol(string? nombre, string? descripcion)
    {
        nombre = (nombre ?? "").Trim();
        var nombreMinusculas = nombre.ToLower();
        if (nombre.Length < 3)
            TempData["Error"] = "El nombre del rol debe tener al menos 3 caracteres.";
        else if (await _db.Roles.AnyAsync(r => r.Nombre.ToLower() == nombreMinusculas))
            TempData["Error"] = "Ya existe un rol con ese nombre.";
        else
        {
            _db.Roles.Add(new Rol { Nombre = nombre, Descripcion = (descripcion ?? "").Trim() });
            await _db.SaveChangesAsync();
            await _bitacora.RegistrarAsync(Eventos.RolCreado, nombre);
            TempData["Exito"] = $"Rol «{nombre}» creado. Ahora marca sus permisos en la tabla.";
        }
        return RedirectToAction(nameof(Roles));
    }

    // ------------------------------------------------------------ bitácora

    [HttpGet]
    [Authorize(Policy = Permisos.BitacoraVer)]
    public async Task<IActionResult> Bitacora(string? evento)
    {
        var consulta = _db.Bitacora.AsQueryable();
        if (!string.IsNullOrEmpty(evento))
            consulta = consulta.Where(b => b.Evento == evento);

        return View(new BitacoraVM
        {
            Registros = await consulta.OrderByDescending(b => b.Id).Take(300).ToListAsync(),
            Eventos = await _db.Bitacora.Select(b => b.Evento).Distinct().OrderBy(e => e).ToListAsync(),
            Filtro = evento,
        });
    }
}

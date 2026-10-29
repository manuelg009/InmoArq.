using InmoArq.Web.Data;
using InmoArq.Web.Models;
using InmoArq.Web.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InmoArq.Web.Controllers;

/// <summary>Proyectos de diseño arquitectónico. Un cliente solo ve sus propios proyectos (HU-04).</summary>
public class ProyectosController : Controller
{
    public static readonly string[] Etapas = { "Anteproyecto", "Planos", "Renders 3D", "Permisos", "Aprobado" };

    private readonly AppDbContext _db;
    private readonly UsuarioActual _actual;
    private readonly ServicioBitacora _bitacora;

    public ProyectosController(AppDbContext db, UsuarioActual actual, ServicioBitacora bitacora)
    {
        _db = db;
        _actual = actual;
        _bitacora = bitacora;
    }

    [HttpGet]
    [Authorize(Policy = Permisos.ProyectosVer)]
    public async Task<IActionResult> Index()
    {
        var veTodos = _actual.Puede(Permisos.ProyectosGestionar);
        var miId = _actual.Usuario!.Id;

        var consulta = _db.Proyectos.AsQueryable();
        if (!veTodos)
            consulta = consulta.Where(p => p.ClienteId == miId);

        var filas = await (from p in consulta
                           join c in _db.Usuarios on p.ClienteId equals (int?)c.Id into clientes
                           from c in clientes.DefaultIfEmpty()
                           join a in _db.Usuarios on p.ArquitectoId equals (int?)a.Id into arquitectos
                           from a in arquitectos.DefaultIfEmpty()
                           orderby p.Id descending
                           select new FilaProyecto(p, c == null ? null : c.Nombre, a == null ? null : a.Nombre)).ToListAsync();

        var clientesActivos = await (from u in _db.Usuarios
                                     join r in _db.Roles on u.RolId equals r.Id
                                     where r.Nombre == Roles.Cliente && u.Activo
                                     orderby u.Nombre
                                     select u).ToListAsync();

        return View(new ProyectosVM { Proyectos = filas, Clientes = clientesActivos, VeTodos = veTodos });
    }

    [HttpPost]
    [Authorize(Policy = Permisos.ProyectosGestionar)]
    public async Task<IActionResult> Nuevo(string? nombre, string? etapa, int? clienteId)
    {
        nombre = (nombre ?? "").Trim();
        if (nombre.Length == 0 || !Etapas.Contains(etapa))
        {
            TempData["Error"] = "Escribe el nombre del proyecto y elige una etapa válida.";
            return RedirectToAction(nameof(Index));
        }

        _db.Proyectos.Add(new Proyecto
        {
            Nombre = nombre,
            Etapa = etapa!,
            ClienteId = clienteId,
            ArquitectoId = _actual.Usuario!.Id,
            CreadoEn = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync(Eventos.ProyectoCreado, nombre);
        TempData["Exito"] = "Proyecto creado.";
        return RedirectToAction(nameof(Index));
    }
}

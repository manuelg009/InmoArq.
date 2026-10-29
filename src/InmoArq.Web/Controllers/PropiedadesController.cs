using InmoArq.Web.Data;
using InmoArq.Web.Models;
using InmoArq.Web.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InmoArq.Web.Controllers;

/// <summary>Módulo de negocio para demostrar la restricción por perfil (HU-04).</summary>
public class PropiedadesController : Controller
{
    public static readonly string[] Operaciones = { "Venta", "Alquiler" };

    private readonly AppDbContext _db;
    private readonly UsuarioActual _actual;
    private readonly ServicioBitacora _bitacora;

    public PropiedadesController(AppDbContext db, UsuarioActual actual, ServicioBitacora bitacora)
    {
        _db = db;
        _actual = actual;
        _bitacora = bitacora;
    }

    [HttpGet]
    [Authorize(Policy = Permisos.PropiedadesVer)]
    public async Task<IActionResult> Index()
    {
        var filas = await (from p in _db.Propiedades
                           join u in _db.Usuarios on p.PublicadoPorId equals (int?)u.Id into autores
                           from u in autores.DefaultIfEmpty()
                           orderby p.Id descending
                           select new FilaPropiedad(p, u == null ? null : u.Nombre)).ToListAsync();
        return View(filas);
    }

    [HttpPost]
    [Authorize(Policy = Permisos.PropiedadesGestionar)]
    public async Task<IActionResult> Nueva(string? titulo, string? ubicacion, string? operacion, string? precio)
    {
        titulo = (titulo ?? "").Trim();
        ubicacion = (ubicacion ?? "").Trim();
        if (titulo.Length == 0 || ubicacion.Length == 0 || !Operaciones.Contains(operacion)
            || !Formato.IntentarLeerMonto(precio, out var monto) || monto <= 0)
        {
            TempData["Error"] = "Completa todos los campos con valores válidos.";
            return RedirectToAction(nameof(Index));
        }

        _db.Propiedades.Add(new Propiedad
        {
            Titulo = titulo,
            Ubicacion = ubicacion,
            Operacion = operacion!,
            Precio = monto,
            PublicadoPorId = _actual.Usuario!.Id,
            CreadoEn = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync();
        await _bitacora.RegistrarAsync(Eventos.PropiedadPublicada, titulo);
        TempData["Exito"] = "Propiedad publicada.";
        return RedirectToAction(nameof(Index));
    }
}

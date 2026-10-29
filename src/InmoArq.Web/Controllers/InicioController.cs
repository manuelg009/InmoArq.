using InmoArq.Web.Data;
using InmoArq.Web.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InmoArq.Web.Controllers;

[Authorize]
public class InicioController : Controller
{
    private readonly AppDbContext _db;
    private readonly UsuarioActual _actual;

    public InicioController(AppDbContext db, UsuarioActual actual)
    {
        _db = db;
        _actual = actual;
    }

    /// <summary>Tablero: muestra solo los módulos que el rol del usuario tiene permitidos.</summary>
    public IActionResult Index() => View();

    public async Task<IActionResult> Perfil()
    {
        var codigos = _actual.Permisos.ToList();
        var permisos = await _db.Permisos.Where(p => codigos.Contains(p.Codigo)).OrderBy(p => p.Id).ToListAsync();
        return View(permisos);
    }
}

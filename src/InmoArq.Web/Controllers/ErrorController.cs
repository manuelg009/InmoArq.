using InmoArq.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InmoArq.Web.Controllers;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class ErrorController : Controller
{
    [Route("Error/{codigo:int}")]
    public IActionResult Mostrar(int codigo)
    {
        var (titulo, mensaje) = codigo switch
        {
            400 => ("Solicitud no válida", "El formulario expiró o no es válido. Recarga la página e inténtalo de nuevo."),
            403 => ("Acceso denegado", "Tu rol no tiene permiso para usar esta función. El intento quedó registrado."),
            404 => ("Página no encontrada", "La dirección que buscas no existe."),
            _ => ("Ocurrió un error", "Algo salió mal. Inténtalo de nuevo."),
        };
        Response.StatusCode = codigo;
        return View("Error", new ErrorVM(codigo, titulo, mensaje));
    }
}

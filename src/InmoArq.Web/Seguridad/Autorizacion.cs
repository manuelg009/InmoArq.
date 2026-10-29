using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace InmoArq.Web.Seguridad;

/// <summary>Requisito de autorización: el rol del usuario debe tener este permiso.</summary>
public class PermisoRequerido : IAuthorizationRequirement
{
    public PermisoRequerido(string permiso) => Permiso = permiso;
    public string Permiso { get; }
}

/// <summary>Evalúa el requisito consultando los permisos actuales del rol en la base de datos.</summary>
public class ManejadorPermisos : AuthorizationHandler<PermisoRequerido>
{
    private readonly UsuarioActual _actual;

    public ManejadorPermisos(UsuarioActual actual) => _actual = actual;

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoRequerido requirement)
    {
        await _actual.CargarAsync(context.User);
        if (_actual.Puede(requirement.Permiso))
            context.Succeed(requirement);
    }
}

/// <summary>
/// Cuando un usuario autenticado intenta usar una función que su rol no permite:
/// registra el intento en la bitácora y responde 403 (Acceso denegado).
/// </summary>
public class ManejadorResultadoAutorizacion : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _porDefecto = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var requeridos = authorizeResult.AuthorizationFailure?.FailedRequirements
                .OfType<PermisoRequerido>().Select(r => r.Permiso) ?? Enumerable.Empty<string>();
            var bitacora = context.RequestServices.GetRequiredService<ServicioBitacora>();
            await bitacora.RegistrarAsync(Eventos.AccesoDenegado,
                $"{context.Request.Method} {context.Request.Path} (requiere {string.Join(", ", requeridos)})");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _porDefecto.HandleAsync(next, context, policy, authorizeResult);
    }
}

/// <summary>Valida en cada petición que la cuenta de la cookie siga existiendo y activa.</summary>
public static class ValidadorSesion
{
    public static async Task ValidarAsync(CookieValidatePrincipalContext context)
    {
        var actual = context.HttpContext.RequestServices.GetRequiredService<UsuarioActual>();
        await actual.CargarAsync(context.Principal);
        if (!actual.Autenticado)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

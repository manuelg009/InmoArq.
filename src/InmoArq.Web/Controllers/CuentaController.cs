using System.Security.Claims;
using System.Text.RegularExpressions;
using InmoArq.Web.Data;
using InmoArq.Web.Models;
using InmoArq.Web.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InmoArq.Web.Controllers;

/// <summary>HU-01 Registro de usuarios · HU-02 Inicio y cierre de sesión.</summary>
public class CuentaController : Controller
{
    private const string MensajeGenerico = "Correo o contraseña incorrectos.";
    private static readonly Regex FormatoEmail = new(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$");

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly ServicioBitacora _bitacora;
    private readonly IConfiguration _config;

    public CuentaController(AppDbContext db, IPasswordHasher<Usuario> hasher, ServicioBitacora bitacora, IConfiguration config)
    {
        _db = db;
        _hasher = hasher;
        _bitacora = bitacora;
        _config = config;
    }

    // ------------------------------------------------------------ registro

    [HttpGet]
    public IActionResult Registro()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Inicio");
        return View(new RegistroVM());
    }

    [HttpPost]
    public async Task<IActionResult> Registro(RegistroVM vm)
    {
        vm.Nombre = (vm.Nombre ?? "").Trim();
        vm.Email = (vm.Email ?? "").Trim().ToLowerInvariant();
        vm.Password ??= "";
        vm.Confirmacion ??= "";

        var errores = new List<string>();
        if (vm.Nombre.Length < 3)
            errores.Add("Escribe tu nombre completo (mínimo 3 caracteres).");
        if (!FormatoEmail.IsMatch(vm.Email))
            errores.Add("Escribe un correo electrónico válido.");
        errores.AddRange(ValidarPassword(vm.Password));
        if (vm.Password != vm.Confirmacion)
            errores.Add("Las contraseñas no coinciden.");
        if (errores.Count == 0 && await _db.Usuarios.AnyAsync(u => u.Email == vm.Email))
            errores.Add("Ya existe una cuenta registrada con ese correo.");

        if (errores.Count > 0)
        {
            foreach (var error in errores)
                ModelState.AddModelError(string.Empty, error);
            vm.Password = vm.Confirmacion = "";
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(vm);
        }

        var rolId = await _db.Roles.Where(r => r.Nombre == Roles.PorDefecto).Select(r => r.Id).FirstAsync();
        var usuario = new Usuario { Nombre = vm.Nombre, Email = vm.Email, RolId = rolId, CreadoEn = DateTime.UtcNow };
        usuario.PasswordHash = _hasher.HashPassword(usuario, vm.Password);
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync(Eventos.Registro, $"Cuenta creada con rol {Roles.PorDefecto}", usuario.Id, usuario.Email);
        TempData["Exito"] = "Cuenta creada. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    public static IEnumerable<string> ValidarPassword(string clave)
    {
        if (clave.Length < 8)
            yield return "La contraseña debe tener al menos 8 caracteres.";
        if (!clave.Any(char.IsLetter) || !clave.Any(char.IsDigit))
            yield return "La contraseña debe combinar letras y números.";
    }

    // --------------------------------------------------------------- login

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Inicio");
        return View(new LoginVM { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        var email = (vm.Email ?? "").Trim().ToLowerInvariant();
        var clave = vm.Password ?? "";
        vm.Email = email;
        vm.Password = "";
        var ahora = DateTime.UtcNow;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        if (usuario is null)
        {
            await _bitacora.RegistrarAsync(Eventos.LoginFallido, "Correo no registrado", email: email);
            return VistaLogin(vm, MensajeGenerico, StatusCodes.Status401Unauthorized);
        }

        if (usuario.BloqueadoHasta is { } hasta && hasta > ahora)
        {
            var minutos = (int)Math.Ceiling((hasta - ahora).TotalMinutes);
            await _bitacora.RegistrarAsync(Eventos.LoginBloqueado, "Intento sobre una cuenta bloqueada", usuario.Id, email);
            return VistaLogin(vm, $"Cuenta bloqueada temporalmente por intentos fallidos. Intenta de nuevo en {minutos} minuto(s).",
                StatusCodes.Status423Locked);
        }

        if (_hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, clave) == PasswordVerificationResult.Failed)
        {
            var maximo = _config.GetValue("Seguridad:MaxIntentosLogin", 5);
            var minutosBloqueo = _config.GetValue("Seguridad:MinutosBloqueo", 15);
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= maximo)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = ahora.AddMinutes(minutosBloqueo);
                await _db.SaveChangesAsync();
                await _bitacora.RegistrarAsync(Eventos.CuentaBloqueada, $"{maximo} intentos fallidos seguidos", usuario.Id, email);
                return VistaLogin(vm, $"Demasiados intentos fallidos. La cuenta quedó bloqueada {minutosBloqueo} minutos.",
                    StatusCodes.Status423Locked);
            }
            await _db.SaveChangesAsync();
            await _bitacora.RegistrarAsync(Eventos.LoginFallido,
                $"Contraseña incorrecta (intento {usuario.IntentosFallidos} de {maximo})", usuario.Id, email);
            return VistaLogin(vm, MensajeGenerico, StatusCodes.Status401Unauthorized);
        }

        if (!usuario.Activo)
        {
            await _bitacora.RegistrarAsync(Eventos.LoginRechazado, "Cuenta desactivada", usuario.Id, email);
            return VistaLogin(vm, "Tu cuenta está desactivada. Contacta al administrador.", StatusCodes.Status403Forbidden);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        usuario.UltimoAcceso = ahora;
        await _db.SaveChangesAsync();

        var rol = await _db.Roles.Where(r => r.Id == usuario.RolId).Select(r => r.Nombre).FirstAsync();
        var identidad = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
        }, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        await _bitacora.RegistrarAsync(Eventos.LoginExitoso, $"Rol: {rol}", usuario.Id, email);
        TempData["Exito"] = $"Bienvenido, {usuario.Nombre}.";

        // Solo se aceptan rutas internas para evitar redirecciones a sitios externos.
        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return LocalRedirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Inicio");
    }

    private IActionResult VistaLogin(LoginVM vm, string error, int codigo)
    {
        ModelState.AddModelError(string.Empty, error);
        Response.StatusCode = codigo;
        return View("Login", vm);
    }

    // -------------------------------------------------------------- logout

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _bitacora.RegistrarAsync(Eventos.Logout, "Cierre de sesión");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Exito"] = "Sesión cerrada correctamente.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccesoDenegado() => RedirectToAction("Mostrar", "Error", new { codigo = 403 });
}

using InmoArq.Web.Data;
using InmoArq.Web.Models;
using InmoArq.Web.Seguridad;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace InmoArq.Web;

public class Program
{
    public static void Main(string[] args)
    {
        Construir(args).Run();
    }

    /// <summary>Arma la aplicación. Las pruebas la usan para levantar un servidor con su propia base de datos.</summary>
    public static WebApplication Construir(string[] args, Action<WebApplicationBuilder>? personalizar = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(Program).Assembly.GetName().Name,
        });
        personalizar?.Invoke(builder);

        // MVC con protección CSRF en todos los formularios POST.
        builder.Services.AddControllersWithViews(o =>
        {
            o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        });
        // Muestra acentos y la ñ tal cual en el HTML (sin convertirlos a entidades).
        builder.Services.Configure<WebEncoderOptions>(o =>
            o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

        // Base de datos: SQLite por defecto (no requiere instalar nada) o SQL Server.
        var proveedor = builder.Configuration["BaseDatos:Proveedor"] ?? "Sqlite";
        var cadena = builder.Configuration.GetConnectionString("Default") ?? "Data Source=inmoarq.db";
        builder.Services.AddDbContext<AppDbContext>(o =>
        {
            if (proveedor.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                o.UseSqlServer(cadena);
            else
                o.UseSqlite(cadena);
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<UsuarioActual>();
        builder.Services.AddScoped<ServicioBitacora>();
        builder.Services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

        // Autenticación: cookie de sesión con expiración por inactividad.
        var minutosSesion = builder.Configuration.GetValue("Seguridad:MinutosSesion", 30);
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = "/Cuenta/Login";
                o.LogoutPath = "/Cuenta/Logout";
                o.AccessDeniedPath = "/Cuenta/AccesoDenegado";
                o.ExpireTimeSpan = TimeSpan.FromMinutes(minutosSesion);
                o.SlidingExpiration = true;
                o.Cookie.Name = "InmoArq.Sesion";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Events = new CookieAuthenticationEvents { OnValidatePrincipal = ValidadorSesion.ValidarAsync };
            });

        // Autorización: una política por permiso, evaluada contra los permisos del rol (RBAC).
        builder.Services.AddAuthorization(o =>
        {
            foreach (var (codigo, _, _) in Permisos.Todos)
                o.AddPolicy(codigo, p => p.RequireAuthenticatedUser().AddRequirements(new PermisoRequerido(codigo)));
        });
        builder.Services.AddScoped<IAuthorizationHandler, ManejadorPermisos>();
        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ManejadorResultadoAutorizacion>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            DbInicializador.Inicializar(
                scope.ServiceProvider.GetRequiredService<AppDbContext>(),
                scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>(),
                app.Configuration.GetValue("BaseDatos:DatosDemo", true));
        }

        if (!app.Environment.IsDevelopment())
            app.UseExceptionHandler("/Error/500");
        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllerRoute("default", "{controller=Inicio}/{action=Index}/{id?}");

        return app;
    }
}

using InmoArq.Web.Seguridad;

namespace InmoArq.Tests;

/// <summary>HU-03 · Asignación de roles y permisos.</summary>
public class HU03_RolesTests : PruebaBase
{
    [Fact]
    public async Task CP18_Cambio_de_rol_aplica_de_inmediato()
    {
        using var otro = Servidor.NuevoCliente();
        await otro.LoginComoAsync("Cliente");
        Assert.Equal(403, (await otro.GetAsync("/Admin/Bitacora")).Estado);

        await Cliente.LoginComoAsync("Administrador");
        var id = Servidor.IdUsuario("cliente@inmoarq.do");
        var r = await Cliente.PostAsync($"/Admin/CambiarRol/{id}",
            ClienteWeb.Campos(("rolId", Servidor.IdRol("Administrador").ToString())), "/Admin/Usuarios");
        Assert.Equal(302, r.Estado);

        // La sesión que ya estaba abierta recibe los permisos nuevos sin volver a entrar.
        Assert.Equal(200, (await otro.GetAsync("/Admin/Bitacora")).Estado);
    }

    [Fact]
    public async Task CP19_Quitar_un_permiso_a_un_rol()
    {
        await Cliente.LoginComoAsync("Administrador");
        var agenteId = Servidor.IdRol("Agente inmobiliario");

        // Se envía la matriz actual, dejando al Agente solo con propiedades.ver.
        var campos = Servidor.Bd(db =>
            (from rp in db.RolPermisos join p in db.Permisos on rp.PermisoId equals p.Id
             select new { rp.RolId, p.Codigo }).ToList())
            .Where(x => x.RolId != agenteId)
            .Select(x => new KeyValuePair<string, string>($"rol-{x.RolId}", x.Codigo))
            .Append(new KeyValuePair<string, string>($"rol-{agenteId}", Permisos.PropiedadesVer))
            .ToList();
        Assert.Equal(302, (await Cliente.PostAsync("/Admin/Roles", campos, "/Admin/Roles")).Estado);

        using var agente = Servidor.NuevoCliente();
        await agente.LoginComoAsync("Agente inmobiliario");
        var pagina = await agente.GetAsync("/Propiedades");
        Assert.Equal(200, pagina.Estado);
        Assert.DoesNotContain("Publicar propiedad", pagina.Html);
    }

    [Fact]
    public async Task CP20_Administrador_no_pierde_permisos_criticos()
    {
        await Cliente.LoginComoAsync("Administrador");
        await Cliente.PostAsync("/Admin/Roles", null, "/Admin/Roles");

        var adminId = Servidor.IdRol("Administrador");
        var codigos = Servidor.Bd(db =>
            (from rp in db.RolPermisos join p in db.Permisos on rp.PermisoId equals p.Id
             where rp.RolId == adminId select p.Codigo).ToList());
        Assert.Contains(Permisos.UsuariosGestionar, codigos);
        Assert.Contains(Permisos.RolesGestionar, codigos);
    }

    [Fact]
    public async Task CP21_Admin_no_puede_cambiar_su_propio_rol()
    {
        await Cliente.LoginComoAsync("Administrador");
        var id = Servidor.IdUsuario("admin@inmoarq.do");
        await Cliente.PostAsync($"/Admin/CambiarRol/{id}",
            ClienteWeb.Campos(("rolId", Servidor.IdRol("Cliente").ToString())), "/Admin/Usuarios");

        Assert.Equal("Administrador", Servidor.RolDe("admin@inmoarq.do"));
    }

    [Fact]
    public async Task CP22_Admin_no_puede_desactivarse_a_si_mismo()
    {
        await Cliente.LoginComoAsync("Administrador");
        var id = Servidor.IdUsuario("admin@inmoarq.do");
        await Cliente.PostAsync($"/Admin/CambiarEstado/{id}", null, "/Admin/Usuarios");

        Assert.True(Servidor.Bd(db => db.Usuarios.Single(u => u.Id == id).Activo));
    }

    [Fact]
    public async Task CP23_Desactivar_usuario_cierra_su_sesion()
    {
        using var otro = Servidor.NuevoCliente();
        await otro.LoginComoAsync("Arquitecto");
        Assert.Equal(200, (await otro.GetAsync("/Proyectos")).Estado);

        await Cliente.LoginComoAsync("Administrador");
        await Cliente.PostAsync($"/Admin/CambiarEstado/{Servidor.IdUsuario("arquitecto@inmoarq.do")}", null, "/Admin/Usuarios");

        var r = await otro.GetAsync("/Proyectos");
        Assert.Equal(302, r.Estado);
        Assert.Contains("/Cuenta/Login", r.Ubicacion);
    }

    [Fact]
    public async Task CP24_Crear_rol_nuevo_con_nombre_unico()
    {
        await Cliente.LoginComoAsync("Administrador");
        await Cliente.PostAsync("/Admin/NuevoRol",
            ClienteWeb.Campos(("nombre", "Supervisor de obra"), ("descripcion", "Revisa avances")), "/Admin/Roles");
        await Cliente.PostAsync("/Admin/NuevoRol", ClienteWeb.Campos(("nombre", "supervisor de obra")), "/Admin/Roles");

        Assert.Equal(1, Servidor.Bd(db => db.Roles.Count(r => r.Nombre.ToLower() == "supervisor de obra")));
    }
}

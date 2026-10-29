namespace InmoArq.Tests;

/// <summary>HU-04 · Restricción de funcionalidades según el perfil.</summary>
public class HU04_RestriccionTests : PruebaBase
{
    /// <summary>CP-25 · Matriz de acceso autorizado (200) y no autorizado (403): 4 roles × 5 módulos.</summary>
    [Theory]
    [InlineData("Administrador", "/Propiedades", 200)]
    [InlineData("Administrador", "/Proyectos", 200)]
    [InlineData("Administrador", "/Admin/Usuarios", 200)]
    [InlineData("Administrador", "/Admin/Roles", 200)]
    [InlineData("Administrador", "/Admin/Bitacora", 200)]
    [InlineData("Agente inmobiliario", "/Propiedades", 200)]
    [InlineData("Agente inmobiliario", "/Proyectos", 403)]
    [InlineData("Agente inmobiliario", "/Admin/Usuarios", 403)]
    [InlineData("Agente inmobiliario", "/Admin/Roles", 403)]
    [InlineData("Agente inmobiliario", "/Admin/Bitacora", 403)]
    [InlineData("Arquitecto", "/Propiedades", 200)]
    [InlineData("Arquitecto", "/Proyectos", 200)]
    [InlineData("Arquitecto", "/Admin/Usuarios", 403)]
    [InlineData("Arquitecto", "/Admin/Roles", 403)]
    [InlineData("Arquitecto", "/Admin/Bitacora", 403)]
    [InlineData("Cliente", "/Propiedades", 200)]
    [InlineData("Cliente", "/Proyectos", 200)]
    [InlineData("Cliente", "/Admin/Usuarios", 403)]
    [InlineData("Cliente", "/Admin/Roles", 403)]
    [InlineData("Cliente", "/Admin/Bitacora", 403)]
    public async Task CP25_Matriz_de_acceso(string rol, string ruta, int esperado)
    {
        await Cliente.LoginComoAsync(rol);
        Assert.Equal(esperado, (await Cliente.GetAsync(ruta)).Estado);
    }

    [Fact]
    public async Task CP26_Menu_solo_muestra_modulos_permitidos()
    {
        await Cliente.LoginComoAsync("Cliente");
        var html = (await Cliente.GetAsync("/")).Html;

        Assert.Contains("href=\"/Propiedades\"", html);
        Assert.Contains("href=\"/Proyectos\"", html);
        Assert.DoesNotContain("/Admin/Usuarios", html);
        Assert.DoesNotContain("/Admin/Bitacora", html);
    }

    [Fact]
    public async Task CP27_Cliente_no_puede_publicar_propiedad_ni_forzando_la_URL()
    {
        await Cliente.LoginComoAsync("Cliente");
        Assert.DoesNotContain("Publicar propiedad", (await Cliente.GetAsync("/Propiedades")).Html);

        var r = await Cliente.PostAsync("/Propiedades/Nueva",
            ClienteWeb.Campos(("titulo", "X"), ("ubicacion", "Y"), ("operacion", "Venta"), ("precio", "1")));
        Assert.Equal(403, r.Estado);
        Assert.False(Servidor.Bd(db => db.Propiedades.Any(p => p.Titulo == "X")));
    }

    [Fact]
    public async Task CP28_Agente_si_puede_publicar_propiedad()
    {
        await Cliente.LoginComoAsync("Agente inmobiliario");
        var r = await Cliente.PostAsync("/Propiedades/Nueva",
            ClienteWeb.Campos(("titulo", "Penthouse"), ("ubicacion", "Bella Vista"), ("operacion", "Venta"), ("precio", "18,500,000")));

        Assert.Equal(302, r.Estado);
        Assert.Equal(18_500_000m, Servidor.Bd(db => db.Propiedades.Single(p => p.Titulo == "Penthouse").Precio));
    }

    [Fact]
    public async Task CP29_Cliente_solo_ve_sus_propios_proyectos()
    {
        await Cliente.LoginComoAsync("Cliente");
        var html = (await Cliente.GetAsync("/Proyectos")).Html;

        Assert.Contains("Remodelación de cocina y terraza", html);
        Assert.DoesNotContain("Torre residencial Ensanche Naco", html);
    }

    [Fact]
    public async Task CP30_Arquitecto_ve_todos_los_proyectos()
    {
        await Cliente.LoginComoAsync("Arquitecto");
        var html = (await Cliente.GetAsync("/Proyectos")).Html;

        Assert.Contains("Remodelación de cocina y terraza", html);
        Assert.Contains("Torre residencial Ensanche Naco", html);
    }

    [Fact]
    public async Task CP31_Agente_no_puede_cambiar_roles()
    {
        await Cliente.LoginComoAsync("Agente inmobiliario");
        var id = Servidor.IdUsuario("agente@inmoarq.do");
        var r = await Cliente.PostAsync($"/Admin/CambiarRol/{id}",
            ClienteWeb.Campos(("rolId", Servidor.IdRol("Administrador").ToString())));

        Assert.Equal(403, r.Estado);
        Assert.Equal("Agente inmobiliario", Servidor.RolDe("agente@inmoarq.do"));
    }
}

namespace InmoArq.Tests;

/// <summary>HU-05 · Trazabilidad de las operaciones (bitácora).</summary>
public class HU05_BitacoraTests : PruebaBase
{
    private List<string> Eventos() => Servidor.Bd(db => db.Bitacora.OrderBy(b => b.Id).Select(b => b.Evento).ToList());

    [Fact]
    public async Task CP32_Se_registran_registro_login_y_logout()
    {
        await Cliente.RegistrarAsync();
        await Cliente.LoginAsync("maria@correo.com", "Clave2026");
        await Cliente.LogoutAsync();
        await Cliente.LoginAsync("maria@correo.com", "incorrecta1");

        Assert.Equal(new[] { "REGISTRO", "LOGIN_EXITOSO", "LOGOUT", "LOGIN_FALLIDO" }, Eventos());
    }

    [Fact]
    public async Task CP33_Acceso_denegado_queda_registrado()
    {
        await Cliente.LoginComoAsync("Cliente");
        await Cliente.GetAsync("/Admin/Usuarios");

        var fila = Servidor.Bd(db => db.Bitacora.Single(b => b.Evento == "ACCESO_DENEGADO"));
        Assert.Equal("cliente@inmoarq.do", fila.Email);
        Assert.Contains("/Admin/Usuarios", fila.Detalle);
        Assert.Contains("usuarios.gestionar", fila.Detalle);
    }

    [Fact]
    public async Task CP34_Cambio_de_rol_queda_registrado_con_detalle()
    {
        await Cliente.LoginComoAsync("Administrador");
        await Cliente.PostAsync($"/Admin/CambiarRol/{Servidor.IdUsuario("cliente@inmoarq.do")}",
            ClienteWeb.Campos(("rolId", Servidor.IdRol("Arquitecto").ToString())), "/Admin/Usuarios");

        var fila = Servidor.Bd(db => db.Bitacora.Single(b => b.Evento == "CAMBIO_ROL"));
        Assert.Equal("admin@inmoarq.do", fila.Email);
        Assert.Equal("cliente@inmoarq.do: Cliente → Arquitecto", fila.Detalle);
    }

    [Fact]
    public async Task CP35_Admin_consulta_y_filtra_la_bitacora()
    {
        await Cliente.LoginAsync("cliente@inmoarq.do", "mala12345");
        await Cliente.LoginComoAsync("Administrador");

        var html = (await Cliente.GetAsync("/Admin/Bitacora?evento=LOGIN_FALLIDO")).Html;
        Assert.Contains("cliente@inmoarq.do", html);
        Assert.DoesNotContain(">LOGIN_EXITOSO</span>", html);
    }
}

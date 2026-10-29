namespace InmoArq.Tests;

/// <summary>HU-02 · Inicio y cierre de sesión.</summary>
public class HU02_SesionTests : PruebaBase
{
    private const string MensajeGenerico = "Correo o contraseña incorrectos.";

    [Fact]
    public async Task CP08_Login_correcto_abre_sesion()
    {
        var r = await Cliente.LoginAsync("admin@inmoarq.do", "Admin2026!");
        Assert.Equal(302, r.Estado);
        Assert.Equal("/", r.Ubicacion);

        var inicio = await Cliente.GetAsync("/");
        Assert.Equal(200, inicio.Estado);
        Assert.Contains("Hola, Ana", inicio.Html);
    }

    [Fact]
    public async Task CP09_Contrasena_incorrecta_muestra_mensaje_generico()
    {
        var r = await Cliente.LoginAsync("admin@inmoarq.do", "equivocada1");

        Assert.Equal(401, r.Estado);
        Assert.Contains(MensajeGenerico, r.Html);
    }

    [Fact]
    public async Task CP10_Correo_inexistente_muestra_el_mismo_mensaje()
    {
        var r = await Cliente.LoginAsync("nadie@inmoarq.do", "Cualquiera1");

        Assert.Equal(401, r.Estado);
        Assert.Contains(MensajeGenerico, r.Html);
    }

    [Fact]
    public async Task CP11_Bloqueo_tras_cinco_intentos_fallidos()
    {
        for (var i = 0; i < 4; i++)
            Assert.Equal(401, (await Cliente.LoginAsync("cliente@inmoarq.do", "mala12345")).Estado);
        Assert.Equal(423, (await Cliente.LoginAsync("cliente@inmoarq.do", "mala12345")).Estado);

        // Ni la contraseña correcta entra mientras dura el bloqueo.
        var r = await Cliente.LoginAsync("cliente@inmoarq.do", "Cliente2026!");
        Assert.Equal(423, r.Estado);
        Assert.Contains("bloqueada temporalmente", r.Html);
    }

    [Fact]
    public async Task CP12_Cuenta_desactivada_no_puede_entrar()
    {
        Servidor.Cambiar(db => db.Usuarios.Single(u => u.Email == "agente@inmoarq.do").Activo = false);

        var r = await Cliente.LoginAsync("agente@inmoarq.do", "Agente2026!");
        Assert.Equal(403, r.Estado);
        Assert.Contains("Tu cuenta está desactivada", r.Html);
    }

    [Fact]
    public async Task CP13_Logout_cierra_la_sesion()
    {
        await Cliente.LoginComoAsync("Cliente");

        var r = await Cliente.LogoutAsync();
        Assert.Equal(302, r.Estado);
        Assert.Equal("/Cuenta/Login", r.Ubicacion);

        var despues = await Cliente.GetAsync("/");
        Assert.Equal(302, despues.Estado);
        Assert.Contains("/Cuenta/Login", despues.Ubicacion);
    }

    [Fact]
    public async Task CP14_Logout_solo_acepta_POST()
    {
        await Cliente.LoginComoAsync("Cliente");

        var r = await Cliente.GetAsync("/Cuenta/Logout");
        Assert.Equal(405, r.Estado);
    }

    [Fact]
    public async Task CP15_Sin_sesion_redirige_al_login_y_luego_vuelve()
    {
        var r = await Cliente.GetAsync("/Propiedades");
        Assert.Equal(302, r.Estado);
        Assert.Contains("/Cuenta/Login?ReturnUrl=%2FPropiedades", r.Ubicacion);

        var login = await Cliente.LoginAsync("cliente@inmoarq.do", "Cliente2026!", "/Propiedades");
        Assert.Equal("/Propiedades", login.Ubicacion);
    }

    [Fact]
    public async Task CP16_Redireccion_externa_es_ignorada()
    {
        var r = await Cliente.LoginAsync("cliente@inmoarq.do", "Cliente2026!", "//sitio-malicioso.com");

        Assert.Equal(302, r.Estado);
        Assert.Equal("/", r.Ubicacion);
    }

    [Fact]
    public async Task CP17_Formulario_sin_token_CSRF_es_rechazado()
    {
        var r = await Cliente.PostSinTokenAsync("/Cuenta/Login",
            ClienteWeb.Campos(("Email", "admin@inmoarq.do"), ("Password", "Admin2026!")));

        Assert.Equal(400, r.Estado);
    }
}

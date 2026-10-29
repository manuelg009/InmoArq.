namespace InmoArq.Tests;

/// <summary>HU-01 · Registro de usuarios.</summary>
public class HU01_RegistroTests : PruebaBase
{
    [Fact]
    public async Task CP01_Registro_exitoso_asigna_rol_Cliente()
    {
        var r = await Cliente.RegistrarAsync();

        Assert.Equal(302, r.Estado);
        Assert.Equal("/Cuenta/Login", r.Ubicacion);
        Assert.Equal("Cliente", Servidor.RolDe("maria@correo.com"));
    }

    [Fact]
    public async Task CP02_Contrasena_se_guarda_cifrada()
    {
        await Cliente.RegistrarAsync(clave: "Secreta123");

        var hash = Servidor.Bd(db => db.Usuarios.Single(u => u.Email == "maria@correo.com").PasswordHash);
        Assert.DoesNotContain("Secreta123", hash);
        Assert.True(hash.Length > 40, "El hash debe ser largo (PBKDF2 con sal).");
    }

    [Fact]
    public async Task CP03_Correo_duplicado_es_rechazado()
    {
        await Cliente.RegistrarAsync();
        var r = await Cliente.RegistrarAsync(nombre: "Otra Persona", email: "MARIA@correo.com");

        Assert.Equal(400, r.Estado);
        Assert.Contains("Ya existe una cuenta registrada con ese correo.", r.Html);
        Assert.Equal(1, Servidor.Bd(db => db.Usuarios.Count(u => u.Email == "maria@correo.com")));
    }

    [Fact]
    public async Task CP04_Contrasena_debil_es_rechazada()
    {
        var r = await Cliente.RegistrarAsync(clave: "corta");

        Assert.Equal(400, r.Estado);
        Assert.Contains("al menos 8 caracteres", r.Html);
        Assert.Contains("combinar letras y números", r.Html);
    }

    [Fact]
    public async Task CP05_Confirmacion_distinta_es_rechazada()
    {
        var r = await Cliente.RegistrarAsync(clave: "Clave2026", confirmacion: "Clave2027");

        Assert.Equal(400, r.Estado);
        Assert.Contains("Las contraseñas no coinciden.", r.Html);
    }

    [Fact]
    public async Task CP06_Correo_invalido_es_rechazado()
    {
        var r = await Cliente.RegistrarAsync(email: "no-es-correo");

        Assert.Equal(400, r.Estado);
        Assert.Contains("Escribe un correo electrónico válido.", r.Html);
    }

    [Fact]
    public async Task CP07_Usuario_registrado_puede_iniciar_sesion()
    {
        await Cliente.RegistrarAsync();
        var r = await Cliente.LoginAsync("maria@correo.com", "Clave2026");

        Assert.Equal(302, r.Estado);
        Assert.Equal("/", r.Ubicacion);
    }
}

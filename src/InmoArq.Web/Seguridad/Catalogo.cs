namespace InmoArq.Web.Seguridad;

/// <summary>Catálogo de permisos del sistema. Cada código es también una política de autorización.</summary>
public static class Permisos
{
    public const string PropiedadesVer = "propiedades.ver";
    public const string PropiedadesGestionar = "propiedades.gestionar";
    public const string ProyectosVer = "proyectos.ver";
    public const string ProyectosGestionar = "proyectos.gestionar";
    public const string UsuariosGestionar = "usuarios.gestionar";
    public const string RolesGestionar = "roles.gestionar";
    public const string BitacoraVer = "bitacora.ver";

    public static readonly (string Codigo, string Descripcion, string Modulo)[] Todos =
    {
        (PropiedadesVer, "Ver el catálogo de propiedades", "Propiedades"),
        (PropiedadesGestionar, "Publicar propiedades en el catálogo", "Propiedades"),
        (ProyectosVer, "Ver proyectos de diseño arquitectónico", "Diseño arquitectónico"),
        (ProyectosGestionar, "Crear y ver todos los proyectos de diseño", "Diseño arquitectónico"),
        (UsuariosGestionar, "Administrar usuarios y asignarles roles", "Administración"),
        (RolesGestionar, "Configurar roles y permisos", "Administración"),
        (BitacoraVer, "Consultar la bitácora de accesos", "Administración"),
    };
}

public static class Roles
{
    public const string Administrador = "Administrador";
    public const string Agente = "Agente inmobiliario";
    public const string Arquitecto = "Arquitecto";
    public const string Cliente = "Cliente";

    /// <summary>Rol que reciben las cuentas creadas desde el registro público.</summary>
    public const string PorDefecto = Cliente;

    /// <summary>Permisos que el Administrador nunca puede perder (evita dejar el sistema sin acceso).</summary>
    public static readonly string[] CriticosAdmin = { Permisos.UsuariosGestionar, Permisos.RolesGestionar };

    public static readonly (string Nombre, string Descripcion, string[] Permisos)[] Iniciales =
    {
        (Administrador, "Control total de la plataforma", Permisos.Todos.Select(p => p.Codigo).ToArray()),
        (Agente, "Publica y gestiona propiedades del catálogo",
            new[] { Permisos.PropiedadesVer, Permisos.PropiedadesGestionar }),
        (Arquitecto, "Gestiona los proyectos de diseño arquitectónico",
            new[] { Permisos.PropiedadesVer, Permisos.ProyectosVer, Permisos.ProyectosGestionar }),
        (Cliente, "Consulta propiedades y el avance de sus propios proyectos",
            new[] { Permisos.PropiedadesVer, Permisos.ProyectosVer }),
    };
}

/// <summary>Módulos del menú. Solo se muestran los que el rol del usuario tiene permitidos.</summary>
public record Modulo(string Titulo, string Descripcion, string Controlador, string Accion, string Permiso);

public static class Modulos
{
    public static readonly Modulo[] Todos =
    {
        new("Propiedades", "Catálogo de inmuebles en venta y alquiler.", "Propiedades", "Index", Permisos.PropiedadesVer),
        new("Diseño arquitectónico", "Proyectos de diseño y su etapa actual.", "Proyectos", "Index", Permisos.ProyectosVer),
        new("Usuarios", "Alta, roles y estado de las cuentas.", "Admin", "Usuarios", Permisos.UsuariosGestionar),
        new("Roles y permisos", "Qué puede hacer cada rol.", "Admin", "Roles", Permisos.RolesGestionar),
        new("Bitácora", "Registro de accesos y cambios de seguridad.", "Admin", "Bitacora", Permisos.BitacoraVer),
    };
}

/// <summary>Eventos que se guardan en la bitácora.</summary>
public static class Eventos
{
    public const string Registro = "REGISTRO";
    public const string LoginExitoso = "LOGIN_EXITOSO";
    public const string LoginFallido = "LOGIN_FALLIDO";
    public const string LoginBloqueado = "LOGIN_BLOQUEADO";
    public const string LoginRechazado = "LOGIN_RECHAZADO";
    public const string CuentaBloqueada = "CUENTA_BLOQUEADA";
    public const string Logout = "LOGOUT";
    public const string AccesoDenegado = "ACCESO_DENEGADO";
    public const string CambioRol = "CAMBIO_ROL";
    public const string CambioPermisos = "CAMBIO_PERMISOS";
    public const string RolCreado = "ROL_CREADO";
    public const string UsuarioActivado = "USUARIO_ACTIVADO";
    public const string UsuarioDesactivado = "USUARIO_DESACTIVADO";
    public const string PropiedadPublicada = "PROPIEDAD_PUBLICADA";
    public const string ProyectoCreado = "PROYECTO_CREADO";
}

namespace InmoArq.Web.Models;

/// <summary>Cuenta de acceso a la plataforma.</summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    /// <summary>Siempre en minúsculas para que sea único sin importar mayúsculas.</summary>
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public int RolId { get; set; }
    public bool Activo { get; set; } = true;
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadoHasta { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

public class Rol
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
}

public class Permiso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string Modulo { get; set; } = "";
}

/// <summary>Relación muchos a muchos entre roles y permisos.</summary>
public class RolPermiso
{
    public int RolId { get; set; }
    public int PermisoId { get; set; }
}

/// <summary>Trazabilidad: cada operación de seguridad queda registrada.</summary>
public class RegistroBitacora
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public int? UsuarioId { get; set; }
    public string? Email { get; set; }
    public string Evento { get; set; } = "";
    public string Detalle { get; set; } = "";
    public string? Ip { get; set; }
}

public class Propiedad
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Ubicacion { get; set; } = "";
    public string Operacion { get; set; } = "";
    public decimal Precio { get; set; }
    public int? PublicadoPorId { get; set; }
    public DateTime CreadoEn { get; set; }
}

public class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Etapa { get; set; } = "";
    public int? ClienteId { get; set; }
    public int? ArquitectoId { get; set; }
    public DateTime CreadoEn { get; set; }
}

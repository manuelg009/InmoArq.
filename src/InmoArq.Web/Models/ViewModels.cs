namespace InmoArq.Web.Models;

public class RegistroVM
{
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Confirmacion { get; set; } = "";
}

public class LoginVM
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public record FilaUsuario(Usuario Usuario, string Rol);

public record FilaRol(Rol Rol, int TotalUsuarios);

public class RolesVM
{
    public List<FilaRol> Roles { get; set; } = new();
    public List<Permiso> Permisos { get; set; } = new();
    public HashSet<(int RolId, string Codigo)> Asignados { get; set; } = new();
}

public record FilaPropiedad(Propiedad Propiedad, string? Autor);

public record FilaProyecto(Proyecto Proyecto, string? Cliente, string? Arquitecto);

public class ProyectosVM
{
    public List<FilaProyecto> Proyectos { get; set; } = new();
    public List<Usuario> Clientes { get; set; } = new();
    public bool VeTodos { get; set; }
}

public class BitacoraVM
{
    public List<RegistroBitacora> Registros { get; set; } = new();
    public List<string> Eventos { get; set; } = new();
    public string? Filtro { get; set; }
}

public record ErrorVM(int Codigo, string Titulo, string Mensaje);

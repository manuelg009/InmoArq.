using InmoArq.Web.Models;
using InmoArq.Web.Seguridad;
using Microsoft.AspNetCore.Identity;

namespace InmoArq.Web.Data;

/// <summary>Crea la base de datos y carga roles, permisos y usuarios de prueba la primera vez.</summary>
public static class DbInicializador
{
    public static readonly (string Nombre, string Email, string Clave, string Rol)[] UsuariosDemo =
    {
        ("Ana Administradora", "admin@inmoarq.do", "Admin2026!", Roles.Administrador),
        ("Pedro Agente", "agente@inmoarq.do", "Agente2026!", Roles.Agente),
        ("Laura Arquitecta", "arquitecto@inmoarq.do", "Arqui2026!", Roles.Arquitecto),
        ("Carlos Cliente", "cliente@inmoarq.do", "Cliente2026!", Roles.Cliente),
    };

    public static void Inicializar(AppDbContext db, IPasswordHasher<Usuario> hasher, bool datosDemo)
    {
        db.Database.EnsureCreated();
        if (db.Roles.Any())
            return;

        foreach (var (codigo, descripcion, modulo) in Permisos.Todos)
            db.Permisos.Add(new Permiso { Codigo = codigo, Descripcion = descripcion, Modulo = modulo });
        foreach (var (nombre, descripcion, _) in Roles.Iniciales)
            db.Roles.Add(new Rol { Nombre = nombre, Descripcion = descripcion });
        db.SaveChanges();

        var permisos = db.Permisos.ToDictionary(p => p.Codigo, p => p.Id);
        var roles = db.Roles.ToDictionary(r => r.Nombre, r => r.Id);
        foreach (var (nombre, _, codigos) in Roles.Iniciales)
            foreach (var codigo in codigos)
                db.RolPermisos.Add(new RolPermiso { RolId = roles[nombre], PermisoId = permisos[codigo] });
        db.SaveChanges();

        if (!datosDemo)
            return;

        var ahora = DateTime.UtcNow;
        var usuarios = new Dictionary<string, Usuario>();
        foreach (var (nombre, email, clave, rol) in UsuariosDemo)
        {
            var u = new Usuario { Nombre = nombre, Email = email, RolId = roles[rol], CreadoEn = ahora };
            u.PasswordHash = hasher.HashPassword(u, clave);
            db.Usuarios.Add(u);
            usuarios[rol] = u;
        }
        db.SaveChanges();

        var agente = usuarios[Roles.Agente].Id;
        db.Propiedades.AddRange(
            new Propiedad { Titulo = "Apartamento de 3 habitaciones", Ubicacion = "Piantini, Santo Domingo", Operacion = "Venta", Precio = 9_850_000, PublicadoPorId = agente, CreadoEn = ahora },
            new Propiedad { Titulo = "Villa con piscina", Ubicacion = "Cap Cana, Punta Cana", Operacion = "Venta", Precio = 32_500_000, PublicadoPorId = agente, CreadoEn = ahora },
            new Propiedad { Titulo = "Casa familiar de 2 niveles", Ubicacion = "Los Jardines, Santiago", Operacion = "Venta", Precio = 12_900_000, PublicadoPorId = agente, CreadoEn = ahora },
            new Propiedad { Titulo = "Local comercial 120 m²", Ubicacion = "Naco, Santo Domingo", Operacion = "Alquiler", Precio = 95_000, PublicadoPorId = agente, CreadoEn = ahora });

        var arquitecto = usuarios[Roles.Arquitecto].Id;
        db.Proyectos.AddRange(
            new Proyecto { Nombre = "Remodelación de cocina y terraza", Etapa = "Planos", ClienteId = usuarios[Roles.Cliente].Id, ArquitectoId = arquitecto, CreadoEn = ahora },
            new Proyecto { Nombre = "Casa de playa en Las Terrenas", Etapa = "Anteproyecto", ArquitectoId = arquitecto, CreadoEn = ahora },
            new Proyecto { Nombre = "Torre residencial Ensanche Naco", Etapa = "Renders 3D", ArquitectoId = arquitecto, CreadoEn = ahora });
        db.SaveChanges();
    }
}

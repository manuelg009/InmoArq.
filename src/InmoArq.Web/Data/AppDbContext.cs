using InmoArq.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InmoArq.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<Rol> Roles { get; set; } = null!;
    public DbSet<Permiso> Permisos { get; set; } = null!;
    public DbSet<RolPermiso> RolPermisos { get; set; } = null!;
    public DbSet<RegistroBitacora> Bitacora { get; set; } = null!;
    public DbSet<Propiedad> Propiedades { get; set; } = null!;
    public DbSet<Proyecto> Proyectos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.Property(u => u.Nombre).HasMaxLength(120).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne<Rol>().WithMany().HasForeignKey(u => u.RolId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Rol>(e =>
        {
            e.Property(r => r.Nombre).HasMaxLength(60).IsRequired();
            e.HasIndex(r => r.Nombre).IsUnique();
        });

        modelBuilder.Entity<Permiso>(e =>
        {
            e.Property(p => p.Codigo).HasMaxLength(60).IsRequired();
            e.HasIndex(p => p.Codigo).IsUnique();
        });

        modelBuilder.Entity<RolPermiso>(e =>
        {
            e.HasKey(rp => new { rp.RolId, rp.PermisoId });
            e.HasOne<Rol>().WithMany().HasForeignKey(rp => rp.RolId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Permiso>().WithMany().HasForeignKey(rp => rp.PermisoId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RegistroBitacora>(e =>
        {
            e.Property(b => b.Evento).HasMaxLength(40).IsRequired();
            e.HasIndex(b => b.Fecha);
            e.HasIndex(b => b.Evento);
        });

        modelBuilder.Entity<Propiedad>(e =>
        {
            e.Property(p => p.Precio).HasPrecision(18, 2);
        });
    }
}

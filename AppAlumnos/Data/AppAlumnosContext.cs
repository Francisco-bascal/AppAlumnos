using AppAlumnos.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AppAlumnos.Data;

public class AppAlumnosContext : IdentityDbContext<Usuario>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AppAlumnosContext(DbContextOptions<AppAlumnosContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<Materia> Materias { get; set; }
    public DbSet<Cursada> Cursadas { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var usuarioId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.FechaCreacion = DateTime.Now;
                    entry.Entity.UsuarioCreacionId = usuarioId;
                    entry.Entity.FechaModificacion = DateTime.Now;
                    entry.Entity.UsuarioModificacionId = usuarioId;
                    break;
                case EntityState.Modified:
                    entry.Entity.FechaModificacion = DateTime.Now;
                    entry.Entity.UsuarioModificacionId = usuarioId;
                    entry.Property(nameof(AuditableEntity.FechaCreacion)).IsModified = false;
                    entry.Property(nameof(AuditableEntity.UsuarioCreacionId)).IsModified = false;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);

        builder.Entity<Cursada>()
            .HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Cursada>()
            .HasOne(c => c.Materia)
            .WithMany()
            .HasForeignKey(c => c.MateriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Cursada>()
            .Property(c => c.Nota)
            .HasPrecision(4, 1);

        builder.Entity<Cursada>()
            .HasIndex(c => new { c.UsuarioId, c.MateriaId, c.AnioLectivo })
            .IsUnique();
    }
}
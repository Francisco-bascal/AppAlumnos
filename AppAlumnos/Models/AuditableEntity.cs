using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models;

public abstract class AuditableEntity
{
    [Display(Name = "Fecha de Creación")]
    public DateTime FechaCreacion { get; set; }

    public string? UsuarioCreacionId { get; set; }

    [Display(Name = "Fecha de Modificación")]
    public DateTime FechaModificacion { get; set; }

    public string? UsuarioModificacionId { get; set; }
}
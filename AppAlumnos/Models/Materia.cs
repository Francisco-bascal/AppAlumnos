using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models
{
    public class Materia : AuditableEntity
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El nombre de la materia es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
        [Display(Name = "Nombre de la Materia")]
        public string Nombre { get; set; } = string.Empty;
        [Display(Name = "Año Cursado")]
        [Range(1, 6, ErrorMessage = "El año debe estar entre 1 y 6.")]
        public int Anio { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.DTOs;

public class MateriaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string? DocenteId { get; set; }
    public string? DocenteNombre { get; set; }
}

public class GuardarMateriaDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la materia es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre de la Materia")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Año Cursado")]
    [Range(1, 6, ErrorMessage = "El año debe estar entre 1 y 6.")]
    public int Anio { get; set; }

    [Display(Name = "Docente")]
    public string? DocenteId { get; set; }
}
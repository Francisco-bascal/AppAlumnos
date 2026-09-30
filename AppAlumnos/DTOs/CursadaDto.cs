using AppAlumnos.Models;
using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.DTOs;

public class CursadaDto
{
    public int Id { get; set; }
    public string UsuarioId { get; set; } = null!;
    public string AlumnoNombre { get; set; } = string.Empty;
    public int MateriaId { get; set; }
    public string MateriaNombre { get; set; } = string.Empty;
    public int AnioLectivo { get; set; }
    public decimal? Nota { get; set; }
    public EstadoCursada Estado { get; set; }
}

public class GuardarCursadaDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un alumno.")]
    [Display(Name = "Alumno")]
    public string UsuarioId { get; set; } = null!;

    [Required(ErrorMessage = "Debe seleccionar una materia.")]
    [Display(Name = "Materia")]
    public int MateriaId { get; set; }

    [Display(Name = "Año Lectivo")]
    [Range(2000, 2100, ErrorMessage = "Ingrese un año lectivo válido.")]
    public int AnioLectivo { get; set; } = DateTime.Today.Year;
}

public class NotasDto
{
    public int Id { get; set; }
    public int MateriaId { get; set; }
    public string? AlumnoNombre { get; set; }
    public string? MateriaNombre { get; set; }

    [Display(Name = "Nota")]
    public string? Nota { get; set; }

    [Display(Name = "Estado")]
    public EstadoCursada Estado { get; set; }
}
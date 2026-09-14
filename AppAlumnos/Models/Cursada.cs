using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models;

public class Cursada : AuditableEntity
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un alumno.")]
    [Display(Name = "Alumno")]
    public string UsuarioId { get; set; } = null!;

    public Usuario Usuario { get; set; } = null!;

    [Required(ErrorMessage = "Debe seleccionar una materia.")]
    [Display(Name = "Materia")]
    public int MateriaId { get; set; }

    public Materia Materia { get; set; } = null!;

    [Display(Name = "Año Lectivo")]
    [Range(2000, 2100, ErrorMessage = "Ingrese un año lectivo válido.")]
    public int AnioLectivo { get; set; } = DateTime.Today.Year;

    [Display(Name = "Nota")]
    [Range(1, 10, ErrorMessage = "La nota debe estar entre 1 y 10.")]
    public decimal? Nota { get; set; }

    [Display(Name = "Estado")]
    public EstadoCursada Estado { get; set; } = EstadoCursada.Cursando;
}

public enum EstadoCursada
{
    [Display(Name = "Cursando")]
    Cursando,
    [Display(Name = "Regular")]
    Regular,
    [Display(Name = "Aprobada")]
    Aprobada,
    [Display(Name = "Libre")]
    Libre,
    [Display(Name = "Desaprobada")]
    Desaprobada
}
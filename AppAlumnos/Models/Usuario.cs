using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.Models;

// Add profile data for application users by adding properties to the User class
public class Usuario : IdentityUser
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [Display(Name = "Nombre")]
    [StringLength(50, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
    public string Nombre { get; set; } = null!;
    [Required(ErrorMessage = "El apellido es obligatorio")]
    [Display(Name = "Apellido")]
    [StringLength(50, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
    public string Apellido { get; set; } = null!;
    [Required(ErrorMessage = "El DNI es obligatorio")]
    [Display(Name = "DNI")]
    [Range(1000000, 99999999, ErrorMessage = "Ingrese un número de DNI válido(entre 7 y 8 dígitos).")]
    public int Dni { get; set; }
    [Display(Name = "Legajo")]
    [StringLength(20, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
    public string Legajo { get; set; } = null!;
    [Required(ErrorMessage = "El Estado es obligatorio")]
    [Display(Name = "Estado")]
    public bool Estado { get; set; }
    [Display(Name = "Fecha de Alta")]
    public DateTime FechaAlta { get; set; }
}
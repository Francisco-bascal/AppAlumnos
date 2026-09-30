using System.ComponentModel.DataAnnotations;

namespace AppAlumnos.DTOs;

public class UsuarioDto
{
    public string Id { get; set; } = null!;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Dni { get; set; }
    public string Legajo { get; set; } = string.Empty;
    public string Rol { get; set; } = "Alumno";
    public bool Estado { get; set; }
    public DateTime FechaAlta { get; set; }
    public string? RutaFoto { get; set; }
}

public class UsuarioFormDto
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [Display(Name = "Nombre")]
    [StringLength(50, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [Display(Name = "Apellido")]
    [StringLength(50, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
    public string Apellido { get; set; } = null!;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Ingrese un email válido")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "El DNI es obligatorio")]
    [Display(Name = "DNI")]
    [Range(1000000, 99999999, ErrorMessage = "Ingrese un número de DNI válido(entre 7 y 8 dígitos).")]
    public int Dni { get; set; }

    [Display(Name = "Legajo")]
    [StringLength(20, ErrorMessage = "El {0} no puede superar los {1} caracteres.")]
    public string Legajo { get; set; } = "-";

    [Required(ErrorMessage = "Debe seleccionar un rol.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "Alumno";

    [Display(Name = "Estado")]
    public bool Estado { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string? ConfirmPassword { get; set; }
}

public class CrearUsuarioDto : UsuarioFormDto
{
}

public class EditarUsuarioDto : UsuarioFormDto
{
}

public class UsuarioOpcionDto
{
    public string Id { get; set; } = null!;
    public string NombreCompleto { get; set; } = string.Empty;
}
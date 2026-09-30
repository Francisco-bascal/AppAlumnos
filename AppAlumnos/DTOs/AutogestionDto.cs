using AppAlumnos.Models;

namespace AppAlumnos.DTOs;

public class MateriaDisponibleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Anio { get; set; }
}

public class CursadaHistoriaDto
{
    public string MateriaNombre { get; set; } = string.Empty;
    public int AnioLectivo { get; set; }
    public decimal? Nota { get; set; }
    public EstadoCursada Estado { get; set; }
}

public class InscriptoDto
{
    public int Id { get; set; }
    public string AlumnoNombre { get; set; } = string.Empty;
    public int AnioLectivo { get; set; }
    public decimal? Nota { get; set; }
    public EstadoCursada Estado { get; set; }
}

public class CertificadoInfoDto
{
    public bool EsRegular { get; set; }
    public int AnioActual { get; set; }
    public int MateriasAprobadas { get; set; }
}

public class PerfilUsuarioDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Dni { get; set; }
    public string Legajo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? RutaFoto { get; set; }
}

public class MateriaAprobadaDto
{
    public string Nombre { get; set; } = string.Empty;
    public decimal Nota { get; set; }
    public int Anio { get; set; }
}

public class InscriptoNotasDto
{
    public string ApellidoNombre { get; set; } = string.Empty;
    public int AnioLectivo { get; set; }
    public decimal? Nota { get; set; }
    public string Estado { get; set; } = string.Empty;
}
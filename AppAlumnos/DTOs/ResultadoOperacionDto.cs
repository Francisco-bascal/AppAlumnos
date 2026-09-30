namespace AppAlumnos.DTOs;

public class ResultadoOperacionDto
{
    public bool Ok { get; set; }
    public string? Mensaje { get; set; }
    public string? RutaRelativa { get; set; }

    public ResultadoOperacionDto(bool ok, string? mensaje = null)
    {
        Ok = ok;
        Mensaje = mensaje;
    }
}
namespace AppAlumnos.Exceptions;

/// <summary>
/// Se lanza cuando un documento no puede generarse. Permite a los controladores
/// devolver un mensaje comprensible en lugar de una respuesta 500 opaca.
/// </summary>
public class FalloGeneracionDocumentoException : Exception
{
    public FalloGeneracionDocumentoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
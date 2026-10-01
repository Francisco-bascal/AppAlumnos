using AppAlumnos.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AppAlumnos.Services;

public class ArchivoService
{
    private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] MimeTypesPermitidos = { "image/jpeg", "image/png", "image/webp" };
    private const int LongitudMinimaCabecera = 12;

    public const long TamanoMaximoBytes = 2 * 1024 * 1024;

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ArchivoService> _logger;

    public ArchivoService(IWebHostEnvironment environment, ILogger<ArchivoService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    // Además de extensión y MIME, se validan los magic bytes del archivo porque ambos
    // valores los declara el cliente y pueden falsearse para subir contenido no permitido.
    public async Task<ResultadoOperacionDto> GuardarAvatarAsync(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            return new ResultadoOperacionDto(false, "Seleccioná una imagen.");
        }

        if (archivo.Length > TamanoMaximoBytes)
        {
            return new ResultadoOperacionDto(false, "La imagen supera el tamaño máximo de 2 MB.");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (!ExtensionesPermitidas.Contains(extension))
        {
            return new ResultadoOperacionDto(false, "Formato no permitido. Usá JPG, PNG o WebP.");
        }

        var mimeType = archivo.ContentType.ToLowerInvariant();
        if (!MimeTypesPermitidos.Contains(mimeType))
        {
            return new ResultadoOperacionDto(false, "El tipo de archivo no es una imagen válida.");
        }

        byte[] contenido;
        try
        {
            using var stream = new MemoryStream();
            await archivo.CopyToAsync(stream);
            contenido = stream.ToArray();
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "No se pudo leer el archivo de avatar enviado.");
            return new ResultadoOperacionDto(false, "No se pudo leer el archivo enviado.");
        }

        if (!EsContenidoValido(contenido, extension))
        {
            return new ResultadoOperacionDto(false, "El contenido del archivo no es una imagen válida.");
        }

        if (string.IsNullOrEmpty(_environment.WebRootPath))
        {
            _logger.LogError("No se puede guardar el avatar porque WebRootPath no está configurado.");
            return new ResultadoOperacionDto(false, "No se pudo guardar la imagen en el servidor.");
        }

        var carpeta = Path.Combine(_environment.WebRootPath, "images", "avatars");
        var nombreArchivo = $"{Guid.NewGuid()}{extension}";
        var rutaFisica = Path.Combine(carpeta, nombreArchivo);

        try
        {
            Directory.CreateDirectory(carpeta);
            await File.WriteAllBytesAsync(rutaFisica, contenido);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "No se pudo escribir el archivo de avatar en {Ruta}.", rutaFisica);
            EliminarSiExiste(rutaFisica);
            return new ResultadoOperacionDto(false, "No se pudo guardar la imagen en el servidor.");
        }

        var resultado = new ResultadoOperacionDto(true, "Imagen guardada correctamente.");
        resultado.RutaRelativa = $"/images/avatars/{nombreArchivo}";
        return resultado;
    }

    // Exigir el prefijo images/avatars/ actúa como guard contra path traversal: solo
    // se borran archivos dentro del directorio de avatares, nunca rutas arbitrarias.
    public void EliminarAvatar(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)) return;

        if (!rutaRelativa.Contains("images/avatars/", StringComparison.OrdinalIgnoreCase)) return;

        if (string.IsNullOrEmpty(_environment.WebRootPath)) return;

        try
        {
            var rutaFisica = Path.GetFullPath(Path.Combine(
                _environment.WebRootPath,
                rutaRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

            EliminarSiExiste(rutaFisica);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "No se pudo eliminar el avatar {Ruta}.", rutaRelativa);
        }
    }

    private void EliminarSiExiste(string rutaFisica)
    {
        try
        {
            if (File.Exists(rutaFisica))
            {
                File.Delete(rutaFisica);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "No se pudo eliminar el archivo {Ruta}.", rutaFisica);
        }
    }

    private static bool EsContenidoValido(byte[] bytes, string extension)
    {
        if (bytes.Length < LongitudMinimaCabecera) return false;

        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
                return bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
            case ".png":
                return bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47;
            case ".webp":
                return bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46
                    && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50;
            default:
                return false;
        }
    }
}
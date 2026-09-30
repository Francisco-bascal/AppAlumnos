using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AppAlumnos.Services
{
    public class ArchivoService
    {
            private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };
            private static readonly string[] MimeTypesPermitidos = { "image/jpeg", "image/png", "image/webp" };
            private const long TamanoMaximoBytes = 2 * 1024 * 1024;

            private readonly IWebHostEnvironment _environment;

            public ArchivoService(IWebHostEnvironment environment)
            {
                _environment = environment;
            }

            public async Task<(bool ok, string mensaje, string? rutaRelativa)> GuardarAvatarAsync(IFormFile archivo)
            {
                if (archivo == null || archivo.Length == 0)
                {
                    return (false, "Seleccioná una imagen.", null);
                }

                if (archivo.Length > TamanoMaximoBytes)
                {
                    return (false, "La imagen supera el tamaño máximo de 2 MB.", null);
                }

                var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (!ExtensionesPermitidas.Contains(extension))
                {
                    return (false, "Formato no permitido. Usá JPG, PNG o WebP.", null);
                }

                var mimeType = archivo.ContentType.ToLowerInvariant();
                if (!MimeTypesPermitidos.Contains(mimeType))
                {
                    return (false, "El tipo de archivo no es una imagen válida.", null);
                }

                using var stream = new MemoryStream();
                await archivo.CopyToAsync(stream);
                var bytes = stream.ToArray();

                if (!EsContenidoValido(bytes, extension))
                {
                    return (false, "El contenido del archivo no es una imagen válida.", null);
                }

                var carpeta = Path.Combine(_environment.WebRootPath, "images", "avatars");
                Directory.CreateDirectory(carpeta);

                var nombreArchivo = $"{Guid.NewGuid()}{extension}";
                var rutaFisica = Path.Combine(carpeta, nombreArchivo);
                await File.WriteAllBytesAsync(rutaFisica, bytes);

                return (true, "Imagen guardada correctamente.", $"/images/avatars/{nombreArchivo}");
            }

            public void EliminarAvatar(string? rutaRelativa)
            {
                if (string.IsNullOrWhiteSpace(rutaRelativa)) return;

                if (!rutaRelativa.Contains("images/avatars/", StringComparison.OrdinalIgnoreCase)) return;

                var rutaFisica = Path.GetFullPath(Path.Combine(
                    _environment.WebRootPath,
                    rutaRelativa.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));

                if (File.Exists(rutaFisica))
                {
                    File.Delete(rutaFisica);
                }
            }

            private static bool EsContenidoValido(byte[] bytes, string extension)
            {
                if (bytes.Length < 12) return false;

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
    }
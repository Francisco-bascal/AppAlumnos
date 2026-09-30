using System.Text;
using Microsoft.Extensions.Logging;

namespace AppAlumnos.Logging;

/// <summary>
/// Escribe los registros de la aplicación en un archivo diario.
/// Existe para que un cierre abrupto del proceso deje evidencia en disco:
/// la salida de consola de Visual Studio se pierde cuando el proceso muere.
/// </summary>
public sealed class ArchivoLogProvider : ILoggerProvider
{
    private const string PrefijoCategoriasIgnoradas = "Microsoft.EntityFrameworkCore.Database.Command";

    private readonly string _rutaArchivo;
    private readonly LogLevel _nivelMinimo;
    private readonly object _sincronizacion = new();

    public ArchivoLogProvider(string rutaArchivo, LogLevel nivelMinimo)
    {
        _rutaArchivo = rutaArchivo;
        _nivelMinimo = nivelMinimo;

        var carpeta = Path.GetDirectoryName(rutaArchivo);
        if (!string.IsNullOrEmpty(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }
    }

    public ILogger CreateLogger(string categoryName) => new ArchivoLogger(this, categoryName);

    public void Dispose()
    {
    }

    private bool EstaHabilitado(string categoria, LogLevel nivel)
    {
        if (nivel < _nivelMinimo || nivel == LogLevel.None)
        {
            return false;
        }

        // Las sentencias SQL son muy verbosas a nivel Information y solo interesan
        // si muestran un problema, es decir, a partir de Warning.
        return nivel > LogLevel.Information || !categoria.StartsWith(PrefijoCategoriasIgnoradas, StringComparison.Ordinal);
    }

    private void Escribir(string linea)
    {
        try
        {
            lock (_sincronizacion)
            {
                File.AppendAllText(_rutaArchivo, linea + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (IOException)
        {
            // Un archivo de log inaccesible nunca debe impedir que la aplicación funcione.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class ArchivoLogger : ILogger
    {
        private readonly ArchivoLogProvider _proveedor;
        private readonly string _categoria;

        public ArchivoLogger(ArchivoLogProvider proveedor, string categoria)
        {
            _proveedor = proveedor;
            _categoria = categoria;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => _proveedor.EstaHabilitado(_categoria, logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{logLevel}] {_categoria}: {formatter(state, exception)}";

            if (exception != null)
            {
                linea += Environment.NewLine + exception;
            }

            _proveedor.Escribir(linea);
        }
    }
}
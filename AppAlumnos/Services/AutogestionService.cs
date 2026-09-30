using AppAlumnos.Data;
using AppAlumnos.DTOs;
using AppAlumnos.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AppAlumnos.Services;

public class AutogestionService
{
    private readonly AppAlumnosContext _contexto;
    private readonly UserManager<Usuario> _userManager;
    private readonly ArchivoService _archivoService;
    private readonly CursadaService _cursadaService;

    public AutogestionService(
        AppAlumnosContext contexto,
        UserManager<Usuario> userManager,
        ArchivoService archivoService,
        CursadaService cursadaService)
    {
        _contexto = contexto;
        _userManager = userManager;
        _archivoService = archivoService;
        _cursadaService = cursadaService;
    }

    public async Task<List<MateriaDisponibleDto>> ObtenerMateriasDisponiblesAsync(string usuarioId, int anio)
    {
        var inscriptas = await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.AnioLectivo == anio)
            .Select(c => c.MateriaId)
            .ToListAsync();

        return await _contexto.Materias
            .AsNoTracking()
            .Where(m => !inscriptas.Contains(m.Id))
            .OrderBy(m => m.Anio)
            .ThenBy(m => m.Nombre)
            .Select(m => new MateriaDisponibleDto { Id = m.Id, Nombre = m.Nombre, Anio = m.Anio })
            .ToListAsync();
    }

    public async Task<ResultadoOperacionDto> InscribirseAsync(string usuarioId, int materiaId, int anio)
    {
        var materia = await _contexto.Materias
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == materiaId);

        if (materia == null)
        {
            return new ResultadoOperacionDto(false, "La materia no existe.");
        }

        if (await _contexto.Cursadas.AnyAsync(c =>
                c.UsuarioId == usuarioId && c.MateriaId == materiaId && c.AnioLectivo == anio))
        {
            return new ResultadoOperacionDto(false, "Ya te encuentras inscripto/a en esta materia para el año actual.");
        }

        _contexto.Cursadas.Add(new Cursada
        {
            UsuarioId = usuarioId,
            MateriaId = materiaId,
            AnioLectivo = anio,
            Estado = EstadoCursada.Cursando
        });

        try
        {
            await _contexto.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return new ResultadoOperacionDto(false, "Ya te encuentras inscripto/a en esta materia para el año actual.");
        }

        return new ResultadoOperacionDto(true, $"Inscripción confirmada en {materia.Nombre}.");
    }

    public async Task<(List<CursadaHistoriaDto> Cursadas, List<int> Anios)> ObtenerHistoriaAsync(
        string usuarioId,
        int? anio,
        EstadoCursada? estado)
    {
        var consulta = _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId);

        if (anio.HasValue)
        {
            consulta = consulta.Where(c => c.AnioLectivo == anio.Value);
        }

        if (estado.HasValue)
        {
            consulta = consulta.Where(c => c.Estado == estado.Value);
        }

        var cursadas = await consulta
            .OrderByDescending(c => c.AnioLectivo)
            .ThenBy(c => c.Materia.Nombre)
            .Select(c => new CursadaHistoriaDto
            {
                MateriaNombre = c.Materia.Nombre,
                AnioLectivo = c.AnioLectivo,
                Nota = c.Nota,
                Estado = c.Estado
            })
            .ToListAsync();

        var anios = await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .Select(c => c.AnioLectivo)
            .Distinct()
            .OrderByDescending(a => a)
            .ToListAsync();

        return (cursadas, anios);
    }

    public async Task<(List<CursadaHistoriaDto> Cursadas, string Titular)> ObtenerHistoriaParaImprimirAsync(string usuarioId)
    {
        var datos = await _contexto.Users
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new { u.Apellido, u.Nombre, u.Dni })
            .FirstOrDefaultAsync();

        var cursadas = await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.AnioLectivo)
            .ThenBy(c => c.Materia.Nombre)
            .Select(c => new CursadaHistoriaDto
            {
                MateriaNombre = c.Materia.Nombre,
                AnioLectivo = c.AnioLectivo,
                Nota = c.Nota,
                Estado = c.Estado
            })
            .ToListAsync();

        var titular = datos != null ? $"{datos.Apellido}, {datos.Nombre} - DNI {datos.Dni}" : string.Empty;
        return (cursadas, titular);
    }

    public async Task<CertificadoInfoDto> ObtenerInfoCertificadosAsync(string usuarioId)
    {
        var anioActual = DateTime.Today.Year;

        var esRegular = await _contexto.Cursadas.AnyAsync(c =>
            c.UsuarioId == usuarioId &&
            c.AnioLectivo == anioActual &&
            (c.Estado == EstadoCursada.Cursando || c.Estado == EstadoCursada.Regular));

        var materiasAprobadas = await _contexto.Cursadas.CountAsync(c =>
            c.UsuarioId == usuarioId && c.Estado == EstadoCursada.Aprobada);

        return new CertificadoInfoDto { EsRegular = esRegular, AnioActual = anioActual, MateriasAprobadas = materiasAprobadas };
    }

    public async Task<bool> EsAlumnoRegularAsync(string usuarioId, int anio)
    {
        return await _contexto.Cursadas.AnyAsync(c =>
            c.UsuarioId == usuarioId &&
            c.AnioLectivo == anio &&
            (c.Estado == EstadoCursada.Cursando || c.Estado == EstadoCursada.Regular));
    }

    public async Task<List<MateriaAprobadaDto>> ObtenerMateriasAprobadasAsync(string usuarioId)
    {
        return await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.UsuarioId == usuarioId && c.Estado == EstadoCursada.Aprobada)
            .OrderBy(c => c.AnioLectivo)
            .ThenBy(c => c.Materia.Nombre)
            .Select(c => new MateriaAprobadaDto
            {
                Nombre = c.Materia.Nombre,
                Nota = c.Nota ?? 0,
                Anio = c.AnioLectivo
            })
            .ToListAsync();
    }

    public async Task<List<MateriaDto>> ObtenerMisMateriasAsync(string usuarioId)
    {
        return await _contexto.Materias
            .AsNoTracking()
            .Where(m => m.DocenteId == usuarioId)
            .OrderBy(m => m.Anio)
            .ThenBy(m => m.Nombre)
            .Select(m => new MateriaDto { Id = m.Id, Nombre = m.Nombre, Anio = m.Anio })
            .ToListAsync();
    }

    public async Task<MateriaDto?> ObtenerMateriaAutorizadaAsync(int materiaId, string usuarioId)
    {
        return await _contexto.Materias
            .AsNoTracking()
            .Where(m => m.Id == materiaId && m.DocenteId == usuarioId)
            .Select(m => new MateriaDto { Id = m.Id, Nombre = m.Nombre, Anio = m.Anio, DocenteId = m.DocenteId })
            .FirstOrDefaultAsync();
    }

    public async Task<List<InscriptoDto>> ObtenerInscriptosAsync(int materiaId)
    {
        return await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.MateriaId == materiaId)
            .OrderBy(c => c.Usuario.Apellido)
            .ThenBy(c => c.Usuario.Nombre)
            .Select(c => new InscriptoDto
            {
                Id = c.Id,
                AlumnoNombre = $"{c.Usuario.Apellido}, {c.Usuario.Nombre}",
                AnioLectivo = c.AnioLectivo,
                Nota = c.Nota,
                Estado = c.Estado
            })
            .ToListAsync();
    }

    public async Task<(bool Existe, bool Autorizado, NotasDto? Dto)> ObtenerFormularioNotasAsync(
        int cursadaId,
        string usuarioId)
    {
        var fila = await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.Id == cursadaId)
            .Select(c => new
            {
                c.Id,
                c.Materia.DocenteId,
                c.MateriaId,
                MateriaNombre = c.Materia.Nombre,
                Apellido = c.Usuario.Apellido,
                Nombre = c.Usuario.Nombre,
                c.Nota,
                c.Estado
            })
            .FirstOrDefaultAsync();

        if (fila == null)
        {
            return (false, false, null);
        }

        var autorizado = fila.DocenteId == usuarioId;
        var dto = new NotasDto
        {
            Id = fila.Id,
            MateriaId = fila.MateriaId,
            AlumnoNombre = $"{fila.Apellido}, {fila.Nombre}",
            MateriaNombre = fila.MateriaNombre,
            Nota = fila.Nota != null ? fila.Nota.Value.ToString("0.#", CultureInfo.InvariantCulture) : null,
            Estado = fila.Estado
        };

        return (true, autorizado, dto);
    }

    public async Task<ResultadoOperacionDto> GuardarNotasAsync(
        int id,
        string? nota,
        EstadoCursada estado,
        string usuarioId)
    {
        return await _cursadaService.GuardarNotasAsync(id, nota, estado, usuarioId, false);
    }

    public async Task<List<MateriaDto>> ObtenerMateriasParaSeleccionAsync(string usuarioId)
    {
        return await _contexto.Materias
            .AsNoTracking()
            .Where(m => m.DocenteId == usuarioId)
            .OrderBy(m => m.Nombre)
            .Select(m => new MateriaDto { Id = m.Id, Nombre = m.Nombre })
            .ToListAsync();
    }

    public async Task<(MateriaDto? Materia, List<InscriptoNotasDto> Inscriptos)> ObtenerListadoInscriptosAsync(
        int materiaId,
        string usuarioId)
    {
        var materia = await ObtenerMateriaAutorizadaAsync(materiaId, usuarioId);
        if (materia == null)
        {
            return (null, new List<InscriptoNotasDto>());
        }

        var inscriptos = await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.MateriaId == materiaId)
            .OrderBy(c => c.Usuario.Apellido)
            .ThenBy(c => c.Usuario.Nombre)
            .Select(c => new InscriptoNotasDto
            {
                ApellidoNombre = c.Usuario.Apellido + ", " + c.Usuario.Nombre,
                Nota = c.Nota,
                Estado = c.Estado.ToString()
            })
            .ToListAsync();

        return (materia, inscriptos);
    }

    public async Task<PerfilUsuarioDto?> ObtenerPerfilAsync(string usuarioId)
    {
        return await _contexto.Users
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new PerfilUsuarioDto
            {
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email ?? string.Empty,
                Dni = u.Dni,
                Legajo = u.Legajo,
                Rol = u.Rol,
                RutaFoto = u.RutaFoto
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacionDto> ActualizarAvatarAsync(string usuarioId, IFormFile? foto)
    {
        var usuario = await _userManager.FindByIdAsync(usuarioId);
        if (usuario == null)
        {
            return new ResultadoOperacionDto(false, "El usuario no existe.");
        }

        var resultado = await _archivoService.GuardarAvatarAsync(foto);
        if (!resultado.Ok)
        {
            return resultado;
        }

        if (!string.IsNullOrEmpty(usuario.RutaFoto) &&
            !string.Equals(usuario.RutaFoto, resultado.RutaRelativa, StringComparison.OrdinalIgnoreCase))
        {
            _archivoService.EliminarAvatar(usuario.RutaFoto);
        }

        usuario.RutaFoto = resultado.RutaRelativa;
        var actualizado = await _userManager.UpdateAsync(usuario);
        if (!actualizado.Succeeded)
        {
            _archivoService.EliminarAvatar(resultado.RutaRelativa);
            return new ResultadoOperacionDto(false, "No se pudo actualizar la foto de perfil.");
        }

        var exito = new ResultadoOperacionDto(true, "Foto de perfil actualizada.");
        exito.RutaRelativa = resultado.RutaRelativa;
        return exito;
    }
}
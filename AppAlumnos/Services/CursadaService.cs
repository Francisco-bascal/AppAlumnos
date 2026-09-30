using AppAlumnos.Data;
using AppAlumnos.DTOs;
using AppAlumnos.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AppAlumnos.Services;

public class CursadaService
{
    private readonly AppAlumnosContext _contexto;

    public CursadaService(AppAlumnosContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<List<CursadaDto>> ObtenerTodasAsync()
    {
        return await _contexto.Cursadas
            .AsNoTracking()
            .OrderByDescending(c => c.AnioLectivo)
            .ThenBy(c => c.Materia.Nombre)
            .Select(c => new CursadaDto
            {
                Id = c.Id,
                UsuarioId = c.UsuarioId,
                AlumnoNombre = $"{c.Usuario.Apellido}, {c.Usuario.Nombre}",
                MateriaId = c.MateriaId,
                MateriaNombre = c.Materia.Nombre,
                AnioLectivo = c.AnioLectivo,
                Nota = c.Nota,
                Estado = c.Estado
            })
            .ToListAsync();
    }

    public async Task<GuardarCursadaDto?> ObtenerFormularioEdicionAsync(int id)
    {
        if (id == 0)
        {
            return new GuardarCursadaDto { AnioLectivo = DateTime.Today.Year };
        }

        return await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new GuardarCursadaDto
            {
                Id = c.Id,
                UsuarioId = c.UsuarioId,
                MateriaId = c.MateriaId,
                AnioLectivo = c.AnioLectivo
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacionDto> GuardarAsync(GuardarCursadaDto dto)
    {
        if (await _contexto.Cursadas.AnyAsync(c =>
                c.UsuarioId == dto.UsuarioId &&
                c.MateriaId == dto.MateriaId &&
                c.AnioLectivo == dto.AnioLectivo &&
                c.Id != dto.Id))
        {
            return new ResultadoOperacionDto(false, "El alumno ya se encuentra inscripto en esa materia para el año lectivo seleccionado.");
        }

        if (dto.Id == 0)
        {
            _contexto.Cursadas.Add(new Cursada
            {
                UsuarioId = dto.UsuarioId,
                MateriaId = dto.MateriaId,
                AnioLectivo = dto.AnioLectivo,
                Estado = EstadoCursada.Cursando
            });
        }
        else
        {
            var cursadaExistente = await _contexto.Cursadas.FindAsync(dto.Id);
            if (cursadaExistente == null)
            {
                return new ResultadoOperacionDto(false, "La inscripción no existe.");
            }

            cursadaExistente.UsuarioId = dto.UsuarioId;
            cursadaExistente.MateriaId = dto.MateriaId;
            cursadaExistente.AnioLectivo = dto.AnioLectivo;
        }

        try
        {
            await _contexto.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return new ResultadoOperacionDto(false, "El alumno ya se encuentra inscripto en esa materia para el año lectivo seleccionado.");
        }

        return new ResultadoOperacionDto(true, "Inscripción guardada correctamente.");
    }

    public async Task<ResultadoOperacionDto> EliminarAsync(int id)
    {
        var cursada = await _contexto.Cursadas.FindAsync(id);
        if (cursada == null)
        {
            return new ResultadoOperacionDto(false, "La inscripción no existe.");
        }

        _contexto.Cursadas.Remove(cursada);
        await _contexto.SaveChangesAsync();
        return new ResultadoOperacionDto(true, "Inscripción eliminada correctamente.");
    }

    public async Task<NotasDto?> ObtenerFormularioNotasAsync(int id)
    {
        return await _contexto.Cursadas
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new NotasDto
            {
                Id = c.Id,
                MateriaId = c.MateriaId,
                AlumnoNombre = $"{c.Usuario.Apellido}, {c.Usuario.Nombre}",
                MateriaNombre = c.Materia.Nombre,
                Nota = c.Nota != null ? c.Nota.Value.ToString("0.#", CultureInfo.InvariantCulture) : null,
                Estado = c.Estado
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacionDto> GuardarNotasAsync(
        int id,
        string? nota,
        EstadoCursada estado,
        string? usuarioOperanteId,
        bool esAdministrador)
    {
        var cursada = await _contexto.Cursadas
            .Include(c => c.Materia)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cursada == null)
        {
            return new ResultadoOperacionDto(false, "La cursada no existe.");
        }

        if (!esAdministrador && (usuarioOperanteId == null || cursada.Materia.DocenteId != usuarioOperanteId))
        {
            return new ResultadoOperacionDto(false, "No tenés permisos para cargar notas en esta materia.");
        }

        if (!string.IsNullOrWhiteSpace(nota))
        {
            var parseado = decimal.TryParse(nota, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorNota)
                || decimal.TryParse(nota, NumberStyles.Number, CultureInfo.CurrentCulture, out valorNota);

            if (!parseado || valorNota < 1 || valorNota > 10)
            {
                return new ResultadoOperacionDto(false, "Ingrese una nota válida entre 1 y 10.");
            }
            cursada.Nota = valorNota;
        }
        else
        {
            cursada.Nota = null;
        }

        cursada.Estado = estado;
        await _contexto.SaveChangesAsync();

        return new ResultadoOperacionDto(true, "Notas guardadas correctamente.");
    }
}
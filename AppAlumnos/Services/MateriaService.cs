using AppAlumnos.Data;
using AppAlumnos.DTOs;
using AppAlumnos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppAlumnos.Services;

public class MateriaService
{
    private readonly AppAlumnosContext _contexto;
    private readonly UserManager<Usuario> _userManager;

    public MateriaService(AppAlumnosContext contexto, UserManager<Usuario> userManager)
    {
        _contexto = contexto;
        _userManager = userManager;
    }

    public async Task<List<MateriaDto>> ObtenerTodasAsync()
    {
        return await _contexto.Materias
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MateriaDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Anio = m.Anio,
                DocenteId = m.DocenteId,
                DocenteNombre = m.Docente != null ? $"{m.Docente.Apellido}, {m.Docente.Nombre}" : null
            })
            .ToListAsync();
    }

    public async Task<MateriaDto?> ObtenerPorIdAsync(int id)
    {
        return await _contexto.Materias
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new MateriaDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Anio = m.Anio,
                DocenteId = m.DocenteId,
                DocenteNombre = m.Docente != null ? $"{m.Docente.Apellido}, {m.Docente.Nombre}" : null
            })
            .FirstOrDefaultAsync();
    }

    public async Task<GuardarMateriaDto?> ObtenerFormularioEdicionAsync(int id)
    {
        return await _contexto.Materias
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(m => new GuardarMateriaDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Anio = m.Anio,
                DocenteId = m.DocenteId
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacionDto> GuardarAsync(GuardarMateriaDto dto)
    {
        if (dto.Id == 0)
        {
            _contexto.Materias.Add(new Materia
            {
                Nombre = dto.Nombre,
                Anio = dto.Anio,
                DocenteId = string.IsNullOrWhiteSpace(dto.DocenteId) ? null : dto.DocenteId
            });
        }
        else
        {
            var materia = await _contexto.Materias.FindAsync(dto.Id);
            if (materia == null)
            {
                return new ResultadoOperacionDto(false, "La materia no existe.");
            }

            materia.Nombre = dto.Nombre;
            materia.Anio = dto.Anio;
            materia.DocenteId = string.IsNullOrWhiteSpace(dto.DocenteId) ? null : dto.DocenteId;
        }

        await _contexto.SaveChangesAsync();
        return new ResultadoOperacionDto(true, "Materia guardada correctamente.");
    }

    public async Task<ResultadoOperacionDto> EliminarAsync(int id)
    {
        var materia = await _contexto.Materias.FindAsync(id);
        if (materia == null)
        {
            return new ResultadoOperacionDto(false, "La materia no existe.");
        }

        if (await _contexto.Cursadas.AnyAsync(c => c.MateriaId == id))
        {
            return new ResultadoOperacionDto(false, "No se puede eliminar la materia porque tiene inscripciones asociadas.");
        }

        _contexto.Materias.Remove(materia);
        await _contexto.SaveChangesAsync();
        return new ResultadoOperacionDto(true, "Materia eliminada correctamente.");
    }

    public async Task<List<UsuarioOpcionDto>> ObtenerDocentesAsync()
    {
        var docentes = await _userManager.GetUsersInRoleAsync("Docente");
        return docentes
            .OrderBy(d => d.Apellido)
            .ThenBy(d => d.Nombre)
            .Select(d => new UsuarioOpcionDto { Id = d.Id, NombreCompleto = $"{d.Apellido}, {d.Nombre}" })
            .ToList();
    }

    public async Task<List<MateriaDto>> ObtenerMateriasParaSelectAsync()
    {
        return await _contexto.Materias
            .AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MateriaDto { Id = m.Id, Nombre = m.Nombre })
            .ToListAsync();
    }
}
using AppAlumnos.Data;
using AppAlumnos.DTOs;
using AppAlumnos.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AppAlumnos.Services;

public class UsuarioService
{
    private readonly AppAlumnosContext _contexto;
    private readonly UserManager<Usuario> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsuarioService(
        AppAlumnosContext contexto,
        UserManager<Usuario> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _contexto = contexto;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<List<UsuarioDto>> ObtenerTodosAsync()
    {
        return await _contexto.Users
            .AsNoTracking()
            .OrderBy(u => u.Apellido)
            .ThenBy(u => u.Nombre)
            .Select(u => new UsuarioDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email ?? string.Empty,
                Dni = u.Dni,
                Legajo = u.Legajo,
                Rol = u.Rol,
                Estado = u.Estado,
                FechaAlta = u.FechaAlta,
                RutaFoto = u.RutaFoto
            })
            .ToListAsync();
    }

    public async Task<List<string>> ObtenerRolesAsync()
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => r.Name!)
            .ToListAsync();
    }

    public async Task<List<UsuarioOpcionDto>> ObtenerAlumnosAsync()
    {
        var alumnos = await _userManager.GetUsersInRoleAsync("Alumno");
        return alumnos
            .OrderBy(a => a.Apellido)
            .ThenBy(a => a.Nombre)
            .Select(a => new UsuarioOpcionDto { Id = a.Id, NombreCompleto = $"{a.Apellido}, {a.Nombre}" })
            .ToList();
    }

    public async Task<EditarUsuarioDto?> ObtenerFormularioEdicionAsync(string id)
    {
        return await _contexto.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new EditarUsuarioDto
            {
                Id = u.Id,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email ?? string.Empty,
                Dni = u.Dni,
                Legajo = u.Legajo,
                Rol = u.Rol,
                Estado = u.Estado
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ResultadoOperacionDto> CrearUsuarioAsync(CrearUsuarioDto dto)
    {
        if (!EsRolValido(dto.Rol))
        {
            return new ResultadoOperacionDto(false, "El rol seleccionado no es válido.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password))
        {
            return new ResultadoOperacionDto(false, "La contraseña es obligatoria.");
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            return new ResultadoOperacionDto(false, "La contraseña y su confirmación no coinciden.");
        }

        var email = dto.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            return new ResultadoOperacionDto(false, "Ya existe un usuario con ese email.");
        }

        var usuario = new Usuario
        {
            UserName = email,
            Email = email,
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Dni = dto.Dni,
            Legajo = string.IsNullOrWhiteSpace(dto.Legajo) ? "-" : dto.Legajo.Trim(),
            Rol = dto.Rol,
            Estado = dto.Estado,
            FechaAlta = DateTime.Now,
            EmailConfirmed = true
        };

        var resultado = await _userManager.CreateAsync(usuario, dto.Password);
        if (!resultado.Succeeded)
        {
            return new ResultadoOperacionDto(false, string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        await _userManager.AddToRoleAsync(usuario, dto.Rol);
        return new ResultadoOperacionDto(true, "Usuario creado correctamente.");
    }

    public async Task<ResultadoOperacionDto> EditarUsuarioAsync(EditarUsuarioDto dto)
    {
        if (!EsRolValido(dto.Rol))
        {
            return new ResultadoOperacionDto(false, "El rol seleccionado no es válido.");
        }

        var usuario = await _userManager.FindByIdAsync(dto.Id!);
        if (usuario == null)
        {
            return new ResultadoOperacionDto(false, "El usuario no existe.");
        }

        var email = dto.Email.Trim();
        if (!string.Equals(email, usuario.Email, StringComparison.OrdinalIgnoreCase)
            && await _userManager.FindByEmailAsync(email) != null)
        {
            return new ResultadoOperacionDto(false, "Ya existe un usuario con ese email.");
        }

        if (!string.Equals(dto.Rol, "Administrador", StringComparison.Ordinal) || !dto.Estado)
        {
            if (await EsUltimoAdministradorActivoAsync(usuario.Id))
            {
                return new ResultadoOperacionDto(false, "No se puede modificar al último administrador activo.");
            }
        }

        var rolesActuales = await _userManager.GetRolesAsync(usuario);
        if (!rolesActuales.Contains(dto.Rol))
        {
            await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
            await _userManager.AddToRoleAsync(usuario, dto.Rol);
        }

        usuario.Nombre = dto.Nombre.Trim();
        usuario.Apellido = dto.Apellido.Trim();
        usuario.Dni = dto.Dni;
        usuario.Legajo = string.IsNullOrWhiteSpace(dto.Legajo) ? "-" : dto.Legajo.Trim();
        usuario.Rol = dto.Rol;
        usuario.Estado = dto.Estado;

        if (!string.Equals(email, usuario.Email, StringComparison.OrdinalIgnoreCase))
        {
            await _userManager.SetUserNameAsync(usuario, email);
            await _userManager.SetEmailAsync(usuario, email);
        }

        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            return new ResultadoOperacionDto(false, string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        return new ResultadoOperacionDto(true, "Usuario actualizado correctamente.");
    }

    public async Task<ResultadoOperacionDto> CambiarEstadoAsync(string id, bool estado)
    {
        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario == null)
        {
            return new ResultadoOperacionDto(false, "El usuario no existe.");
        }

        if (!estado && await EsUltimoAdministradorActivoAsync(usuario.Id))
        {
            return new ResultadoOperacionDto(false, "No se puede desactivar al último administrador activo.");
        }

        if (usuario.Estado == estado)
        {
            return new ResultadoOperacionDto(true, estado ? "El usuario ya se encuentra activado." : "El usuario ya se encuentra desactivado.");
        }

        usuario.Estado = estado;
        var resultado = await _userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
        {
            return new ResultadoOperacionDto(false, string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        return new ResultadoOperacionDto(true, estado ? "Usuario activado correctamente." : "Usuario desactivado correctamente.");
    }

    public async Task<ResultadoOperacionDto> EliminarUsuarioAsync(string id, string? usuarioOperanteId)
    {
        if (string.Equals(id, usuarioOperanteId, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoOperacionDto(false, "No puedes eliminarte a ti mismo.");
        }

        var usuario = await _userManager.FindByIdAsync(id);
        if (usuario == null)
        {
            return new ResultadoOperacionDto(false, "El usuario no existe.");
        }

        if (usuario.Rol == "Administrador" && await EsUltimoAdministradorActivoAsync(usuario.Id))
        {
            return new ResultadoOperacionDto(false, "No se puede eliminar al último administrador activo.");
        }

        var tieneCursadas = await _contexto.Cursadas.AnyAsync(c => c.UsuarioId == id);
        var esDocenteDeMateria = await _contexto.Materias.AnyAsync(m => m.DocenteId == id);

        if (!tieneCursadas && !esDocenteDeMateria)
        {
            foreach (var rol in await _userManager.GetRolesAsync(usuario))
            {
                await _userManager.RemoveFromRoleAsync(usuario, rol);
            }

            var borrado = await _userManager.DeleteAsync(usuario);
            if (!borrado.Succeeded)
            {
                return new ResultadoOperacionDto(false, string.Join(" ", borrado.Errors.Select(e => e.Description)));
            }

            return new ResultadoOperacionDto(true, "Usuario eliminado correctamente.");
        }

        usuario.Estado = false;
        await _userManager.UpdateAsync(usuario);
        return new ResultadoOperacionDto(true, "El usuario tenía registros asociados: se aplicó la baja lógica (Estado = inactivo).");
    }

    private async Task<bool> EsUltimoAdministradorActivoAsync(string idExcluido)
    {
        var cantidad = await _contexto.Users.CountAsync(u =>
            u.Rol == "Administrador" && u.Estado && u.Id != idExcluido);
        return cantidad == 0;
    }

    private static bool EsRolValido(string rol)
    {
        return rol is "Administrador" or "Docente" or "Alumno";
    }
}
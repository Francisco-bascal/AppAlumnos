using AppAlumnos.Data;
using AppAlumnos.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AppAlumnos.Controllers
{
    [Authorize]
    public class CursadasController : Controller
    {
        private readonly AppAlumnosContext _contexto;
        private readonly UserManager<Usuario> _userManager;

        public CursadasController(AppAlumnosContext contexto, UserManager<Usuario> userManager)
        {
            _contexto = contexto;
            _userManager = userManager;
        }

        // GET: Cursadas
        public async Task<IActionResult> Index()
        {
            var cursadas = await _contexto.Cursadas
                .Include(c => c.Usuario)
                .Include(c => c.Materia)
                .OrderByDescending(c => c.AnioLectivo)
                .ThenBy(c => c.Materia.Nombre)
                .ToListAsync();

            return View(cursadas);
        }

        // GET: Cursadas/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            Cursada cursada;
            if (id == 0)
            {
                cursada = new Cursada { AnioLectivo = DateTime.Today.Year };
            }
            else
            {
                var existente = await _contexto.Cursadas.FindAsync(id);
                if (existente == null) return NotFound();
                cursada = existente;
            }

            await CargarListasAsync(cursada.UsuarioId, cursada.MateriaId);
            return PartialView("_FormularioCursadaPartial", cursada);
        }

        // GET: Cursadas/ObtenerFormularioNotas/5 (Carga de notas por Docente/Admin)
        [HttpGet]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> ObtenerFormularioNotas(int id)
        {
            var cursada = await _contexto.Cursadas
                .Include(c => c.Usuario)
                .Include(c => c.Materia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cursada == null) return NotFound();

            ViewBag.AlumnoNombre = $"{cursada.Usuario.Apellido}, {cursada.Usuario.Nombre}";
            ViewBag.MateriaNombre = cursada.Materia.Nombre;
            return PartialView("_FormularioNotasPartial", cursada);
        }

        // POST: Cursadas/GuardarNotas/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> GuardarNotas(int id, decimal? nota, EstadoCursada estado)
        {
            var cursada = await _contexto.Cursadas.FindAsync(id);
            if (cursada == null)
            {
                return Json(new { success = false, mensaje = "La cursada no existe." });
            }

            cursada.Nota = nota;
            cursada.Estado = estado;
            await _contexto.SaveChangesAsync();

            return Json(new { success = true, mensaje = "Notas guardadas correctamente." });
        }

        // POST: Cursadas/Guardar (Procesa Crear y Editar vía AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(Cursada cursada)
        {
            if (ModelState.IsValid)
            {
                var duplicada = await _contexto.Cursadas.AnyAsync(c =>
                    c.UsuarioId == cursada.UsuarioId &&
                    c.MateriaId == cursada.MateriaId &&
                    c.AnioLectivo == cursada.AnioLectivo &&
                    c.Id != cursada.Id);

                if (duplicada)
                {
                    return Json(new { success = false, mensaje = "El alumno ya se encuentra inscripto en esa materia para el año lectivo seleccionado." });
                }

                if (cursada.Id == 0)
                {
                    cursada.Estado = EstadoCursada.Cursando;
                    _contexto.Add(cursada);
                }
                else
                {
                    var cursadaExistente = await _contexto.Cursadas.FindAsync(cursada.Id);
                    if (cursadaExistente == null)
                    {
                        return Json(new { success = false, mensaje = "La inscripción no existe." });
                    }

                    cursadaExistente.UsuarioId = cursada.UsuarioId;
                    cursadaExistente.MateriaId = cursada.MateriaId;
                    cursadaExistente.AnioLectivo = cursada.AnioLectivo;
                }

                try
                {
                    await _contexto.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    return Json(new { success = false, mensaje = "El alumno ya se encuentra inscripto en esa materia para el año lectivo seleccionado." });
                }

                return Json(new { success = true, mensaje = "Inscripción guardada correctamente." });
            }

            await CargarListasAsync(cursada.UsuarioId, cursada.MateriaId);
            return PartialView("_FormularioCursadaPartial", cursada);
        }

        // POST: Cursadas/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cursada = await _contexto.Cursadas.FindAsync(id);
            if (cursada == null)
            {
                return Json(new { success = false, mensaje = "La inscripción no existe." });
            }

            _contexto.Cursadas.Remove(cursada);
            await _contexto.SaveChangesAsync();

            return Json(new { success = true, mensaje = "Inscripción eliminada correctamente." });
        }

        private async Task CargarListasAsync(string? usuarioIdSeleccionado = null, int? materiaIdSeleccionada = null)
        {
            var alumnos = await _userManager.GetUsersInRoleAsync("Alumno");
            ViewBag.Usuarios = new SelectList(
                alumnos.OrderBy(a => a.Apellido).ThenBy(a => a.Nombre)
                    .Select(a => new { a.Id, NombreCompleto = $"{a.Apellido}, {a.Nombre}" }),
                "Id", "NombreCompleto", usuarioIdSeleccionado);

            ViewBag.Materias = new SelectList(
                await _contexto.Materias.OrderBy(m => m.Nombre).ToListAsync(),
                "Id", "Nombre", materiaIdSeleccionada);
        }
    }
}
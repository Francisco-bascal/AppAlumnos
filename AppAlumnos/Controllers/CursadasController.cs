using AppAlumnos.DTOs;
using AppAlumnos.Models;
using AppAlumnos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Administrador,Docente")]
    public class CursadasController : Controller
    {
        private readonly CursadaService _cursadaService;
        private readonly MateriaService _materiaService;
        private readonly UsuarioService _usuarioService;
        private readonly UserManager<Usuario> _userManager;

        public CursadasController(
            CursadaService cursadaService,
            MateriaService materiaService,
            UsuarioService usuarioService,
            UserManager<Usuario> userManager)
        {
            _cursadaService = cursadaService;
            _materiaService = materiaService;
            _usuarioService = usuarioService;
            _userManager = userManager;
        }

        // GET: Cursadas
        public async Task<IActionResult> Index()
        {
            return View(await _cursadaService.ObtenerTodasAsync());
        }

        // GET: Cursadas/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            var dto = await _cursadaService.ObtenerFormularioEdicionAsync(id);
            if (dto == null)
            {
                return NotFound();
            }

            await CargarListasAsync(dto.UsuarioId, dto.MateriaId);
            return PartialView("_FormularioCursadaPartial", dto);
        }

        // GET: Cursadas/ObtenerFormularioNotas/5 (Carga de notas por Docente/Admin)
        [HttpGet]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> ObtenerFormularioNotas(int id)
        {
            var dto = await _cursadaService.ObtenerFormularioNotasAsync(id);
            if (dto == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Administrador"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null || !await _cursadaService.EsDocenteDeCursadaAsync(id, usuario.Id))
                {
                    return Forbid();
                }
            }

            return PartialView("_FormularioNotasPartial", dto);
        }

        // POST: Cursadas/GuardarNotas/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> GuardarNotas(int id, string nota, EstadoCursada estado)
        {
            var usuario = await _userManager.GetUserAsync(User);
            var resultado = await _cursadaService.GuardarNotasAsync(id, nota, estado, usuario?.Id, User.IsInRole("Administrador"));
            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
        }

        // POST: Cursadas/Guardar (Procesa Crear y Editar vía AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(GuardarCursadaDto dto)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _cursadaService.GuardarAsync(dto);
                return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
            }

            await CargarListasAsync(dto.UsuarioId, dto.MateriaId);
            return PartialView("_FormularioCursadaPartial", dto);
        }

        // POST: Cursadas/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _cursadaService.EliminarAsync(id);
            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
        }

        private async Task CargarListasAsync(string? usuarioIdSeleccionado = null, int? materiaIdSeleccionada = null)
        {
            var alumnos = await _usuarioService.ObtenerAlumnosAsync();
            ViewBag.Usuarios = new SelectList(alumnos, "Id", "NombreCompleto", usuarioIdSeleccionado);

            var materias = await _materiaService.ObtenerMateriasParaSelectAsync();
            ViewBag.Materias = new SelectList(materias, "Id", "Nombre", materiaIdSeleccionada);
        }
    }
}
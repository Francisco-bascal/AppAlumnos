using AppAlumnos.DTOs;
using AppAlumnos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class MateriasController : Controller
    {
        private readonly MateriaService _materiaService;

        public MateriasController(MateriaService materiaService)
        {
            _materiaService = materiaService;
        }

        // GET: Materias
        public async Task<IActionResult> Index()
        {
            return View(await _materiaService.ObtenerTodasAsync());
        }

        // GET: Materias/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {
            var docentes = await _materiaService.ObtenerDocentesAsync();

            if (id == 0)
            {
                ViewBag.Docentes = new SelectList(docentes, "Id", "NombreCompleto");
                return PartialView("_FormularioMateriaPartial", new GuardarMateriaDto { Anio = 1 });
            }

            var materia = await _materiaService.ObtenerFormularioEdicionAsync(id);
            if (materia == null)
            {
                return NotFound();
            }

            ViewBag.Docentes = new SelectList(docentes, "Id", "NombreCompleto", materia.DocenteId);
            return PartialView("_FormularioMateriaPartial", materia);
        }

        // GET: Materias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materia = await _materiaService.ObtenerPorIdAsync(id.Value);
            if (materia == null)
            {
                return NotFound();
            }

            return View(materia);
        }

        // POST: Materias/Guardar (Procesa Crear y Editar vía AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(GuardarMateriaDto dto)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _materiaService.GuardarAsync(dto);
                return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
            }

            ViewBag.Docentes = new SelectList(await _materiaService.ObtenerDocentesAsync(), "Id", "NombreCompleto", dto.DocenteId);
            return PartialView("_FormularioMateriaPartial", dto);
        }

        // POST: Materias/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var resultado = await _materiaService.EliminarAsync(id);
            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
        }
    }
}
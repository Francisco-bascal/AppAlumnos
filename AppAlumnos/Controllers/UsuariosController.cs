using AppAlumnos.DTOs;
using AppAlumnos.Models;
using AppAlumnos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppAlumnos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UsuarioService _usuarioService;
        private readonly UserManager<Usuario> _userManager;

        public UsuariosController(UsuarioService usuarioService, UserManager<Usuario> userManager)
        {
            _usuarioService = usuarioService;
            _userManager = userManager;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            return View(await _usuarioService.ObtenerTodosAsync());
        }

        // GET: Usuarios/ObtenerFormulario (Crear) o /ObtenerFormulario/{id} (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(string id = "")
        {
            var esNuevo = string.IsNullOrEmpty(id);

            if (esNuevo)
            {
                ViewBag.EsNuevo = true;
                ViewBag.Roles = new SelectList(await _usuarioService.ObtenerRolesAsync(), "Alumno");
                return PartialView("_FormularioUsuarioPartial", new CrearUsuarioDto());
            }

            var dto = await _usuarioService.ObtenerFormularioEdicionAsync(id);
            if (dto == null)
            {
                return NotFound();
            }

            ViewBag.EsNuevo = false;
            ViewBag.Roles = new SelectList(await _usuarioService.ObtenerRolesAsync(), dto.Rol);
            return PartialView("_FormularioUsuarioPartial", dto);
        }

        // POST: Usuarios/GuardarNuevo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarNuevo(CrearUsuarioDto dto)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _usuarioService.CrearUsuarioAsync(dto);
                if (resultado.Ok)
                {
                    return Json(new { success = true, mensaje = resultado.Mensaje });
                }
                return Json(new { success = false, mensaje = resultado.Mensaje });
            }

            ViewBag.EsNuevo = true;
            ViewBag.Roles = new SelectList(await _usuarioService.ObtenerRolesAsync(), dto.Rol);
            return PartialView("_FormularioUsuarioPartial", dto);
        }

        // POST: Usuarios/GuardarEditado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEditado(EditarUsuarioDto dto)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _usuarioService.EditarUsuarioAsync(dto);
                if (resultado.Ok)
                {
                    return Json(new { success = true, mensaje = resultado.Mensaje });
                }
                return Json(new { success = false, mensaje = resultado.Mensaje });
            }

            ViewBag.EsNuevo = false;
            ViewBag.Roles = new SelectList(await _usuarioService.ObtenerRolesAsync(), dto.Rol);
            return PartialView("_FormularioUsuarioPartial", dto);
        }

        // POST: Usuarios/CambiarEstado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(string id, bool estado)
        {
            var resultado = await _usuarioService.CambiarEstadoAsync(id, estado);
            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
        }

        // POST: Usuarios/Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(string id)
        {
            var usuarioActual = await _userManager.GetUserAsync(User);
            var resultado = await _usuarioService.EliminarUsuarioAsync(id, usuarioActual?.Id);
            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
        }
    }
}
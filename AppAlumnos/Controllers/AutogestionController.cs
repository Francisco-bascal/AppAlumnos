using AppAlumnos.DTOs;
using AppAlumnos.Exceptions;
using AppAlumnos.Models;
using AppAlumnos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AppAlumnos.Controllers
{
    [Authorize]
    public class AutogestionController : Controller
    {
        private readonly AutogestionService _autogestionService;
        private readonly CertificadoService _certificadoService;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogger<AutogestionController> _logger;

        public AutogestionController(
            AutogestionService autogestionService,
            CertificadoService certificadoService,
            UserManager<Usuario> userManager,
            ILogger<AutogestionController> logger)
        {
            _autogestionService = autogestionService;
            _certificadoService = certificadoService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Autogestion/Index
        public IActionResult Index()
        {
            return View();
        }

        #region Alumno

        // GET: Autogestion/InscripcionMaterias
        [Authorize(Roles = "Alumno")]
        public async Task<IActionResult> InscripcionMaterias()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var anioActual = DateTime.Today.Year;
            ViewBag.AnioActual = anioActual;
            var materias = await _autogestionService.ObtenerMateriasDisponiblesAsync(usuario.Id, anioActual);
            return PartialView("_InscripcionMateriasPartial", materias);
        }

        // POST: Autogestion/Inscribirse
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Alumno")]
        public async Task<IActionResult> Inscribirse(int materiaId)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var resultado = await _autogestionService.InscribirseAsync(usuario.Id, materiaId, DateTime.Today.Year);
            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
        }

        // GET: Autogestion/HistoriaAcademica?anio=&estado=
        [Authorize(Roles = "Alumno")]
        public async Task<IActionResult> HistoriaAcademica(int? anio, EstadoCursada? estado)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var (cursadas, anios) = await _autogestionService.ObtenerHistoriaAsync(usuario.Id, anio, estado);

            ViewBag.Anios = anios;
            ViewBag.AnioSeleccionado = anio;
            ViewBag.Estados = ObtenerEstados((int?)estado);
            return PartialView("_HistoriaAcademicaPartial", cursadas);
        }

        // GET: Autogestion/HistoriaImprimir
        [Authorize(Roles = "Alumno")]
        public async Task<IActionResult> HistoriaImprimir()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var (cursadas, titular) = await _autogestionService.ObtenerHistoriaParaImprimirAsync(usuario.Id);
            ViewBag.Titular = titular;
            return View(cursadas);
        }

        // GET: Autogestion/Certificados
        [Authorize(Roles = "Alumno")]
        public async Task<IActionResult> Certificados()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var info = await _autogestionService.ObtenerInfoCertificadosAsync(usuario.Id);

            ViewBag.EsRegular = info.EsRegular;
            ViewBag.AnioActual = info.AnioActual;
            ViewBag.MateriasAprobadas = info.MateriasAprobadas;
            return PartialView("_CertificadosPartial");
        }

        // GET: Autogestion/GenerarCertificado?tipo=regular|materias
        [Authorize(Roles = "Alumno")]
        public async Task<IActionResult> GenerarCertificado(string tipo)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var perfil = await _autogestionService.ObtenerPerfilAsync(usuario.Id);
            if (perfil == null) return Challenge();

            var apellidoNombre = $"{perfil.Apellido}, {perfil.Nombre}";
            var anioActual = DateTime.Today.Year;

            if (tipo == "regular")
            {
                if (!await _autogestionService.EsAlumnoRegularAsync(usuario.Id, anioActual))
                {
                    return Json(new { success = false, mensaje = "No consta tu condición de alumno regular en el año actual." });
                }

                try
                {
                    var pdf = _certificadoService.GenerarCertificadoAlumnoRegular(apellidoNombre, perfil.Dni.ToString(), anioActual);
                    return File(pdf, "application/pdf", $"certificado-alumno-regular-{anioActual}.pdf");
                }
                catch (FalloGeneracionDocumentoException)
                {
                    return Json(new { success = false, mensaje = "No se pudo generar el certificado. Intentá nuevamente." });
                }
            }

            if (tipo == "materias")
            {
                var materias = await _autogestionService.ObtenerMateriasAprobadasAsync(usuario.Id);
                if (materias.Count == 0)
                {
                    return Json(new { success = false, mensaje = "Todavía no tenés materias aprobadas para certificar." });
                }

                try
                {
                    var pdf = _certificadoService.GenerarCertificadoMateriasAprobadas(apellidoNombre, perfil.Dni.ToString(), materias);
                    return File(pdf, "application/pdf", "certificado-materias-aprobadas.pdf");
                }
                catch (FalloGeneracionDocumentoException)
                {
                    return Json(new { success = false, mensaje = "No se pudo generar el certificado. Intentá nuevamente." });
                }
            }

            return Json(new { success = false, mensaje = "Tipo de certificado no válido." });
        }

        #endregion

        #region Docente

        // GET: Autogestion/MisMaterias
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> MisMaterias()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var materias = await _autogestionService.ObtenerMisMateriasAsync(usuario.Id);
            return PartialView("_MisMateriasPartial", materias);
        }

        // GET: Autogestion/Inscriptos?materiaId=5
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> Inscriptos(int materiaId)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var materia = await _autogestionService.ObtenerMateriaAutorizadaAsync(materiaId, usuario.Id);
            if (materia == null) return NotFound();

            var inscriptos = await _autogestionService.ObtenerInscriptosAsync(materiaId);

            ViewBag.MateriaNombre = materia.Nombre;
            ViewBag.MateriaId = materiaId;
            return PartialView("_InscriptosPartial", inscriptos);
        }

        // GET: Autogestion/ObtenerFormularioNotas?cursadaId=7 (contenido del modal)
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> ObtenerFormularioNotas(int cursadaId)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var (existe, autorizado, dto) = await _autogestionService.ObtenerFormularioNotasAsync(cursadaId, usuario.Id);
            if (!existe) return NotFound();
            if (!autorizado) return Forbid();

            return PartialView("_FormularioNotasPortalPartial", dto);
        }

        // POST: Autogestion/GuardarNotas
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> GuardarNotas(int id, string nota, EstadoCursada estado)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var resultado = await _autogestionService.GuardarNotasAsync(id, nota, estado, usuario.Id);
            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje });
        }

        // GET: Autogestion/SeleccionarListado
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> SeleccionarListado()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var materias = await _autogestionService.ObtenerMateriasParaSeleccionAsync(usuario.Id);

            ViewBag.Materias = new SelectList(materias, "Id", "Nombre");
            return PartialView("_SeleccionarListadoPartial");
        }

        // GET: Autogestion/ListadoInscriptos?materiaId=5 (página imprimible)
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> ListadoInscriptos(int materiaId)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var (materia, inscriptos) = await _autogestionService.ObtenerListadoInscriptosAsync(materiaId, usuario.Id);
            if (materia == null) return NotFound();

            ViewBag.MateriaNombre = materia.Nombre;
            ViewBag.Cantidad = inscriptos.Count;
            var perfil = await _autogestionService.ObtenerPerfilAsync(usuario.Id);
            ViewBag.DocenteNombre = perfil != null ? $"{perfil.Apellido}, {perfil.Nombre}" : "";
            return View(inscriptos);
        }

        // GET: Autogestion/DescargarListadoPdf?materiaId=5
        [Authorize(Roles = "Docente")]
        public async Task<IActionResult> DescargarListadoPdf(int materiaId)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var (materia, inscriptos) = await _autogestionService.ObtenerListadoInscriptosAsync(materiaId, usuario.Id);
            if (materia == null) return NotFound();

            try
            {
                var pdf = _certificadoService.GenerarListadoInscriptos(materia.Nombre, DateTime.Today.Year, inscriptos);
                return File(pdf, "application/pdf", $"listado-inscriptos-{materia.Nombre}.pdf");
            }
            catch (FalloGeneracionDocumentoException)
            {
                return Json(new { success = false, mensaje = "No se pudo generar el listado. Intentá nuevamente." });
            }
        }

        #endregion

        #region Perfil

        // GET: Autogestion/MiPerfil
        public async Task<IActionResult> MiPerfil()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var perfil = await _autogestionService.ObtenerPerfilAsync(usuario.Id);
            if (perfil == null) return Challenge();

            ViewBag.RolNombre = perfil.Rol;
            return PartialView("_MiPerfilPartial", perfil);
        }

        // POST: Autogestion/SubirAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirAvatar(IFormFile? foto)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            // Sin estas trazas el rechazo solo era visible en la alerta del navegador,
            // lo que dejaba sin diagnóstico cualquier fallo de la subida.
            _logger.LogInformation(
                "Subida de avatar solicitada. Usuario: {UsuarioId}, Archivo: {NombreArchivo}, TamanoBytes: {TamanoBytes}",
                usuario.Id,
                foto?.FileName ?? "(sin archivo)",
                foto?.Length ?? 0);

            var resultado = await _autogestionService.ActualizarAvatarAsync(usuario.Id, foto);

            _logger.LogInformation(
                "Subida de avatar finalizada. Usuario: {UsuarioId}, Exito: {Exito}, Mensaje: {Mensaje}",
                usuario.Id,
                resultado.Ok,
                resultado.Mensaje);

            return Json(new { success = resultado.Ok, mensaje = resultado.Mensaje, rutaFoto = resultado.RutaRelativa });
        }

        #endregion

        private static List<SelectListItem> ObtenerEstados(int? seleccionado)
        {
            return Enum.GetValues<EstadoCursada>()
                .Select(e => new SelectListItem(
                    e.ToString(),
                    ((int)e).ToString(),
                    seleccionado.HasValue && (int)e == seleccionado.Value))
                .ToList();
        }
    }
}
using AppAlumnos.Data;
using AppAlumnos.Models;
using AppAlumnos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AppAlumnos.Controllers
{
    [Authorize]
    public class AutogestionController : Controller
    {
        private readonly AppAlumnosContext _contexto;
        private readonly UserManager<Usuario> _userManager;
        private readonly ArchivoService _archivoService;
        private readonly CertificadoService _certificadoService;

        public AutogestionController(
            AppAlumnosContext contexto,
            UserManager<Usuario> userManager,
            ArchivoService archivoService,
            CertificadoService certificadoService)
        {
            _contexto = contexto;
            _userManager = userManager;
            _archivoService = archivoService;
            _certificadoService = certificadoService;
        }

        // GET: Autogestion/Index
        public IActionResult Index()
        {
            return View();
        }

        #region Alumno

        // GET: Autogestion/InscripcionMaterias
        [Authorize(Roles = "Alumno,Administrador")]
        public async Task<IActionResult> InscripcionMaterias()
        {
            var anioActual = DateTime.Today.Year;
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var inscriptas = await _contexto.Cursadas
                .Where(c => c.UsuarioId == usuario.Id && c.AnioLectivo == anioActual)
                .Select(c => c.MateriaId)
                .ToListAsync();

            var materias = await _contexto.Materias
                .Where(m => !inscriptas.Contains(m.Id))
                .OrderBy(m => m.Anio)
                .ThenBy(m => m.Nombre)
                .ToListAsync();

            ViewBag.AnioActual = anioActual;
            return PartialView("_InscripcionMateriasPartial", materias);
        }

        // POST: Autogestion/Inscribirse
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Alumno,Administrador")]
        public async Task<IActionResult> Inscribirse(int materiaId)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var materia = await _contexto.Materias.FindAsync(materiaId);
            if (materia == null)
            {
                return Json(new { success = false, mensaje = "La materia no existe." });
            }

            var anioActual = DateTime.Today.Year;

            if (await _contexto.Cursadas.AnyAsync(c =>
                    c.UsuarioId == usuario.Id &&
                    c.MateriaId == materiaId &&
                    c.AnioLectivo == anioActual))
            {
                return Json(new { success = false, mensaje = "Ya te encuentras inscripto/a en esta materia para el año actual." });
            }

            _contexto.Cursadas.Add(new Cursada
            {
                UsuarioId = usuario.Id,
                MateriaId = materiaId,
                AnioLectivo = anioActual,
                Estado = EstadoCursada.Cursando
            });

            try
            {
                await _contexto.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Json(new { success = false, mensaje = "Ya te encuentras inscripto/a en esta materia para el año actual." });
            }

            return Json(new { success = true, mensaje = $"Inscripción confirmada en {materia.Nombre}." });
        }

        // GET: Autogestion/HistoriaAcademica?anio=&estado=
        [Authorize(Roles = "Alumno,Administrador")]
        public async Task<IActionResult> HistoriaAcademica(int? anio, EstadoCursada? estado)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var consulta = _contexto.Cursadas
                .AsNoTracking()
                .Include(c => c.Materia)
                .Where(c => c.UsuarioId == usuario.Id);

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
                .ToListAsync();

            var anios = await _contexto.Cursadas
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuario.Id)
                .Select(c => c.AnioLectivo)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();

            ViewBag.Anios = anios;
            ViewBag.AnioSeleccionado = anio;
            ViewBag.Estados = ObtenerEstados((int?)estado);
            return PartialView("_HistoriaAcademicaPartial", cursadas);
        }

        // GET: Autogestion/HistoriaImprimir
        [Authorize(Roles = "Alumno,Administrador")]
        public async Task<IActionResult> HistoriaImprimir()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var cursadas = await _contexto.Cursadas
                .AsNoTracking()
                .Include(c => c.Materia)
                .Where(c => c.UsuarioId == usuario.Id)
                .OrderByDescending(c => c.AnioLectivo)
                .ThenBy(c => c.Materia.Nombre)
                .ToListAsync();

            ViewBag.Titular = $"{usuario.Apellido}, {usuario.Nombre} - DNI {usuario.Dni}";
            return View(cursadas);
        }

        // GET: Autogestion/Certificados
        [Authorize(Roles = "Alumno,Administrador")]
        public async Task<IActionResult> Certificados()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var anioActual = DateTime.Today.Year;

            var esRegular = await _contexto.Cursadas.AnyAsync(c =>
                c.UsuarioId == usuario.Id &&
                c.AnioLectivo == anioActual &&
                (c.Estado == EstadoCursada.Cursando || c.Estado == EstadoCursada.Regular));

            var materiasAprobadas = await _contexto.Cursadas.CountAsync(c =>
                c.UsuarioId == usuario.Id &&
                c.Estado == EstadoCursada.Aprobada);

            ViewBag.EsRegular = esRegular;
            ViewBag.AnioActual = anioActual;
            ViewBag.MateriasAprobadas = materiasAprobadas;
            ViewBag.EsAdministrador = User.IsInRole("Administrador");
            return PartialView("_CertificadosPartial");
        }

        // GET: Autogestion/GenerarCertificado?tipo=regular|materias
        [Authorize(Roles = "Alumno,Administrador")]
        public async Task<IActionResult> GenerarCertificado(string tipo)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            var apellidoNombre = $"{usuario.Apellido}, {usuario.Nombre}";
            var anioActual = DateTime.Today.Year;

            if (tipo == "regular")
            {
                var esRegular = await _contexto.Cursadas.AnyAsync(c =>
                    c.UsuarioId == usuario.Id &&
                    c.AnioLectivo == anioActual &&
                    (c.Estado == EstadoCursada.Cursando || c.Estado == EstadoCursada.Regular));

                if (!esRegular)
                {
                    return Json(new { success = false, mensaje = "No consta tu condición de alumno regular en el año actual." });
                }

                var pdf = _certificadoService.GenerarCertificadoAlumnoRegular(apellidoNombre, usuario.Dni.ToString(), anioActual);
                return File(pdf, "application/pdf", $"certificado-alumno-regular-{anioActual}.pdf");
            }

            if (tipo == "materias")
            {
                var materias = await _contexto.Cursadas
                    .AsNoTracking()
                    .Include(c => c.Materia)
                    .Where(c => c.UsuarioId == usuario.Id && c.Estado == EstadoCursada.Aprobada)
                    .OrderBy(c => c.AnioLectivo)
                    .ThenBy(c => c.Materia.Nombre)
                    .Select(c => new { c.Materia.Nombre, Nota = c.Nota ?? 0, c.AnioLectivo })
                    .ToListAsync();

                if (materias.Count == 0)
                {
                    return Json(new { success = false, mensaje = "Todavía no tenés materias aprobadas para certificar." });
                }

                var listaMaterias = materias
                    .Select(m => (Materia: m.Nombre, m.Nota, Anio: m.AnioLectivo))
                    .ToList();

                var pdf = _certificadoService.GenerarCertificadoMateriasAprobadas(apellidoNombre, usuario.Dni.ToString(), listaMaterias);
                return File(pdf, "application/pdf", $"certificado-materias-aprobadas.pdf");
            }

            return Json(new { success = false, mensaje = "Tipo de certificado no válido." });
        }

        #endregion

        #region Docente

        // GET: Autogestion/MisMaterias
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> MisMaterias()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            IQueryable<Materia> consulta = _contexto.Materias.AsNoTracking();

            if (!User.IsInRole("Administrador"))
            {
                consulta = consulta.Where(m => m.DocenteId == usuario.Id);
            }

            var materias = await consulta
                .OrderBy(m => m.Anio)
                .ThenBy(m => m.Nombre)
                .ToListAsync();

            ViewBag.EsAdministrador = User.IsInRole("Administrador");
            return PartialView("_MisMateriasPartial", materias);
        }

        // GET: Autogestion/Inscriptos?materiaId=5
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> Inscriptos(int materiaId)
        {
            var materia = await ObtenerMateriaAutorizadaAsync(materiaId);
            if (materia == null) return NotFound();

            var inscriptos = await _contexto.Cursadas
                .AsNoTracking()
                .Include(c => c.Usuario)
                .Where(c => c.MateriaId == materiaId)
                .OrderBy(c => c.Usuario.Apellido)
                .ThenBy(c => c.Usuario.Nombre)
                .ToListAsync();

            ViewBag.MateriaNombre = materia.Nombre;
            ViewBag.MateriaId = materiaId;
            return PartialView("_InscriptosPartial", inscriptos);
        }

        // GET: Autogestion/ObtenerFormularioNotas?cursadaId=7 (contenido del modal)
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> ObtenerFormularioNotas(int cursadaId)
        {
            var cursada = await _contexto.Cursadas
                .AsNoTracking()
                .Include(c => c.Usuario)
                .Include(c => c.Materia)
                .FirstOrDefaultAsync(c => c.Id == cursadaId);

            if (cursada == null) return NotFound();

            if (!User.IsInRole("Administrador"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null || cursada.Materia.DocenteId != usuario.Id) return Forbid();
            }

            ViewBag.AlumnoNombre = $"{cursada.Usuario.Apellido}, {cursada.Usuario.Nombre}";
            ViewBag.MateriaNombre = cursada.Materia.Nombre;
            return PartialView("_FormularioNotasPortalPartial", cursada);
        }

        // POST: Autogestion/GuardarNotas
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> GuardarNotas(int id, string nota, EstadoCursada estado)
        {
            var cursada = await _contexto.Cursadas
                .Include(c => c.Materia)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cursada == null)
            {
                return Json(new { success = false, mensaje = "La cursada no existe." });
            }

            if (!User.IsInRole("Administrador"))
            {
                var usuario = await _userManager.GetUserAsync(User);
                if (usuario == null || cursada.Materia.DocenteId != usuario.Id)
                {
                    return Json(new { success = false, mensaje = "No tenés permisos para cargar notas en esta materia." });
                }
            }

            if (!string.IsNullOrWhiteSpace(nota))
            {
                var parseado = decimal.TryParse(nota, NumberStyles.Number, CultureInfo.InvariantCulture, out var valorNota)
                    || decimal.TryParse(nota, NumberStyles.Number, CultureInfo.CurrentCulture, out valorNota);

                if (!parseado || valorNota < 1 || valorNota > 10)
                {
                    return Json(new { success = false, mensaje = "Ingrese una nota válida entre 1 y 10." });
                }
                cursada.Nota = valorNota;
            }
            else
            {
                cursada.Nota = null;
            }

            cursada.Estado = estado;
            await _contexto.SaveChangesAsync();

            return Json(new { success = true, mensaje = "Notas guardadas correctamente." });
        }

        // GET: Autogestion/SeleccionarListado
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> SeleccionarListado()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            IQueryable<Materia> consulta = _contexto.Materias.AsNoTracking().OrderBy(m => m.Nombre);

            if (!User.IsInRole("Administrador"))
            {
                consulta = consulta.Where(m => m.DocenteId == usuario.Id);
            }

            var materias = await consulta.ToListAsync();

            ViewBag.Materias = new SelectList(materias, "Id", "Nombre");
            return PartialView("_SeleccionarListadoPartial");
        }

        // GET: Autogestion/ListadoInscriptos?materiaId=5 (página imprimible)
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> ListadoInscriptos(int materiaId)
        {
            var materia = await ObtenerMateriaAutorizadaAsync(materiaId);
            if (materia == null) return NotFound();

            var inscriptos = await _contexto.Cursadas
                .AsNoTracking()
                .Include(c => c.Usuario)
                .Where(c => c.MateriaId == materiaId)
                .OrderBy(c => c.Usuario.Apellido)
                .ThenBy(c => c.Usuario.Nombre)
                .ToListAsync();

            ViewBag.MateriaNombre = materia.Nombre;
            ViewBag.Cantidad = inscriptos.Count;
            var usuarioActual = await _userManager.GetUserAsync(User);
            ViewBag.DocenteNombre = usuarioActual != null ? $"{usuarioActual.Apellido}, {usuarioActual.Nombre}" : "";
            return View(inscriptos);
        }

        // GET: Autogestion/DescargarListadoPdf?materiaId=5
        [Authorize(Roles = "Docente,Administrador")]
        public async Task<IActionResult> DescargarListadoPdf(int materiaId)
        {
            var materia = await ObtenerMateriaAutorizadaAsync(materiaId);
            if (materia == null) return NotFound();

            var inscriptos = await _contexto.Cursadas
                .AsNoTracking()
                .Include(c => c.Usuario)
                .Where(c => c.MateriaId == materiaId)
                .OrderBy(c => c.Usuario.Apellido)
                .ThenBy(c => c.Usuario.Nombre)
                .Select(c => new
                {
                    ApellidoNombre = c.Usuario.Apellido + ", " + c.Usuario.Nombre,
                    c.Nota,
                    Estado = c.Estado.ToString()
                })
                .ToListAsync();

            var pdf = _certificadoService.GenerarListadoInscriptos(
                materia.Nombre,
                DateTime.Today.Year,
                inscriptos
                    .Select(x => (x.ApellidoNombre, x.Nota, x.Estado))
                    .ToList());

            return File(pdf, "application/pdf", $"listado-inscriptos-{materia.Nombre}.pdf");
        }

        #endregion

        #region Perfil

        // GET: Autogestion/MiPerfil
        public async Task<IActionResult> MiPerfil()
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            ViewBag.RolNombre = usuario.Rol;
            return PartialView("_MiPerfilPartial", usuario);
        }

        // POST: Autogestion/SubirAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirAvatar(IFormFile? foto)
        {
            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null) return Challenge();

            if (foto == null || foto.Length == 0)
            {
                return Json(new { success = false, mensaje = "Seleccioná una imagen." });
            }

            var resultado = await _archivoService.GuardarAvatarAsync(foto);

            if (!resultado.ok)
            {
                return Json(new { success = false, mensaje = resultado.mensaje });
            }

            if (!string.IsNullOrEmpty(usuario.RutaFoto) &&
                !string.Equals(usuario.RutaFoto, resultado.rutaRelativa, StringComparison.OrdinalIgnoreCase))
            {
                _archivoService.EliminarAvatar(usuario.RutaFoto);
            }

            usuario.RutaFoto = resultado.rutaRelativa;
            await _userManager.UpdateAsync(usuario);

            return Json(new { success = true, mensaje = "Foto de perfil actualizada.", rutaFoto = resultado.rutaRelativa });
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

        private async Task<Materia?> ObtenerMateriaAutorizadaAsync(int materiaId)
        {
            var materia = await _contexto.Materias
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == materiaId);

            if (materia == null) return null;

            if (User.IsInRole("Administrador")) return materia;

            var usuario = await _userManager.GetUserAsync(User);
            if (usuario == null || materia.DocenteId != usuario.Id) return null;

            return materia;
        }
    }
}
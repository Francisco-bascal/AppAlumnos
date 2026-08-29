using AppAlumnos.Data;
using AppAlumnos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AppAlumnos.Controllers
{
    public class MateriasController : Controller
    {
        private readonly AppAlumnosContext _contexto;

        public MateriasController(AppAlumnosContext contexto)
        {
            _contexto = contexto;
        }

        // GET: Materias
        public async Task<IActionResult> Index()
        {
            return View(await _contexto.Materias.ToListAsync());
        }

        // GET: Materias/ObtenerFormulario/0 (Crear) o /5 (Editar)
        [HttpGet]
        public async Task<IActionResult> ObtenerFormulario(int id = 0)
        {

            if (id == 0)
            {
                return PartialView("_FormularioMateriaPartial",
                new Materia());
            }

            var materia = await _contexto.Materias.FindAsync(id);
            if (materia == null) return NotFound();
            return PartialView("_FormularioMateriaPartial", materia);
        }

        // GET: Materias/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materia = await _contexto.Materias
                .FirstOrDefaultAsync(m => m.Id == id);
            if (materia == null)
            {
                return NotFound();
            }

            return View(materia);
        }

        // POST: Materias/Guardar (Procesa Crear y Editar vía AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(Materia materia)
        {
            if (ModelState.IsValid)
            {
                if (materia.Id == 0)
                {
                    _contexto.Add(materia);
                }
                else
                {
                    _contexto.Update(materia);
                }
                await _contexto.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    mensaje = "Materia guardada correctamente."
                });
            }
            // Si falla la validación, devolvemos la misma partial con los errores marcados
            return PartialView("_FormularioMateriaPartial", materia);
        }

        // POST: Materias/Delete/5
        // POST: Materias/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var materia = await _contexto.Materias.FindAsync(id);
            if (materia == null)
            {
                return Json(new { success = false, mensaje = "La materia no existe." });
            }
            _contexto.Materias.Remove(materia);
            await _contexto.SaveChangesAsync();
           
            return Json(new { success = true, mensaje = "Materia eliminada correctamente." });
        }

        private bool MateriaExiste(int id)
        {
            return _contexto.Materias.Any(e => e.Id == id);
        }
    }
}

/*
// GET: Materias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materia = await _contexto.Materias.FindAsync(id);
            if (materia == null)
            {
                return NotFound();
            }
            return View(materia);
        }

        // POST: Materias/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Anio")] Materia materia)
        {
            if (id != materia.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _contexto.Update(materia);
                    await _contexto.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MateriaExiste(materia.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw new Exception("Otra instancia se encuentra editando este mismo registro");
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(materia);
        }

                // GET: Materias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var materia = await _contexto.Materias
                .FirstOrDefaultAsync(m => m.Id == id);

            if (materia == null)
            {
                return NotFound();
            }

            return View(materia);
        }

        // GET: Materias/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Materias/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Anio")] Materia materia)
        {
            if (ModelState.IsValid)
            {
                _contexto.Add(materia);
                await _contexto.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(materia);
        }
*/
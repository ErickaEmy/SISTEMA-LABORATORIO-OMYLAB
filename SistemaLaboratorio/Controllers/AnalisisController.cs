using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    public class AnalisisController : Controller
    {
        private readonly DblaboratorioContext _context;

        public AnalisisController(DblaboratorioContext context)
        {
            _context = context;
        }

        // GET: Analisis
        public async Task<IActionResult> Index()
        {
            return View(await _context.Analisis.ToListAsync());
        }

        // GET: Analisis/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var analisis = await _context.Analisis
                .Include(a => a.AnalisisComponentes) // Incluye la tabla de unión
                    .ThenInclude(ac => ac.Componente) // Incluye el Componente relacionado
                .FirstOrDefaultAsync(m => m.AnalisisId == id);

            if (analisis == null)
            {
                return NotFound();
            }

            return View(analisis);
        }

        // GET: Analisis/Registrar
        public IActionResult Registrar()
        {
            return View();
        }

        // POST: Analisis/Registrar
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("AnalisisId,Nombre,TipoMuestra,Condicion,Comentario,Precio,Estado")] Analisis analisis)
        {
            if (ModelState.IsValid)
            {
                _context.Add(analisis);
                await _context.SaveChangesAsync();


                // 🔑 Registrar auditoría
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "Analisis",
                    Descripcion = "Nuevo análisis registrado",
                    Comentario = $"Nombre: {analisis.Nombre}, Tipo Muestra: {analisis.TipoMuestra}",
                    EntidadId = analisis.AnalisisId,
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _context.HistorialAuditoria.Add(auditoria);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(analisis);
        }

        // GET: Analisis/Actualizar/5
        public async Task<IActionResult> Actualizar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var analisis = await _context.Analisis.FindAsync(id);
            if (analisis == null)
            {
                return NotFound();
            }
            return View(analisis);
        }

        // POST: Analisis/Actualizar/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("AnalisisId,Nombre,TipoMuestra,Condicion,Comentario,Precio,Estado")] Analisis analisis)
        {
            if (id != analisis.AnalisisId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(analisis);
                    await _context.SaveChangesAsync();

                    // 🔑 Registrar auditoría
                    var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                    var auditoria = new HistorialAuditoria
                    {
                        Actividad = "Analisis",
                        Descripcion = "Actualización de análisis",
                        Comentario = $"Nombre: {analisis.Nombre}, Tipo Muestra: {analisis.TipoMuestra}",
                        EntidadId = analisis.AnalisisId,
                        Accion = "Actualizar",
                        Fecha = DateTime.Now,
                        EmpleadoId = empleadoId
                    };
                    _context.HistorialAuditoria.Add(auditoria);
                    await _context.SaveChangesAsync();


                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnalisisExists(analisis.AnalisisId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(analisis);
        }

        // GET: Analisis/RegistrarComponentes/5
        public async Task<IActionResult> RegistrarComponente(int? id)
        {
            if (id == null) return NotFound();

            var analisis = await _context.Analisis.FindAsync(id);
            if (analisis == null) return NotFound();

            ViewBag.Analisis = analisis;
            ViewBag.Componentes = await _context.Componente.ToListAsync();
            ViewBag.ComponentesAsociados = await _context.AnalisisComponente
                .Include(ac => ac.Componente)
                .Where(ac => ac.AnalisisId == id)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarComponente(int analisisId, int componenteId)
        {
            bool yaExiste = await _context.AnalisisComponente
                .AnyAsync(ac => ac.AnalisisId == analisisId && ac.ComponenteId == componenteId);

            if (yaExiste)
            {
                TempData["Error"] = "El componente ya está asociado a este análisis.";
                return RedirectToAction("RegistrarComponente", new { id = analisisId });
            }

            var nuevo = new AnalisisComponente
            {
                AnalisisId = analisisId,
                ComponenteId = componenteId
            };

            _context.Add(nuevo);
            await _context.SaveChangesAsync();

            return RedirectToAction("RegistrarComponente", new { id = analisisId });
        }

        // POST: Analisis/Eliminar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var analisis = await _context.Analisis.FindAsync(id);
            if (analisis == null)
            {
                return NotFound();
            }

            _context.Analisis.Remove(analisis);
            await _context.SaveChangesAsync();

            // 🔑 Registrar auditoría para Eliminar Analisis
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            var auditoriaEliminar = new HistorialAuditoria
            {
                Actividad = "Analisis",
                Descripcion = "Análisis eliminado",
                Comentario = $"Nombre: {analisis.Nombre}, Tipo Muestra: {analisis.TipoMuestra}",
                EntidadId = analisis.AnalisisId,
                Accion = "Eliminar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _context.HistorialAuditoria.Add(auditoriaEliminar);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool AnalisisExists(int id)
        {
            return _context.Analisis.Any(e => e.AnalisisId == id);
        }
    }
}

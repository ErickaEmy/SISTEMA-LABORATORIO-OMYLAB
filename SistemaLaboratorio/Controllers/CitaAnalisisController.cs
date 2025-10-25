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
    public class CitaAnalisisController : Controller
    {
        private readonly DblaboratorioContext _context;

        public CitaAnalisisController(DblaboratorioContext context)
        {
            _context = context;
        }


        // GET: CitaAnalisis/Registrar
        public IActionResult Registrar(int? citaId = null)
        {
            // ——— LISTA DE ANÁLISIS ———
            var listaAnalisis = new SelectList(_context.Analisis, "AnalisisId", "Nombre");
            ViewData["AnalisisId"] = listaAnalisis;      //  <-  usaremos ViewData en la vista
                                                         // (si prefieres ViewBag, simplemente añade  ViewBag.AnalisisId = listaAnalisis)

            // ——— CITA ———
            if (citaId.HasValue)
            {
                ViewBag.FijarCita = true;
                ViewData["CitaId"] = citaId.Value;       // hidden en la vista
            }
            else
            {
                ViewData["CitaId"] = new SelectList(_context.Cita, "CitaId", "Estado");
            }

            return View();
        }

        // POST: CitaAnalisis/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar(
            [Bind("CitaAnalisisId,CitaId,AnalisisId")] CitaAnalisis citaAnalisis,
            string continuar = "no")
        {
            if (!ModelState.IsValid)
            {
                RecargarCombos(citaAnalisis.CitaId, citaAnalisis.AnalisisId);
                return View(citaAnalisis);
            }

            _context.Add(citaAnalisis);
            await _context.SaveChangesAsync();

            if (continuar == "si")
            {
                TempData["Mensaje"] = "Análisis registrado correctamente.";
                return RedirectToAction("Create", new { citaId = citaAnalisis.CitaId });
            }

            return RedirectToAction("Index", "Cita");
        }

        private void RecargarCombos(int citaId, int analisisId)
        {
            ViewData["AnalisisId"] = new SelectList(_context.Analisis, "AnalisisId", "Nombre", analisisId);
            ViewData["CitaId"] = new SelectList(_context.Cita, "CitaId", "Estado", citaId);
        }
    }
}

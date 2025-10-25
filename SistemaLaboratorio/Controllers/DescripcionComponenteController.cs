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
    public class DescripcionComponenteController : Controller
    {
        private readonly DblaboratorioContext _context;

        public DescripcionComponenteController(DblaboratorioContext context)
        {
            _context = context;
        }

        
        // GET: DescripcionComponente/Registrar
        public IActionResult Registrar()
        {
            ViewData["ComponenteId"] = new SelectList(_context.Componente, "ComponenteId", "Categoria");
            return View();
        }

        // POST: DescripcionComponente/Registrar
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("DescripcionComponenteId,ComponenteId,ValorMinimo,ValorMaximo,Sexo,EdadMinima,EdadMaxima")] DescripcionComponente descripcionComponente)
        {
            if (ModelState.IsValid)
            {
                _context.Add(descripcionComponente);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ComponenteId"] = new SelectList(_context.Componente, "ComponenteId", "Categoria", descripcionComponente.ComponenteId);
            return View(descripcionComponente);
        }
    }
}

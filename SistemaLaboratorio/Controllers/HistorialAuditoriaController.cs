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
    public class HistorialAuditoriaController : Controller
    {
        private readonly DblaboratorioContext _context;

        public HistorialAuditoriaController(DblaboratorioContext context)
        {
            _context = context;
        }

        // GET: HistorialAuditoria
        public async Task<IActionResult> Index()
        {
            var dblaboratorioContext = _context.HistorialAuditoria.Include(h => h.Empleado);
            return View(await dblaboratorioContext.ToListAsync());
        }
    }
}

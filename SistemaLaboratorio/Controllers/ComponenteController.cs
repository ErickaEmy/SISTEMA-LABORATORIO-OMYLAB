using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    public class ComponenteController : Controller
    {
        private readonly DblaboratorioContext _context;

        public ComponenteController(DblaboratorioContext context)
        {
            _context = context;
        }

        // Acción principal que lista todos los componentes registrados
        public async Task<IActionResult> Index()
        {
            return View(await _context.Componente.ToListAsync());
        }

        // Muestra los detalles de un componente específico
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null) return NotFound();

            var componente = await _context.Componente
                .Include(c => c.DescripcionComponentes)
                .Include(c => c.ReactivoComponentes)
                    .ThenInclude(rc => rc.Reactivo)
                .FirstOrDefaultAsync(m => m.ComponenteId == id);

            if (componente == null) return NotFound();

            return View(componente);
        }

        // Vista del formulario para registrar un nuevo componente
        public IActionResult Registrar()
        {
            return View();
        }

        // Procesa el registro de un nuevo componente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("ComponenteId,Nombre,Categoria")] Componente componente, int cantidadValores)
        {
            if (!ModelState.IsValid || cantidadValores < 1)
            {
                if (cantidadValores < 1)
                    ViewBag.CantidadError = "Debe ingresar un número mayor o igual a 1.";
                return View(componente);
            }

            _context.Add(componente);
            await _context.SaveChangesAsync();
            TempData["ComponenteId"] = componente.ComponenteId;
            TempData["CantidadValores"] = cantidadValores;

            // 🔑 Registrar auditoría
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            var auditoria = new HistorialAuditoria
            {
                Actividad = "Componente",
                Descripcion = "Componente registrado",
                Comentario = $"Nombre: {componente.Nombre}, Categoría: {componente.Categoria}",
                EntidadId = componente.ComponenteId,
                Accion = "Registrar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _context.HistorialAuditoria.Add(auditoria);
            await _context.SaveChangesAsync();

            return RedirectToAction("RegistrarValorReferencia");
        }


        // Paso siguiente luego de registrar el componente: ingresar cantidad de valores de referencia
        public IActionResult IngresarCantidadValoresReferencia()
        {
            if (TempData["ComponenteIdRecienRegistrado"] == null)
                return RedirectToAction("Index");

            ViewBag.ComponenteId = TempData["ComponenteIdRecienRegistrado"];
            return View();
        }

        // Procesa el ingreso de cantidad de valores de referencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IngresarCantidadValoresReferencia(int componenteId, int cantidad)
        {
            if (cantidad < 1)
            {
                ModelState.AddModelError("cantidad", "Debe ingresar un número entero válido mayor o igual a 1.");
                ViewBag.ComponenteId = componenteId;
                return View();
            }
            TempData["CantidadValores"] = cantidad;
            TempData["ComponenteId"] = componenteId;
            return RedirectToAction("RegistrarValorReferencia");
        }

        // Vista para ingresar valores de referencia uno por uno
        public IActionResult RegistrarValorReferencia()
        {
            if (TempData["CantidadValores"] == null || TempData["ComponenteId"] == null)
                return RedirectToAction("Index");

            int cantidad = (int)TempData["CantidadValores"];
            int componenteId = (int)TempData["ComponenteId"];

            TempData.Keep("CantidadValores");
            TempData.Keep("ComponenteId");

            ViewBag.Restantes = cantidad;
            ViewBag.ComponenteId = componenteId;




            return View();
        }

        // Procesa un formulario de valor de referencia
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarValorReferencia(DescripcionComponente valor)
        {
            // Recuperar si llega nulo
            if (valor.ComponenteId == 0 && TempData["ComponenteId"] != null)
                valor.ComponenteId = (int)TempData["ComponenteId"];
            
            
            ModelState.Remove("Componente");

            if (!ModelState.IsValid)
            {
                ViewBag.Restantes = TempData["CantidadValores"];
                ViewBag.ComponenteId = valor.ComponenteId;
                TempData.Keep(); // Mantener para la próxima solicitud
                return View(valor);
            }

            _context.DescripcionComponente.Add(valor);
            await _context.SaveChangesAsync();

            int restantes = (int)TempData["CantidadValores"] - 1;
            TempData["CantidadValores"] = restantes;
            TempData["ComponenteId"] = valor.ComponenteId;
            TempData.Keep();

            

            return RedirectToAction("RegistrarValorReferencia");
        }



        // Vista para actualizar un componente
        public async Task<IActionResult> Actualizar(int? id)
        {
            if (id == null) return NotFound();

            var componente = await _context.Componente
                .Include(c => c.DescripcionComponentes)
                .FirstOrDefaultAsync(c => c.ComponenteId == id);

            if (componente == null) return NotFound();

            return View(componente); // pasamos el componente completo
        }

        // Procesa la actualización del componente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("ComponenteId,Nombre,Categoria")] Componente componente)
        {
            if (id != componente.ComponenteId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(componente);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExisteComponente(componente.ComponenteId)) return NotFound();
                    else throw;
                }

                // 🔑 Registrar auditoría
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "Componente",
                    Descripcion = "Componente actualizado",
                    Comentario = $"Nombre: {componente.Nombre}, Categoría: {componente.Categoria}",
                    EntidadId = componente.ComponenteId,
                    Accion = "Actualizar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _context.HistorialAuditoria.Add(auditoria);
                await _context.SaveChangesAsync();


                return RedirectToAction("Index");
            }
            return View(componente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarValorReferencia(int id, int componenteId)
        {
            var valor = await _context.DescripcionComponente.FindAsync(id);
            if (valor != null)
            {
                _context.DescripcionComponente.Remove(valor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Actualizar", new { id = componenteId });
        }
        
        public async Task<IActionResult> RegistrarReactivos(int? id)
        {
            if (id == null) return NotFound();

            var componente = await _context.Componente.FindAsync(id);
            if (componente == null) return NotFound();

            ViewBag.Componente = componente;
            ViewBag.Reactivos = await _context.Reactivo.ToListAsync(); // Suponiendo que tienes esta tabla
            ViewBag.ReactivosAsociados = await _context.ReactivoComponente
                .Include(cr => cr.Reactivo)
                .Where(cr => cr.ComponenteId == id)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistrarReactivoAsociado(int componenteId, int reactivoId, int cantidad)
        {
            if (cantidad < 1)
            {
                TempData["Error"] = "Cantidad debe ser mayor o igual a 1.";
                return RedirectToAction("RegistrarReactivos", new { id = componenteId });
            }

            bool yaExiste = await _context.ReactivoComponente
                .AnyAsync(cr => cr.ComponenteId == componenteId && cr.ReactivoId == reactivoId);

            if (yaExiste)
            {
                TempData["Error"] = "El reactivo ya está asociado a este componente.";
                return RedirectToAction("RegistrarReactivos", new { id = componenteId });
            }

            var nuevo = new ReactivoComponente
            {
                ComponenteId = componenteId,
                ReactivoId = reactivoId,
                Cantidad = cantidad
            };

            _context.Add(nuevo);
            await _context.SaveChangesAsync();

            return RedirectToAction("RegistrarReactivos", new { id = componenteId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var componente = await _context.Componente
                .Include(c => c.DescripcionComponentes)
                .FirstOrDefaultAsync(c => c.ComponenteId == id);

            if (componente == null)
            {
                return NotFound();
            }

            // Eliminar dependencias
            _context.DescripcionComponente.RemoveRange(componente.DescripcionComponentes);
            _context.Componente.Remove(componente);

            // Registrar auditoría
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            var auditoria = new HistorialAuditoria
            {
                Actividad = "Componente",
                Descripcion = "Componente eliminado",
                Comentario = $"Nombre: {componente.Nombre}, Categoría: {componente.Categoria}",
                EntidadId = componente.ComponenteId,
                Accion = "Eliminar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _context.HistorialAuditoria.Add(auditoria);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        // Verifica si existe un componente con el ID dado
        private bool ExisteComponente(int id)
        {
            return _context.Componente.Any(e => e.ComponenteId == id);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    /// <summary>
    /// Controlador para la gestión de reactivos del laboratorio.
    /// Permite registrar, actualizar, eliminar y listar reactivos.
    /// </summary>
    public class ReactivoController : Controller
    {
        /// <summary>
        /// Contexto de base de datos del laboratorio.
        /// </summary>
        private readonly DblaboratorioContext _contexto;

        /// <summary>
        /// Constructor que inicializa el contexto de base de datos.
        /// </summary>
        /// <param name="contexto">Contexto inyectado.</param>
        public ReactivoController(DblaboratorioContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Acción que muestra la lista de todos los reactivos.
        /// </summary>
        /// <returns>Vista con lista de reactivos.</returns>
        public async Task<IActionResult> Index()
        {
            var listaReactivos = await _contexto.Reactivo.ToListAsync();
            return View(listaReactivos);
        }

        /// <summary>
        /// Acción GET que muestra el formulario para registrar un nuevo reactivo.
        /// </summary>
        /// <returns>Vista de formulario de registro.</returns>
        public IActionResult Registrar()
        {
            return View();
        }

        /// <summary>
        /// Acción POST que guarda el reactivo registrado.
        /// Calcula automáticamente la FechaIngreso, CantidadTotal, CapacidadTotal y Disponibilidad.
        /// </summary>
        /// <param name="reactivo">Reactivo con datos desde el formulario.</param>
        /// <returns>Redirige a Index si es exitoso.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("Nombre,Cantidad,Capacidad,FechaVencimiento,Proveedor,Presentacion")] Reactivo reactivo)
        {
            if (ModelState.IsValid)
            {
                // Registrar fecha de ingreso como fecha actual.
                reactivo.FechaIngreso = DateOnly.FromDateTime(DateTime.Now);

                // Calcular cantidad total como Cantidad * Capacidad.
                reactivo.CantidadTotal = reactivo.Cantidad * reactivo.Capacidad;

                reactivo.CapacidadTotal = 0;
                reactivo.Disponibilidad = 0;

                // Guardar en base de datos.
                _contexto.Add(reactivo);
                await _contexto.SaveChangesAsync();

                // 🔑 Registrar auditoría
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "Reactivo",
                    Descripcion = "Reactivo registrado",
                    Comentario = $"Nombre: {reactivo.Nombre}, Cantidad: {reactivo.Cantidad}, Capacidad: {reactivo.Capacidad}",
                    EntidadId = reactivo.ReactivoId,
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _contexto.HistorialAuditoria.Add(auditoria);
                await _contexto.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            return View(reactivo);
        }

        /// <summary>
        /// Acción GET que muestra el formulario para actualizar datos permitidos de un reactivo.
        /// Solo permite editar Cantidad, Proveedor y Presentación.
        /// </summary>
        /// <param name="id">Identificador del reactivo.</param>
        /// <returns>Vista de actualización si existe; NotFound si no existe.</returns>
        public async Task<IActionResult> Actualizar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reactivo = await _contexto.Reactivo.FindAsync(id);
            if (reactivo == null)
            {
                return NotFound();
            }

            // ✅ Preparar fechas formateadas (yyyy-MM-dd) para input type="date"
            ViewBag.FechaIngreso = reactivo.FechaIngreso.ToString("yyyy-MM-dd");
            ViewBag.FechaVencimiento = reactivo.FechaVencimiento.ToString("yyyy-MM-dd");

            return View(reactivo);
        }


        /// <summary>
        /// Acción POST que actualiza solo los campos permitidos de un reactivo.
        /// </summary>
        /// <param name="id">Identificador del reactivo.</param>
        /// <param name="reactivoForm">Reactivo con campos editables desde el formulario.</param>
        /// <returns>Redirige a Index si es exitoso.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("ReactivoId,Nombre,FechaIngreso,FechaVencimiento,Capacidad,Cantidad,Proveedor,Presentacion,CapacidadTotal, CantidadTotal, Disponibilidad")] Reactivo reactivoForm)
        {
            if (id != reactivoForm.ReactivoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Buscar el reactivo original
                var reactivoOriginal = await _contexto.Reactivo
                    .FirstOrDefaultAsync(r => r.ReactivoId == id);

                if (reactivoOriginal == null)
                {
                    return NotFound();
                }

                // ✅ Actualizar solo campos permitidos
                reactivoOriginal.Nombre = reactivoForm.Nombre;
                reactivoOriginal.FechaIngreso = reactivoForm.FechaIngreso;  
                reactivoOriginal.FechaVencimiento = reactivoForm.FechaVencimiento;
                reactivoOriginal.Capacidad = reactivoOriginal.Capacidad; // Mantener la capacidad original
                reactivoOriginal.Cantidad = reactivoForm.Cantidad;
                reactivoOriginal.Proveedor = reactivoForm.Proveedor;
                reactivoOriginal.Presentacion = reactivoForm.Presentacion;
                reactivoOriginal.CapacidadTotal = reactivoForm.CapacidadTotal;// Mantener la capacidad total original
                reactivoOriginal.Disponibilidad = reactivoOriginal.Cantidad; // Mantener la disponibilidad original
                // ✅ Recalcular CantidadTotal
                reactivoOriginal.CantidadTotal = reactivoOriginal.Cantidad * reactivoOriginal.Capacidad;

                // Guardar cambios
                await _contexto.SaveChangesAsync();


                // 🔑 Registrar auditoría
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "Reactivo",
                    Descripcion = "Reactivo actualizado",
                    Comentario = $"Nombre: {reactivoOriginal.Nombre}, Nueva Cantidad: {reactivoOriginal.Cantidad}, Proveedor: {reactivoOriginal.Proveedor}",
                    EntidadId = reactivoOriginal.ReactivoId,
                    Accion = "Actualizar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _contexto.HistorialAuditoria.Add(auditoria);
                await _contexto.SaveChangesAsync();


                return RedirectToAction(nameof(Index));
            }

            // Si falla, recargar fechas para mostrarlas de nuevo
            var reactivoReload = await _contexto.Reactivo.FindAsync(id);
            if (reactivoReload != null)
            {
                ViewBag.FechaIngreso = reactivoReload.FechaIngreso.ToString("yyyy-MM-dd");
                ViewBag.FechaVencimiento = reactivoReload.FechaVencimiento.ToString("yyyy-MM-dd");
            }

            return View(reactivoForm);
        }

        /// <summary>
        /// Acción GET que muestra todos los detalles de un reactivo.
        /// </summary>
        /// <param name="id">Identificador del reactivo.</param>
        /// <returns>Vista con los datos del reactivo si existe; NotFound si no existe.</returns>
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Buscar reactivo por su ID.
            var reactivo = await _contexto.Reactivo
                .FirstOrDefaultAsync(m => m.ReactivoId == id);

            if (reactivo == null)
            {
                return NotFound();
            }

            // Retornar vista con el modelo encontrado.
            return View(reactivo);
        }



        /// <summary>
        /// Acción POST que elimina un reactivo desde el modal de confirmación.
        /// </summary>
        /// <param name="id">Identificador del reactivo.</param>
        /// <returns>Redirige a Index.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Reactivo/Eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var reactivo = await _contexto.Reactivo.FindAsync(id);
            if (reactivo != null)
            {
                _contexto.Reactivo.Remove(reactivo);
                await _contexto.SaveChangesAsync();

                // 🔑 Registrar auditoría
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "Reactivo",
                    Descripcion = "Reactivo eliminado",
                    Comentario = $"Nombre: {reactivo.Nombre}, Cantidad: {reactivo.Cantidad}, Proveedor: {reactivo.Proveedor}",
                    EntidadId = reactivo.ReactivoId,
                    Accion = "Eliminar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _contexto.HistorialAuditoria.Add(auditoria);
                await _contexto.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }



        /// <summary>
        /// Método privado para verificar si existe un reactivo.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <returns>True si existe, False si no.</returns>
        private bool ExisteReactivo(int id)
        {
            return _contexto.Reactivo.Any(e => e.ReactivoId == id);
        }
    }
}

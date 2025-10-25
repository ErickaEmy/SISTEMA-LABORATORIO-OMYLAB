using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    /// <summary>
    /// Controlador para la gestión de insumos del laboratorio.
    /// Permite registrar, actualizar, eliminar y listar insumos.
    /// </summary>
    public class InsumoController : Controller
    {
        /// <summary>
        /// Contexto de base de datos del laboratorio.
        /// </summary>
        private readonly DblaboratorioContext _contexto;

        /// <summary>
        /// Constructor que inicializa el contexto de base de datos.
        /// </summary>
        /// <param name="contexto">Contexto inyectado.</param>
        public InsumoController(DblaboratorioContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Acción que muestra la lista de todos los insumos.
        /// </summary>
        /// <returns>Vista con lista de insumos.</returns>
        public async Task<IActionResult> Index()
        {
            var listaInsumos = await _contexto.Insumo.ToListAsync();
            return View(listaInsumos);
        }

        /// <summary>
        /// Acción GET que muestra todos los detalles de un insumo.
        /// </summary>
        /// <param name="id">Identificador del insumo.</param>
        /// <returns>Vista con datos del insumo si existe; NotFound si no existe.</returns>
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insumo = await _contexto.Insumo
                .FirstOrDefaultAsync(i => i.InsumoId == id);

            if (insumo == null)
            {
                return NotFound();
            }

            return View(insumo);
        }

        /// <summary>
        /// Acción GET que muestra el formulario para registrar un nuevo insumo.
        /// </summary>
        /// <returns>Vista de formulario de registro.</returns>
        public IActionResult Registrar()
        {
            return View();
        }

        /// <summary>
        /// Acción POST que guarda el insumo registrado.
        /// </summary>
        /// <param name="insumo">Insumo con datos desde el formulario.</param>
        /// <returns>Redirige a Index si es exitoso.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("Nombre,Descripcion,CantidadDisponible,UnidadMedida,FechaVencimiento,Estado")] Insumo insumo)
        {
            if (ModelState.IsValid)
            {
                _contexto.Add(insumo);
                await _contexto.SaveChangesAsync();

                // 🔑 Registrar auditoría para Registrar Insumo
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoriaRegistrar = new HistorialAuditoria
                {
                    Actividad = "Insumo",
                    Descripcion = "Insumo registrado",
                    Comentario = $"Nombre: {insumo.Nombre}, Cantidad: {insumo.CantidadDisponible}, Unidad: {insumo.UnidadMedida}",
                    EntidadId = insumo.InsumoId,
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _contexto.HistorialAuditoria.Add(auditoriaRegistrar);
                await _contexto.SaveChangesAsync();


                return RedirectToAction(nameof(Index));
            }
            return View(insumo);
        }

        /// <summary>
        /// Acción GET que muestra el formulario para actualizar datos permitidos de un insumo.
        /// Solo permite editar Descripción, CantidadDisponible, UnidadMedida y Estado.
        /// </summary>
        /// <param name="id">Identificador del insumo.</param>
        /// <returns>Vista de actualización si existe; NotFound si no existe.</returns>
        public async Task<IActionResult> Actualizar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var insumo = await _contexto.Insumo.FindAsync(id);
            if (insumo == null)
            {
                return NotFound();
            }

            // ✅ Preparar fecha formateada para input type="date"
            ViewBag.FechaVencimiento = insumo.FechaVencimiento.ToString("yyyy-MM-dd");

            return View(insumo);
        }

        /// <summary>
        /// Acción POST que actualiza solo los campos permitidos de un insumo.
        /// </summary>
        /// <param name="id">Identificador del insumo.</param>
        /// <param name="insumoForm">Insumo con campos editables desde el formulario.</param>
        /// <returns>Redirige a Index si es exitoso.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("InsumoId,Nombre,Descripcion,CantidadDisponible,UnidadMedida,FechaVencimiento,Estado")] Insumo insumoForm)
        {
            if (id != insumoForm.InsumoId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Buscar el insumo original
                var insumoOriginal = await _contexto.Insumo
                    .FirstOrDefaultAsync(i => i.InsumoId == id);

                if (insumoOriginal == null)
                {
                    return NotFound();
                }

                // ✅ Actualizar solo campos permitidos
                insumoOriginal.Descripcion = insumoForm.Descripcion;
                insumoOriginal.CantidadDisponible = insumoForm.CantidadDisponible;
                insumoOriginal.UnidadMedida = insumoForm.UnidadMedida;
                insumoOriginal.Estado = insumoForm.Estado;
                // Mantener Nombre y FechaVencimiento originales

                // Guardar cambios
                await _contexto.SaveChangesAsync();

                // 🔑 Registrar auditoría para Actualizar Insumo
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoriaActualizar = new HistorialAuditoria
                {
                    Actividad = "Insumo",
                    Descripcion = "Insumo actualizado",
                    Comentario = $"Nombre: {insumoOriginal.Nombre}, Nueva Cantidad: {insumoOriginal.CantidadDisponible}, Nueva Unidad: {insumoOriginal.UnidadMedida}, Estado: {insumoOriginal.Estado}",
                    EntidadId = insumoOriginal.InsumoId,
                    Accion = "Actualizar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _contexto.HistorialAuditoria.Add(auditoriaActualizar);
                await _contexto.SaveChangesAsync();


                return RedirectToAction(nameof(Index));
            }

            // Si falla, recargar fecha para mostrarla de nuevo
            var insumoReload = await _contexto.Insumo.FindAsync(id);
            if (insumoReload != null)
            {
                ViewBag.FechaVencimiento = insumoReload.FechaVencimiento.ToString("yyyy-MM-dd");
            }

            return View(insumoForm);
        }

        /// <summary>
        /// Acción POST que elimina un insumo desde el modal de confirmación.
        /// </summary>
        /// <param name="id">Identificador del insumo.</param>
        /// <returns>Redirige a Index.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var insumo = await _contexto.Insumo.FindAsync(id);
            if (insumo == null)
            {
                return NotFound();
            }

            _contexto.Insumo.Remove(insumo);
            await _contexto.SaveChangesAsync();

            // 🔑 Registrar auditoría para Eliminar Insumo
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            var auditoriaEliminar = new HistorialAuditoria
            {
                Actividad = "Insumo",
                Descripcion = "Insumo eliminado",
                Comentario = $"Nombre: {insumo.Nombre}, Cantidad: {insumo.CantidadDisponible}, Unidad: {insumo.UnidadMedida}",
                EntidadId = insumo.InsumoId,
                Accion = "Eliminar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _contexto.HistorialAuditoria.Add(auditoriaEliminar);


            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Método privado para verificar si existe un insumo.
        /// </summary>
        /// <param name="id">Identificador.</param>
        /// <returns>True si existe, False si no.</returns>
        private bool ExisteInsumo(int id)
        {
            return _contexto.Insumo.Any(e => e.InsumoId == id);
        }
    }
}

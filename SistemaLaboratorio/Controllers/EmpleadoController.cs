using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;
using Microsoft.AspNetCore.Authorization;
using iText.Commons.Actions.Contexts;

namespace SistemaLaboratorio.Controllers
{
    [Authorize(Roles = "Administrador, Supervisor")]
    public class EmpleadoController : Controller
    {
        private readonly DblaboratorioContext _context;

        public EmpleadoController(DblaboratorioContext context)
        {
            _context = context;
        }

        // GET: Empleado
        public async Task<IActionResult> Index()
        {
            return View(await _context.Empleado.ToListAsync());
        }

        // GET: Empleado/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleado
                .FirstOrDefaultAsync(m => m.EmpleadoId == id);
            if (empleado == null)
            {
                return NotFound();
            }

            return View(empleado);
        }

        // GET: Empleado/Registrar
        public IActionResult Registrar()
        {
            ViewBag.Roles = new SelectList(new[] { "Administrador", "Supervisor", "Biologo", "Recepcionista" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });
            return View();
        }

        // POST: Empleado/Registrar

        /// <summary>
        /// Acción que permite registrar un nuevo empleado en el sistema.
        /// Genera automáticamente el nombre de usuario según el nombre y apellidos,
        /// asigna como contraseña el DNI hasheado, y valida el modelo.
        /// </summary>
        /// <param name="empleado">Objeto Empleado recibido desde el formulario de registro.</param>
        /// <returns>Vista Index si el registro fue exitoso; de lo contrario, la vista de registro con errores.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("EmpleadoId,Nombre,Apellidos,Dni,FechaNacimiento,Celular,Correo,Direccion,Rol,Estado")] Empleado empleado)
        {
            // Separar nombres y apellidos
            var nombres = empleado.Nombre.Trim().Split(' ');
            var apellidos = empleado.Apellidos.Trim().Split(' ');

            string primerNombre = nombres[0];
            string primerApellido = apellidos[0];
            string segundoApellido = apellidos.Length > 1 ? apellidos[1] : "";

            // Generar nombre de usuario base
            string baseUsuario = (primerNombre[0] + primerApellido).ToLower();
            string usuario = baseUsuario;

            int contador = 1;
            int extra = 0;

            // Verificar si ya existe y generar uno único
            while (_context.Empleado.Any(e => e.Usuario == usuario))
            {
                if (segundoApellido.Length >= extra + 1)
                {
                    usuario = baseUsuario + segundoApellido.Substring(0, ++extra).ToLower();
                }
                else
                {
                    usuario = baseUsuario + segundoApellido.ToLower() + (contador++);
                }
            }

            // ✅ Asignar antes de validar el modelo
            empleado.Usuario = usuario;
            empleado.Contrasena = empleado.Dni.ToString(); // Puedes aplicar hash aquí si lo deseas

            // Eliminar errores previos en el ModelState para esos campos
            ModelState.Remove(nameof(empleado.Usuario));
            ModelState.Remove(nameof(empleado.Contrasena));

            // Validar modelo con todos los campos completos
            if (ModelState.IsValid)
            {
                _context.Add(empleado);
                await _context.SaveChangesAsync();

                // Registrar auditoría
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "Empleado",
                    Descripcion = "Empleado registrado",
                    Comentario = $"Nombre: {empleado.Nombre} {empleado.Apellidos}, DNI: {empleado.Dni}, Usuario: {empleado.Usuario}",
                    EntidadId = empleado.EmpleadoId,
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _context.HistorialAuditoria.Add(auditoria);
                await _context.SaveChangesAsync();


                return RedirectToAction(nameof(Index));
            }
            // Si hay error, volver a llenar ViewBag
            ViewBag.Roles = new SelectList(new[] { "Administrador", "Supervisor", "Biologo", "Recepcionista" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });



            // Si no es válido, retornar la vista con los errores
            return View(empleado);
        }

        // GET: Empleado/Actualizar/5
        public async Task<IActionResult> Actualizar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var empleado = await _context.Empleado.FindAsync(id);
            if (empleado == null)
            {
                return NotFound();
            }
            // Si hay error, volver a llenar ViewBag
            ViewBag.Roles = new SelectList(new[] { "Administrador", "Supervisor", "Biologo", "Recepcionista" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            return View(empleado);
        }

        // POST: Empleado/Actualizar/5

        /// <summary>
        /// Acción que permite actualizar ciertos datos del empleado.
        /// Solo se permiten cambios en: Correo, Celular y Rol. Los demás campos permanecen sin modificación.
        /// </summary>
        /// <param name="id">Identificador único del empleado a actualizar.</param>
        /// <param name="form">Formulario con los datos editados.</param>
        /// <returns>Vista Index si la actualización fue exitosa; de lo contrario, la vista de edición con errores.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("EmpleadoId,Nombre,Apellidos,Dni,FechaNacimiento,Usuario, Contrasena, Celular,Correo,Direccion,Rol,Estado")] Empleado form)
        {
            // Si hay error, volver a llenar ViewBag
            ViewBag.Roles = new SelectList(new[] { "Administrador", "Supervisor", "Biologo", "Recepcionista" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            // Buscar al empleado actual registrado en la base de datos
            var empleadoExistente = await _context.Empleado.FindAsync(id);

            if (empleadoExistente == null)
            {
                // Si no existe, retornar código HTTP 404 (no encontrado)
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Asignar solo los campos permitidos a actualizar
                empleadoExistente.Nombre = form.Nombre;
                empleadoExistente.Apellidos = form.Apellidos;
                empleadoExistente.Dni = form.Dni;
                empleadoExistente.FechaNacimiento = form.FechaNacimiento;
                empleadoExistente.Correo = form.Correo;
                empleadoExistente.Celular = form.Celular;
                empleadoExistente.Rol = form.Rol;
                empleadoExistente.Estado = form.Estado;
                empleadoExistente.Direccion = form.Direccion;
                empleadoExistente.Usuario = form.Usuario;
                empleadoExistente.Contrasena = form.Contrasena;

                try
                {
                    // Guardar los cambios en la base de datos
                    _context.Update(empleadoExistente);
                    await _context.SaveChangesAsync();

                    // 🔑 Registrar auditoría
                    var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                    var auditoria = new HistorialAuditoria
                    {
                        Actividad = "Empleado",
                        Descripcion = "Empleado actualizado",
                        Comentario = $"Nombre: {empleadoExistente.Nombre} {empleadoExistente.Apellidos}, DNI: {empleadoExistente.Dni}, Usuario: {empleadoExistente.Usuario}",
                        EntidadId = empleadoExistente.EmpleadoId,
                        Accion = "Actualizar",
                        Fecha = DateTime.Now,
                        EmpleadoId = empleadoId
                    };
                    _context.HistorialAuditoria.Add(auditoria);
                    await _context.SaveChangesAsync();


                }
                catch (DbUpdateConcurrencyException)
                {
                    // Verificar si el empleado aún existe, de lo contrario retornar 404
                    if (!_context.Empleado.Any(e => e.EmpleadoId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        // Relanzar el error si es otro tipo de conflicto
                        throw;
                    }
                }
                
                // Redirigir al listado tras la edición exitosa
                return RedirectToAction(nameof(Index));
            }

            // Si el modelo no es válido, devolver la vista original con validaciones
            return View(empleadoExistente);
        }

       

        // POST: Empleado/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Empleado/Eliminar/{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var empleado = await _context.Empleado.FindAsync(id);
            if (empleado != null)
            {
                _context.Empleado.Remove(empleado);
                await _context.SaveChangesAsync();
            }

            // 🔑 Registrar auditoría
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            var auditoria = new HistorialAuditoria
            {
                Actividad = "Empleado",
                Descripcion = "Empleado eliminado",
                Comentario = $"Nombre: {empleado.Nombre} {empleado.Apellidos}, DNI: {empleado.Dni}, Usuario: {empleado.Usuario}",
                EntidadId = empleado.EmpleadoId,
                Accion = "Eliminar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _context.HistorialAuditoria.Add(auditoria);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private bool EmpleadoExists(int id)
        {
            return _context.Empleado.Any(e => e.EmpleadoId == id);
        }
    }
}

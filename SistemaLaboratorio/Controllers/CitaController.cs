using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Services;
using Microsoft.AspNetCore.Identity;

namespace SistemaLaboratorio.Controllers
{
    public class CitaController : Controller
    {
        private readonly DblaboratorioContext _context;
        private readonly IEmailService _mail;
        private readonly IWhatsAppService _wa;
        public CitaController(DblaboratorioContext context,
                          IEmailService mail,
                          IWhatsAppService wa)
        {
            _context = context;
            _mail = mail;
            _wa = wa;
        }

        // GET: Cita
        public async Task<IActionResult> Index()
        {
            var dblaboratorioContext = _context.Cita.Include(c => c.Empleado).Include(c => c.Paciente);
            return View(await dblaboratorioContext.ToListAsync());
        }
        // GET: Cita/Detalle/5
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cita = await _context.Cita
                .Include(c => c.Empleado)
                .Include(c => c.Paciente)
                .Include(c => c.CitaAnalisis)
                    .ThenInclude(ca => ca.Analisis) // Esto asume que existe una relación con la entidad Analisis
                .FirstOrDefaultAsync(m => m.CitaId == id);

            if (cita == null)
            {
                return NotFound();
            }

            return View(cita);
        }



        // GET: Cita/Registrar
        public IActionResult Registrar()
        {
            ViewData["EmpleadoId"] = new SelectList(_context.Empleado, "EmpleadoId", "Apellidos");
            ViewData["PacienteId"] = new SelectList(_context.Paciente, "PacienteId", "Apellidos");
            return View();
        }


        // POST: Cita/Registrar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("CitaId,PacienteId,Fecha,Hora,Estado,Sede,Comentario")] Cita cita)
        {
            //Asignar "Pendiente" a Estado de la cita
            cita.Estado = "Pendiente";

            // Obtener el ID del empleado desde la sesión del usuario actual
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            cita.EmpleadoId = empleadoId;

            // Validar que no existan 4 citas en la misma sede, fecha y hora
            var cantidadCitas = await _context.Cita
                .CountAsync(c => c.Fecha == cita.Fecha && c.Hora == cita.Hora && c.Sede == cita.Sede);

            if (cantidadCitas >= 3)
            {
                ModelState.AddModelError("", "Límite de 4 citas alcanzado para esta sede y horario. Seleccione otro horario o sede.");
            }

            ModelState.Remove(nameof(cita.EmpleadoId));
            ModelState.Remove(nameof(cita.PacienteId));
            ModelState.Remove(nameof(cita.Estado));

            if (ModelState.IsValid)
            {
                _context.Add(cita);
                await _context.SaveChangesAsync();

                return RedirectToAction(
                    actionName: "Registrar",
                    controllerName: "CitaAnalisis",
                    routeValues: new { citaId = cita.CitaId });
            }

            // 🔑 Registrar auditoría - Cita registrada
            var auditoriaRegistrar = new HistorialAuditoria
            {
                Actividad = "Cita",
                Descripcion = "Registro de cita",
                Comentario = $"PacienteId: {cita.PacienteId}, Fecha: {cita.Fecha:dd/MM/yyyy}, Hora: {cita.Hora}, Sede: {cita.Sede}",
                EntidadId = cita.CitaId,
                Accion = "Registrar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _context.HistorialAuditoria.Add(auditoriaRegistrar);
            await _context.SaveChangesAsync();

            return View(cita);
        }
        [HttpGet]
        public JsonResult BuscarPorDNIoNombre(string filtro)
        {
            var pacientes = _context.Paciente
                .Where(p => p.Dni.Contains(filtro) || p.Nombre.Contains(filtro) || p.Apellidos.Contains(filtro))
                .Select(p => new {
                    pacienteId = p.PacienteId,
                    dni = p.Dni,
                    nombres = p.Nombre,
                    apellidos = p.Apellidos
                }).ToList();

            return Json(pacientes);
        }

        // GET: Cita/Actualizar/5
        public async Task<IActionResult> Actualizar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cita = await _context.Cita.FindAsync(id);
            if (cita == null)
            {
                return NotFound();
            }
            ViewData["EmpleadoId"] = new SelectList(_context.Empleado, "EmpleadoId", "Apellidos", cita.EmpleadoId);
            ViewData["PacienteId"] = new SelectList(_context.Paciente, "PacienteId", "Apellidos", cita.PacienteId);
            return View(cita);
        }

        // POST: Cita/Actualizar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("CitaId,PacienteId,Fecha,Hora,Estado,Sede,Comentario")] Cita cita)
        {
            if (id != cita.CitaId)
                return NotFound();

            // Obtener cita original para comparar cambios
            var citaOriginal = await _context.Cita.AsNoTracking().FirstOrDefaultAsync(c => c.CitaId == id);
            if (citaOriginal == null)
                return NotFound();

            // Validar máximo de 4 citas en la misma sede, fecha y hora (excluyendo esta misma cita)
            var cantidadCitas = await _context.Cita
                .Where(c =>
                    c.CitaId != cita.CitaId &&
                    c.Fecha == cita.Fecha &&
                    c.Hora == cita.Hora &&
                    c.Sede == cita.Sede)
                .CountAsync();

            if (cantidadCitas >= 3)
            {
                ModelState.AddModelError("", "Límite de 4 citas alcanzado para esta sede y horario. Seleccione otro horario o sede.");
            }

            // Asignar nuevamente el empleado desde el usuario autenticado
            cita.EmpleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);

            ModelState.Remove(nameof(cita.EmpleadoId));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cita);
                    await _context.SaveChangesAsync();

                    // Si deseas enviar notificación en caso de cambios relevantes:
                    if (cita.Fecha != citaOriginal.Fecha || cita.Hora != citaOriginal.Hora || cita.Estado != citaOriginal.Estado)
                    {
                        var citaCompleta = await _context.Cita
                            .Include(c => c.Paciente)
                            .Include(c => c.Empleado)
                            .Include(c => c.CitaAnalisis).ThenInclude(ca => ca.Analisis)
                            .FirstOrDefaultAsync(c => c.CitaId == cita.CitaId);

                        if (citaCompleta != null)
                        {
                            var (plain, html) = ConstruirMensaje(citaCompleta);
                            await _mail.SendAsync(
                                citaCompleta.Paciente.Correo,
                                "Actualización de su cita",
                                plain,
                                html);
                            await _wa.SendAsync(citaCompleta.Paciente.Celular, plain);
                        }
                    }

                    TempData["Ok"] = "Cita actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExisteCita(cita.CitaId))
                        return NotFound();
                    else
                        throw;
                }
            }

            ViewData["EmpleadoId"] = new SelectList(_context.Empleado, "EmpleadoId", "Apellidos", cita.EmpleadoId);
            ViewData["PacienteId"] = new SelectList(_context.Paciente, "PacienteId", "Apellidos", cita.PacienteId);

            // 🔑 Registrar auditoría - Cita actualizada
            var auditoriaActualizar = new HistorialAuditoria
            {
                Actividad = "Cita",
                Descripcion = "Actualización de cita",
                Comentario = $"CitaId: {cita.CitaId}, Fecha: {cita.Fecha:dd/MM/yyyy}, Hora: {cita.Hora}, Estado: {cita.Estado}, Sede: {cita.Sede}",
                EntidadId = cita.CitaId,
                Accion = "Actualizar",
                Fecha = DateTime.Now,
                EmpleadoId = (int)cita.EmpleadoId
            };
            _context.HistorialAuditoria.Add(auditoriaActualizar);
            await _context.SaveChangesAsync();


            return View(cita);
        }

        // GET: Cita/Eliminar/5
        public async Task<IActionResult> Eliminar(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cita = await _context.Cita
                .Include(c => c.Empleado)
                .Include(c => c.Paciente)
                .FirstOrDefaultAsync(m => m.CitaId == id);
            if (cita == null)
            {
                return NotFound();
            }

            return View(cita);
        }

        // POST: Cita/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEliminar(int id)
        {
            var cita = await _context.Cita.FindAsync(id);

            // Primero eliminamos las referencias en la tabla relacionada
            var citaAnalisis = _context.CitaAnalisis.Where(ca => ca.CitaId == id);
            _context.CitaAnalisis.RemoveRange(citaAnalisis);

            _context.Cita.Remove(cita);
            await _context.SaveChangesAsync();
            // 🔑 Registrar auditoría - Cita eliminada
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            var auditoriaEliminar = new HistorialAuditoria
            {
                Actividad = "Cita",
                Descripcion = "Eliminación de cita",
                Comentario = $"CitaId: {cita.CitaId}, PacienteId: {cita.PacienteId}, Fecha: {cita.Fecha:dd/MM/yyyy}, Hora: {cita.Hora}, Sede: {cita.Sede}",
                EntidadId = cita.CitaId,
                Accion = "Eliminar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _context.HistorialAuditoria.Add(auditoriaEliminar);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private bool ExisteCita(int id)
        {
            return _context.Cita.Any(e => e.CitaId == id);
        }

        // ------------- Enviar notificacion ---------------
        [HttpGet]
        public async Task<IActionResult> EnviarNotificacion(int id)
        {
            var cita = await _context.Cita
                .Include(c => c.Paciente)
                .Include(c => c.Empleado)
                .Include(c => c.CitaAnalisis)
                    .ThenInclude(ca => ca.Analisis)
                .FirstOrDefaultAsync(c => c.CitaId == id);

            if (cita is null) return NotFound();

            // 1) Construir mensaje
            var (plain, html) = ConstruirMensaje(cita);

            // 2) Enviar
            await _mail.SendAsync(
                cita.Paciente.Correo,"Detalles de su cita y análisis",
                plain,
                html);

            await _wa.SendAsync(cita.Paciente.Celular, plain);

            TempData["Ok"] = "Notificación enviada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ------------------------------------------

        private static (string plain, string html) ConstruirMensaje(Cita c)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Hola {c.Paciente.Nombre} {c.Paciente.Apellidos}");
            sb.AppendLine();
            sb.AppendLine("Somos Laboratorio OMYLAB, le informamos:");
            sb.AppendLine();
            sb.AppendLine("DETALLES DE SU CITA");
            sb.AppendLine($"Fecha   : {c.Fecha:dd/MM/yyyy}");
            sb.AppendLine($"Hora    : {c.Hora}");
            sb.AppendLine($"Estado  : {c.Estado}");
            sb.AppendLine($"Sede    : {c.Sede}");
            sb.AppendLine($"Personal: {c.Empleado.Nombre} {c.Empleado.Apellidos}");
            sb.AppendLine();
            sb.AppendLine("ANÁLISIS SOLICITADOS:");

            foreach (var ca in c.CitaAnalisis)
            {
                var analisis = ca.Analisis;
                sb.AppendLine($"• {analisis.Nombre}");
                sb.AppendLine($"  - Tipo de Muestra : {analisis.TipoMuestra}");
                sb.AppendLine($"  - Condición       : {analisis.Condicion}");
                sb.AppendLine($"  - Comentario      : {analisis.Comentario}");
                sb.AppendLine($"  - Precio          : S/ {analisis.Precio:F2}");
                sb.AppendLine();
            }

            sb.AppendLine("¡Gracias por confiar en nosotros!");
            sb.AppendLine();
            sb.AppendLine("Encuéntranos en cualquiera de nuestras sedes:");
            sb.AppendLine("Centro: Av. Los Laureles 123, Tacna");
            sb.AppendLine("Blondel: Jr. Libertad 456, Tacna");
            sb.AppendLine("Solidaridad: Calle Principal 789, Tacna");
            sb.AppendLine();
            sb.AppendLine("Contáctanos:");
            sb.AppendLine("Blgo. Oscar Martinez  +51 987654321");
            sb.AppendLine("Blgo. Magaly Flores   +51 912345678");

            string plain = sb.ToString();
            string html = plain.Replace("\n", "<br>");

            return (plain, html);
        }
    }
}

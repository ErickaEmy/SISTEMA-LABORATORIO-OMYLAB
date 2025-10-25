using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iText.Commons.Actions.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    /// <summary>
    /// Controlador para la gestión de pacientes.
    /// Permite registrar, actualizar, eliminar y ver detalles de pacientes.
    /// </summary>
    public class PacienteController : Controller
    {
        /// <summary>
        /// Contexto de base de datos.
        /// </summary>
        private readonly DblaboratorioContext _contexto;

        /// <summary>
        /// Constructor con inyección del contexto.
        /// </summary>
        public PacienteController(DblaboratorioContext contexto)
        {
            _contexto = contexto;
        }

        /// <summary>
        /// Muestra la lista de todos los pacientes.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            return View(await _contexto.Paciente.ToListAsync());
        }

        /// <summary>
        /// Muestra los detalles de un paciente específico.
        /// </summary>
        public async Task<IActionResult> Detalle(int? id)
        {
            if (id == null)
                return NotFound();

            var paciente = await _contexto.Paciente
                .FirstOrDefaultAsync(m => m.PacienteId == id);

            if (paciente == null)
                return NotFound();

            return View(paciente);
        }

        /// <summary>
        /// Muestra el formulario para registrar un nuevo paciente.
        /// </summary>
        public IActionResult Registrar()
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });
            return View();
        }

        /// <summary>
        /// Guarda el nuevo paciente registrado.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("PacienteId,Nombre,Apellidos,Dni,FechaNacimiento,Celular,Sexo,Correo,Direccion,Estado")] Paciente paciente)
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            if (ModelState.IsValid)
            {
                // ✅ Validar si el DNI ya existe
                var dniExistente = await _contexto.Paciente
                    .AnyAsync(p => p.Dni == paciente.Dni);

                if (dniExistente)
                {
                    ModelState.AddModelError("Dni", "El DNI ingresado ya está registrado.");
                    return View(paciente);
                }

                paciente.Celular = "+51" + paciente.Celular; // Asegurar que el celular tenga el prefijo del país
                _contexto.Add(paciente);
                await _contexto.SaveChangesAsync();

                // 🔑 Registrar auditoría para Registrar Paciente
                var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                var auditoriaRegistrar = new HistorialAuditoria
                {
                    Actividad = "Paciente",
                    Descripcion = "Paciente registrado",
                    Comentario = $"Nombre: {paciente.Nombre} {paciente.Apellidos}, DNI: {paciente.Dni}",
                    EntidadId = paciente.PacienteId,
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _contexto.HistorialAuditoria.Add(auditoriaRegistrar);
                await _contexto.SaveChangesAsync();


                return RedirectToAction(nameof(Index));
            }
            return View(paciente);
        }

        /// <summary>
        /// Muestra el formulario para actualizar un paciente.
        /// Solo permite modificar Celular, Correo, Dirección y Estado.
        /// </summary>
        public async Task<IActionResult> Actualizar(int? id)
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            if (id == null)
                return NotFound();

            var paciente = await _contexto.Paciente.FindAsync(id);
            if (paciente == null)
                return NotFound();
           

            // ✅ Preparar fecha formateada para input type="date"
            ViewBag.FechaNacimiento = paciente.FechaNacimiento.ToString("yyyy-MM-dd");
            return View(paciente);
        }

        /// <summary>
        /// Guarda los cambios permitidos del paciente.
        /// Modifica solo Celular, Correo, Dirección y Estado.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("PacienteId,Nombre, Apellidos, FechaNacimiento, Dni, Sexo, Celular,Correo,Direccion,Estado")] Paciente paciente)
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            if (id != paciente.PacienteId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Buscar paciente original
                    var pacienteOriginal = await _contexto.Paciente.FindAsync(id);
                    if (pacienteOriginal == null)
                        return NotFound();

                    paciente.Nombre = pacienteOriginal.Nombre; // Mantener nombre original
                    paciente.Apellidos = pacienteOriginal.Apellidos; // Mantener apellidos originales   
                    paciente.Dni = pacienteOriginal.Dni; // Mantener DNI original   
                    paciente.FechaNacimiento = pacienteOriginal.FechaNacimiento; // Mantener fecha de nacimiento original
                    paciente.Sexo = pacienteOriginal.Sexo; // Mantener sexo original

                    // Actualizar solo campos permitidos
                    pacienteOriginal.Celular = paciente.Celular;
                    pacienteOriginal.Correo = paciente.Correo;
                    pacienteOriginal.Direccion = paciente.Direccion;
                    pacienteOriginal.Estado = paciente.Estado;

                    await _contexto.SaveChangesAsync();

                    // 🔑 Registrar auditoría para Actualizar Paciente
                    var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
                    var auditoriaActualizar = new HistorialAuditoria
                    {
                        Actividad = "Paciente",
                        Descripcion = "Paciente actualizado",
                        Comentario = $"Nombre: {pacienteOriginal.Nombre} {pacienteOriginal.Apellidos}, Nuevo Celular: {pacienteOriginal.Celular}, Nuevo Correo: {pacienteOriginal.Correo}, Nueva Dirección: {pacienteOriginal.Direccion}, Estado: {pacienteOriginal.Estado}",
                        EntidadId = pacienteOriginal.PacienteId,
                        Accion = "Actualizar",
                        Fecha = DateTime.Now,
                        EmpleadoId = empleadoId
                    };
                    _contexto.HistorialAuditoria.Add(auditoriaActualizar);
                    await _contexto.SaveChangesAsync();

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ExistePaciente(paciente.PacienteId))
                        return NotFound();
                    else
                        throw;
                }


                return RedirectToAction(nameof(Index));
            }
            // Si falla, recargar fecha para mostrarla de nuevo
            var pacienteReload = await _contexto.Paciente.FindAsync(id);
            if (pacienteReload != null)
            {
                ViewBag.FechaNacimiento = pacienteReload.FechaNacimiento.ToString("yyyy-MM-dd");
            }
            return View(paciente);
        }

        /// <summary>
        /// Elimina un paciente de forma directa.
        /// No usa vista de confirmación.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var paciente = await _contexto.Paciente.FindAsync(id);
            if (paciente == null)
                return NotFound();

            _contexto.Paciente.Remove(paciente);
            await _contexto.SaveChangesAsync();


            // 🔑 Registrar auditoría para Eliminar Paciente
            var empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);
            var auditoriaEliminar = new HistorialAuditoria
            {
                Actividad = "Paciente",
                Descripcion = "Paciente eliminado",
                Comentario = $"Nombre: {paciente.Nombre} {paciente.Apellidos}, DNI: {paciente.Dni}",
                EntidadId = paciente.PacienteId,
                Accion = "Eliminar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _contexto.HistorialAuditoria.Add(auditoriaEliminar);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Verifica si existe el paciente.
        /// </summary>
        private bool ExistePaciente(int id)
        {
            return _contexto.Paciente.Any(e => e.PacienteId == id);
        }
        // Helper para calcular edad exacta
        private int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.Today;
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }

        public async Task<IActionResult> HistorialDelPaciente(int pacienteId)
        {
            // Buscar paciente
            var paciente = await _contexto.Paciente.FindAsync(pacienteId);
            if (paciente == null)
            {
                return NotFound();
            }

            // Obtener todos los AnalisisPaciente de ese paciente, incluyendo Resultado y Analisis
            var listaAnalisisPaciente = await _contexto.AnalisisPaciente
                .Where(ap => ap.PacienteId == pacienteId)
                .Include(ap => ap.Analisis)
                .OrderByDescending(ap => ap.FechaHoraRegistro)
                .ToListAsync();

            var edad = CalcularEdad(paciente.FechaNacimiento.ToDateTime(TimeOnly.MinValue));
            var sexo = paciente.Sexo;

            var listaAnalisis = new List<object>();

            foreach (var ap in listaAnalisisPaciente)
            {
                // Traer resultado manualmente
                var resultado = await _contexto.Resultados
                    .FirstOrDefaultAsync(r => r.AnalisisPacienteId == ap.AnalisisPacienteId);

                var componentes = new List<object>();
                if (resultado != null)
                {
                    var listaCAP = await _contexto.ComponenteAnalisisPaciente
                        .Include(cap => cap.Componente)
                            .ThenInclude(c => c.DescripcionComponentes)
                        .Where(cap => cap.ResultadoId == resultado.ResultadoId
                                   && cap.AnalisisPacienteId == ap.AnalisisPacienteId)
                        .OrderBy(cap => cap.Componente.Nombre)
                        .ToListAsync();

                    componentes = listaCAP.Select(cap => new
                    {
                        NombreComponente = cap.Componente.Nombre,
                        ValorResultado = cap.ValorResultado,
                        Resultado = cap.Resultado,
                        Referencias = cap.Componente.DescripcionComponentes
                            .Where(d => d.Sexo == "Ambos" || d.Sexo == sexo)
                            .Where(d => (!d.EdadMinima.HasValue || d.EdadMinima <= edad) &&
                                        (!d.EdadMaxima.HasValue || d.EdadMaxima >= edad))
                            .Select(d => new {
                                d.ValorMinimo,
                                d.ValorMaximo,
                                d.Unidad,
                                d.Sexo,
                                d.EdadMinima,
                                d.EdadMaxima
                            }).ToList()
                    }).ToList<object>();
                }

                listaAnalisis.Add(new
                {
                    Analisis = new
                    {
                        Nombre = ap.Analisis.Nombre,
                        TipoMuestra = ap.Analisis.TipoMuestra
                    },
                    AnalisisPaciente = new
                    {
                        FechaHoraRegistro = ap.FechaHoraRegistro,
                        Estado = ap.Estado
                    },
                    Resultado = resultado != null ? new
                    {
                        FechaRegistro = resultado.FechaRegistro
                    } : null,
                    Componentes = componentes
                });
            }

            var viewModel = new
            {
                Paciente = new
                {
                    Dni = paciente.Dni,
                    Nombre = paciente.Nombre,
                    Apellidos = paciente.Apellidos,
                    Sexo = sexo,
                    Edad = edad
                },
                ListaAnalisis = listaAnalisis
            };

            // Si NO tiene análisis, retornar PDF con lista vacía
            if (!listaAnalisisPaciente.Any())
            {
                return new ViewAsPdf("PdfHistorialDelPaciente", viewModel)
                {
                    FileName = $"Historial_{paciente.Dni}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                    CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
                };
            }

            // ✅ Si SÍ tiene análisis, retornar PDF normalmente
            return new ViewAsPdf("PdfHistorialDelPaciente", viewModel)
            {
                FileName = $"Historial_{paciente.Dni}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }


    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    public class PacienteController : Controller
    {
        private readonly DblaboratorioContext _contexto;
        private readonly ILogger<PacienteController> _logger;

        public PacienteController(DblaboratorioContext contexto, ILogger<PacienteController> logger)
        {
            _contexto = contexto;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _contexto.Paciente.ToListAsync());
        }

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

        public IActionResult Registrar()
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Registrar([Bind("PacienteId,Nombre,Apellidos,Dni,FechaNacimiento,Celular,Sexo,Correo,Direccion,Estado")] Paciente paciente)
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            if (ModelState.IsValid)
            {
                var dniExistente = await _contexto.Paciente
                    .AnyAsync(p => p.Dni == paciente.Dni);

                if (dniExistente)
                {
                    ModelState.AddModelError("Dni", "El DNI ingresado ya está registrado.");
                    return View(paciente);
                }

                paciente.Celular = "+51" + paciente.Celular;
                _contexto.Add(paciente);
                await _contexto.SaveChangesAsync();

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

        public async Task<IActionResult> Actualizar(int? id)
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            if (id == null)
                return NotFound();

            var paciente = await _contexto.Paciente.FindAsync(id);
            if (paciente == null)
                return NotFound();

            ViewBag.FechaNacimiento = paciente.FechaNacimiento.ToString("yyyy-MM-dd");
            return View(paciente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(int id, [Bind("PacienteId,Nombre,Apellidos,FechaNacimiento,Dni,Sexo,Celular,Correo,Direccion,Estado")] Paciente paciente)
        {
            ViewBag.Sexos = new SelectList(new[] { "Femenino", "Masculino" });
            ViewBag.Estados = new SelectList(new[] { "Activo", "Inactivo" });

            if (id != paciente.PacienteId)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var pacienteOriginal = await _contexto.Paciente.FindAsync(id);
                    if (pacienteOriginal == null)
                        return NotFound();

                    paciente.Nombre = pacienteOriginal.Nombre;
                    paciente.Apellidos = pacienteOriginal.Apellidos;
                    paciente.Dni = pacienteOriginal.Dni;
                    paciente.FechaNacimiento = pacienteOriginal.FechaNacimiento;
                    paciente.Sexo = pacienteOriginal.Sexo;

                    pacienteOriginal.Celular = paciente.Celular;
                    pacienteOriginal.Correo = paciente.Correo;
                    pacienteOriginal.Direccion = paciente.Direccion;
                    pacienteOriginal.Estado = paciente.Estado;

                    await _contexto.SaveChangesAsync();

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

            var pacienteReload = await _contexto.Paciente.FindAsync(id);
            if (pacienteReload != null)
            {
                ViewBag.FechaNacimiento = pacienteReload.FechaNacimiento.ToString("yyyy-MM-dd");
            }
            return View(paciente);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var paciente = await _contexto.Paciente.FindAsync(id);
            if (paciente == null)
                return NotFound();

            _contexto.Paciente.Remove(paciente);
            await _contexto.SaveChangesAsync();

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

        private bool ExistePaciente(int id)
        {
            return _contexto.Paciente.Any(e => e.PacienteId == id);
        }

        private int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.Today;
            int edad = hoy.Year - fechaNacimiento.Year;
            if (fechaNacimiento.Date > hoy.AddYears(-edad)) edad--;
            return edad;
        }

        public async Task<IActionResult> HistorialDelPaciente(int pacienteId)
        {
            try
            {
                _logger.LogInformation($"Iniciando generación de historial PDF para paciente {pacienteId}");

                var paciente = await _contexto.Paciente.FindAsync(pacienteId);
                if (paciente == null)
                {
                    _logger.LogWarning($"Paciente {pacienteId} no encontrado");
                    return NotFound();
                }

                _logger.LogInformation($"Paciente encontrado: {paciente.Nombre} {paciente.Apellidos}");

                var listaAnalisisPaciente = await _contexto.AnalisisPaciente
                    .Where(ap => ap.PacienteId == pacienteId)
                    .Include(ap => ap.Analisis)
                    .OrderByDescending(ap => ap.FechaHoraRegistro)
                    .ToListAsync();

                _logger.LogInformation($"Se encontraron {listaAnalisisPaciente.Count} análisis");

                var edad = CalcularEdad(paciente.FechaNacimiento.ToDateTime(TimeOnly.MinValue));
                var sexo = paciente.Sexo;

                var listaAnalisis = new List<object>();

                foreach (var ap in listaAnalisisPaciente)
                {
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

                _logger.LogInformation("Generando PDF con Rotativa...");

                return new ViewAsPdf("PdfHistorialDelPaciente", viewModel)
                {
                    FileName = $"Historial_{paciente.Dni}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                    CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR generando PDF para paciente {pacienteId}");
                _logger.LogError($"{ex.GetType().Name}: {ex.Message}");
                _logger.LogError($"StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"InnerException: {ex.InnerException.Message}");
                }

                throw; // Re-lanzar para que ASP.NET maneje el error
            }
        }
    }
}
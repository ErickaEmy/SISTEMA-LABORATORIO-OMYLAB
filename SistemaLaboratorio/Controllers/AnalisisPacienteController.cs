using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    /// <summary>
    /// Controlador para gestionar los análisis realizados a pacientes.
    /// Permite registrar, listar, ver detalle y cancelar análisis, 
    /// además de registrar automáticamente el consumo de reactivos asociado.
    /// Implementa la funcionalidad del requerimiento RF-07.
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 2025-08-09
    /// </summary>
    public class AnalisisPacienteController : Controller
    {
        /// <summary>
        /// Contexto para acceder a la base de datos del laboratorio.
        /// Se utiliza para realizar operaciones CRUD sobre entidades relacionadas a análisis y pacientes.
        /// </summary>
        private readonly DblaboratorioContext _context;

        /// <summary>
        /// Constructor que inyecta el contexto de datos para ser utilizado en las operaciones del controlador.
        /// </summary>
        /// <param name="context">Instancia del contexto de base de datos</param>
        public AnalisisPacienteController(DblaboratorioContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene y muestra la lista de todos los análisis de pacientes, ordenando por estado y fecha.
        /// Se priorizan los análisis con estado "Pendiente" y luego los más recientes.
        /// </summary>
        /// <returns>Vista con listado de análisis pacientes</returns>
        public async Task<IActionResult> Index()
        {
            // Carga de análisis paciente junto con datos relacionados para facilitar la presentación
            // Ordena para mostrar primero los análisis pendientes y luego ordena por fecha descendente
            var analisisPaciente = await _context.AnalisisPaciente
                .Include(a => a.Paciente)
                .Include(a => a.Analisis)
                .Include(a => a.Empleado)
                .OrderBy(a => a.Estado != "Pendiente") // false (pendiente) primero
                .ThenByDescending(a => a.FechaHoraRegistro)
                .ToListAsync();

            return View(analisisPaciente);
        }

        /// <summary>
        /// Obtiene el detalle completo de un análisis específico dado su ID.
        /// Incluye información del paciente, análisis y empleado responsable.
        /// </summary>
        /// <param name="analisisPacienteId">Identificador único del análisis paciente</param>
        /// <returns>Vista con detalle del análisis o NotFound si no existe</returns>
        public async Task<IActionResult> Detalle(int? id)
        {
            // Validación temprana: verifica que el parámetro ID no sea nulo
            if (id == null)
            {
                return NotFound();
            }

            // Obtiene el registro del análisis paciente incluyendo sus relaciones para mostrar detalles completos
            var analisisPaciente = await _context.AnalisisPaciente
                .Include(a => a.Analisis)
                .Include(a => a.Empleado)
                .Include(a => a.Paciente)
                .FirstOrDefaultAsync(m => m.AnalisisPacienteId == id);

            // Si no se encuentra el análisis, responde con NotFound para evitar errores posteriores
            if (analisisPaciente == null)
            {
                return NotFound();
            }

            // Preparación de datos extra para la vista usando ViewBag
            ViewBag.NombrePaciente = $"{analisisPaciente.Paciente?.Nombre} {analisisPaciente.Paciente?.Apellidos}";
            ViewBag.DniPaciente = analisisPaciente.Paciente?.Dni;
            ViewBag.NombreAnalisis = analisisPaciente.Analisis?.Nombre;

            return View(analisisPaciente);
        }

        /// <summary>
        /// Presenta el formulario para registrar un nuevo análisis a un paciente.
        /// Se cargan listas desplegables con pacientes y análisis activos para selección.
        /// </summary>
        /// <returns>Vista con formulario para registro</returns>
        public IActionResult Registrar()
        {
            // Carga SelectList con pacientes activos para desplegar en el formulario
            ViewData["PacienteId"] = new SelectList(_context.Paciente.Where(p => p.Estado == "Activo"), "PacienteId", "Nombre");
            // Carga SelectList con análisis activos para selección
            ViewData["AnalisisId"] = new SelectList(_context.Analisis.Where(a => a.Estado), "AnalisisId", "Nombre");
            return View();
        }

        /// <summary>
        /// Procesa el registro de un nuevo análisis para un paciente específico.
        /// Valida datos de entrada, crea registros de análisis paciente, resultado, componentes y consumos.
        /// También registra auditoría de la acción.
        /// </summary>
        /// <param name="pacienteId">ID del paciente al que se le realiza el análisis</param>
        /// <param name="analisisId">ID del análisis a registrar</param>
        /// <returns>Redirige a la lista de análisis o muestra error en caso de fallo</returns>
        [HttpPost]
        public async Task<IActionResult> Registrar(int pacienteId, int analisisId)
        {
            // Recarga las listas desplegables para mantener la selección en caso de error y volver a mostrar el formulario
            ViewData["PacienteId"] = new SelectList(_context.Paciente.Where(p => p.Estado == "Activo"), "PacienteId", "Nombre");
            ViewData["AnalisisId"] = new SelectList(_context.Analisis.Where(a => a.Estado), "AnalisisId", "Nombre");

            try
            {
                // Validación Fail Fast para asegurarse que el paciente existe y está activo
                var paciente = await _context.Paciente.FindAsync(pacienteId);
                if (paciente == null || paciente.Estado != "Activo")
                    return NotFound("Paciente no encontrado o inactivo.");

                // Validación Fail Fast para asegurarse que el análisis existe y está activo
                var analisis = await _context.Analisis.FindAsync(analisisId);
                if (analisis == null || !analisis.Estado)
                    return NotFound("Análisis no encontrado o inactivo.");

                // Validación explícita para obtener el EmpleadoId desde el token de usuario
                var claimEmpleadoId = User.FindFirst("EmpleadoId");
                if (claimEmpleadoId == null)
                {
                    // Retorna error si no existe el claim en el token
                    ModelState.AddModelError("", "No se encontró el identificador del empleado en la sesión actual.");
                    return View();
                }

                if (!int.TryParse(claimEmpleadoId.Value, out int empleadoId))
                {
                    // Retorna error si el claim no es un entero válido
                    ModelState.AddModelError("", "El identificador del empleado no es válido.");
                    return View();
                }

                // Validación adicional: prevenir duplicados si ya existe un análisis idéntico para este paciente
                bool existeAnalisisDuplicado = await _context.AnalisisPaciente
                    .AnyAsync(ap => ap.PacienteId == pacienteId && ap.AnalisisId == analisisId && ap.Estado != "Cancelado");
                if (existeAnalisisDuplicado)
                {
                    ModelState.AddModelError("", "El paciente ya tiene registrado este análisis activo.");
                    return View();
                }

                // 1. Crear nuevo registro de AnalisisPaciente con estado inicial "Pendiente" y fecha actual
                var analisisPaciente = new AnalisisPaciente
                {
                    PacienteId = pacienteId,
                    AnalisisId = analisisId,
                    EmpleadoId = empleadoId,
                    FechaHoraRegistro = DateTime.Now,
                    Estado = "Pendiente"
                };
                _context.AnalisisPaciente.Add(analisisPaciente);
                await _context.SaveChangesAsync(); // Guarda para obtener el ID generado en base de datos

                // 2. Crear registro inicial de Resultado asociado al análisis paciente
                var resultado = new Resultado
                {
                    AnalisisId = analisisId,
                    PacienteId = pacienteId,
                    Estado = "Pendiente",
                    FechaRegistro = DateOnly.FromDateTime(DateTime.Today),
                    AnalisisPacienteId = analisisPaciente.AnalisisPacienteId
                };
                _context.Resultados.Add(resultado);
                await _context.SaveChangesAsync(); // Guarda para obtener ResultadoId

                // 3. Obtener los componentes que conforman el análisis seleccionado, incluyendo reactivos vinculados
                var componentes = await _context.AnalisisComponente
                    .Where(ac => ac.AnalisisId == analisisId)
                    .Include(ac => ac.Componente)
                        .ThenInclude(c => c.ReactivoComponentes)
                            .ThenInclude(rc => rc.Reactivo)
                    .ToListAsync();

                // 4. Registrar cada componente para el análisis paciente con estado y valor iniciales
                foreach (var componentePaciente in componentes)
                {
                    var componenteAnalisisPaciente = new ComponenteAnalisisPaciente
                    {
                        AnalisisPacienteId = analisisPaciente.AnalisisPacienteId,
                        ComponenteId = componentePaciente.ComponenteId,
                        ResultadoId = resultado.ResultadoId,
                        ValorResultado = 0.0, // Valor por defecto para posteriores actualizaciones
                        Resultado = "Pendiente"
                    };
                    _context.ComponenteAnalisisPaciente.Add(componenteAnalisisPaciente);
                }

                // 5. Registrar el consumo de reactivos derivados de los componentes del análisis
                foreach (var componentePaciente in componentes)
                {
                    foreach (var reactivoComponente in componentePaciente.Componente.ReactivoComponentes)
                    {
                        var reactivo = reactivoComponente.Reactivo;
                        if (reactivo != null)
                        {
                            // Registro detallado del consumo de reactivos para control de inventario y trazabilidad
                            var consumo = new Consumo
                            {
                                Fecha = DateOnly.FromDateTime(DateTime.Now),
                                // Nota: DiaSemana guarda el número del día del mes, se podría cambiar para guardar nombre día si es requerido
                                DiaSemana = DateTime.Today.Day.ToString(),
                                Mes = DateTime.Today.Month,
                                Año = DateTime.Today.Year,
                                ReactivoId = reactivo.ReactivoId,
                                NombreReactivo = reactivo.Nombre,
                                CantidadConsumida = reactivoComponente.Cantidad, // Cantidad requerida por el componente
                                Comentario = "Ninguno",
                                AnalisisId = analisisId
                            };
                            _context.Consumo.Add(consumo);
                        }
                    }
                }

                // 6. Guardar todos los cambios pendientes (componentes y consumos)
                await _context.SaveChangesAsync();

                // 7. Registrar la auditoría de la operación para control y trazabilidad
                var auditoria = new HistorialAuditoria
                {
                    Actividad = "AnalisisPaciente",
                    Descripcion = "Registro de análisis para paciente",
                    Comentario = $"Paciente: {paciente.Nombre}, Análisis: {analisis.Nombre}, ID: {analisisPaciente.AnalisisPacienteId}",
                    EntidadId = analisisPaciente.AnalisisPacienteId,
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = empleadoId
                };
                _context.HistorialAuditoria.Add(auditoria);
                await _context.SaveChangesAsync();

                // 8. Redirige a la acción Index para mostrar la lista actualizada de análisis pacientes
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Captura cualquier excepción inesperada durante el proceso
                // Se puede registrar la excepción en logs o auditoría para seguimiento

                // Registro de auditoría de error
                var auditoriaError = new HistorialAuditoria
                {
                    Actividad = "AnalisisPaciente",
                    Descripcion = "Error al registrar análisis para paciente",
                    Comentario = $"Error: {ex.Message}",
                    EntidadId = 0,
                    Accion = "Registrar",
                    Fecha = DateTime.Now,
                    EmpleadoId = 0
                };
                _context.HistorialAuditoria.Add(auditoriaError);
                await _context.SaveChangesAsync();

                // Añade error general al modelo para mostrar en la vista
                ModelState.AddModelError("", "Ocurrió un error inesperado al registrar el análisis. Por favor, intente nuevamente.");

                return View();
            }
        }

        /// <summary>
        /// Proporciona un método para buscar pacientes activos por nombre, apellido o DNI, 
        /// utilizado para autocompletar formularios.
        /// </summary>
        /// <param name="term">Cadena de búsqueda ingresada por el usuario</param>
        /// <returns>JSON con pacientes coincidentes para autocompletado</returns>
        [HttpGet]
        public JsonResult BuscarPorNombre(string term)
        {
            // Realiza búsqueda insensible a mayúsculas/minúsculas en nombre, apellido o DNI
            var pacientes = _context.Paciente
                .Where(p => p.Estado == "Activo" &&
                    (p.Nombre.ToLower().Contains(term.ToLower()) ||
                     p.Apellidos.ToLower().Contains(term.ToLower()) ||
                     p.Dni.Contains(term)))
                .Select(p => new
                {
                    label = p.Nombre + " " + p.Apellidos + " - DNI: " + p.Dni,
                    value = p.PacienteId
                })
                .ToList();

            return Json(pacientes);
        }

        /// <summary>
        /// Cancela un análisis paciente que esté en estado "Pendiente", actualizando también el resultado asociado.
        /// Incluye manejo y mensajes explícitos si el análisis no está en estado pendiente o ya está cancelado.
        /// </summary>
        /// <param name="analisisPacienteId">Identificador del análisis paciente a cancelar</param>
        /// <returns>JSON indicando el éxito o fallo de la operación con mensaje detallado</returns>
        [HttpPost]
        public async Task<IActionResult> Cancelar(int analisisPacienteId)
        {
            try
            {
                // Busca el análisis paciente por ID para validar existencia y estado
                var analisisPaciente = await _context.AnalisisPaciente
                    .FirstOrDefaultAsync(ap => ap.AnalisisPacienteId == analisisPacienteId);

                if (analisisPaciente == null)
                    return NotFound();

                // Verificar estado actual del análisis para decidir acción
                if (analisisPaciente.Estado == "Pendiente")
                {
                    // Actualiza estado a Cancelado
                    analisisPaciente.Estado = "Cancelado";

                    // Actualiza el estado del resultado relacionado para mantener consistencia
                    var resultado = await _context.Resultados
                        .FirstOrDefaultAsync(r => r.AnalisisPacienteId == analisisPacienteId);

                    if (resultado != null)
                    {
                        resultado.Estado = "Cancelado";
                    }

                    // Guarda los cambios realizados en el contexto
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Análisis cancelado correctamente." });
                }
                else if (analisisPaciente.Estado == "Cancelado")
                {
                    // Informe que ya estaba cancelado
                    return Json(new { success = false, message = "El análisis ya se encontraba cancelado." });
                }
                else
                {
                    // Informe para otros estados no pendientes ni cancelados
                    return Json(new { success = false, message = $"No se puede cancelar un análisis en estado '{analisisPaciente.Estado}'." });
                }
            }
            catch (Exception ex)
            {
                // Captura excepciones inesperadas y registra auditoría de error
                var auditoriaError = new HistorialAuditoria
                {
                    Actividad = "AnalisisPaciente",
                    Descripcion = "Error al cancelar análisis paciente",
                    Comentario = $"Error: {ex.Message}",
                    EntidadId = analisisPacienteId,
                    Accion = "Cancelar",
                    Fecha = DateTime.Now,
                    EmpleadoId = 0
                };
                _context.HistorialAuditoria.Add(auditoriaError);
                await _context.SaveChangesAsync();

                return Json(new { success = false, message = "Error inesperado al cancelar el análisis. Intente nuevamente." });
            }
        }
    }
}
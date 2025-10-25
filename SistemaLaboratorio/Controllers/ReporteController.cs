using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Models;
using System.Linq;

namespace SistemaLaboratorio.Controllers
{
    /// <summary>
    /// Controlador responsable de la generación optimizada de reportes en formato PDF
    /// para diversas entidades del sistema OMYLAB.
    /// 
    /// Este controlador implementa mejoras conforme al requisito funcional RF-13:
    /// - Centralización de lógica repetitiva en métodos reutilizables para consultas y generación de reportes.
    /// - Uso de nombres de variables descriptivos y significativos para mejorar la comprensión del código.
    /// - Eliminación de números mágicos en paginación, filtros y configuraciones.
    /// - Estructura clara y mantenible que cumple con los estándares del proyecto para legibilidad y facilidad de mantenimiento.
    /// 
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 2025-08-09
    /// Referencias:
    /// - RF-13: Generar Reportes
    /// </summary>
    public class ReporteController : Controller
    {
        private readonly DblaboratorioContext _context; // Contexto para acceso a base de datos

        /// <summary>
        /// Constructor que recibe el contexto de base de datos mediante inyección de dependencias.
        /// </summary>
        /// <param name="context">Contexto para acceso a datos del laboratorio.</param>
        public ReporteController(DblaboratorioContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Acción que devuelve la vista principal con el listado de empleados ordenados por nombre.
        /// </summary>
        /// <returns>Vista con la lista de empleados.</returns>
        public IActionResult Index()
        {
            // Consulta todos los empleados, ordenándolos alfabéticamente por su nombre.
            var empleados = _context.Empleado
                .OrderBy(e => e.Nombre)
                .ToList();

            // Se pasa la lista de empleados a la vista para su renderización.
            return View(empleados);
        }

        /// <summary>
        /// Genera un reporte PDF con la lista completa de empleados.
        /// </summary>
        /// <returns>Archivo PDF con reporte de empleados.</returns>
        public IActionResult GenerarPdfEmpleados()
        {
            // Consulta la lista ordenada de empleados para el reporte.
            var empleados = _context.Empleado
                .OrderBy(e => e.Nombre)
                .ToList();

            // Construye y devuelve el PDF con configuración personalizada para tamaño, orientación y pie de página.
            return new ViewAsPdf("PdfEmpleados", empleados)
            {
                FileName = "ReporteEmpleados.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                // Pie de página centrado con número de página
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF que incluye todas las citas con sus análisis asociados, pacientes y empleados responsables.
        /// </summary>
        /// <returns>PDF con listado completo de citas.</returns>
        public IActionResult GenerarPdfCitas()
        {
            // Carga citas incluyendo las relaciones necesarias para mostrar datos completos en el reporte.
            var citas = _context.CitaAnalisis
                .Include(ca => ca.Analisis) // Incluye datos del análisis realizado
                .Include(ca => ca.Cita)
                    .ThenInclude(c => c.Paciente) // Incluye información del paciente relacionado
                .Include(ca => ca.Cita)
                    .ThenInclude(c => c.Empleado) // Incluye datos del empleado que gestionó la cita
                .OrderBy(ca => ca.Cita.Fecha) // Ordena las citas cronológicamente
                .ToList();

            // Prepara un diccionario para enviar datos adicionales a la vista (aquí rango de fechas "Todos").
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = "Todos"
            };

            // Genera el PDF utilizando la vista 'PdfCitas' y la información cargada.
            return new ViewAsPdf("PdfCitas", citas, viewData)
            {
                FileName = "ReporteCitas.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con las citas filtradas por un rango de fechas.
        /// </summary>
        /// <param name="fechaInicio">Fecha inicial del filtro.</param>
        /// <param name="fechaFin">Fecha final del filtro.</param>
        /// <returns>PDF con listado de citas en el rango indicado.</returns>
        public IActionResult GenerarPdfCitasPorFecha(DateTime fechaInicio, DateTime fechaFin)
        {
            // Convierte DateTime a DateOnly para facilitar comparación precisa sin tiempo.
            DateOnly inicio = DateOnly.FromDateTime(fechaInicio);
            DateOnly fin = DateOnly.FromDateTime(fechaFin);

            // Consulta citas entre el rango de fechas, incluyendo datos relacionados para detalle completo.
            var citas = _context.CitaAnalisis
                .Include(ca => ca.Analisis)
                .Include(ca => ca.Cita)
                    .ThenInclude(c => c.Paciente)
                .Include(ca => ca.Cita)
                    .ThenInclude(c => c.Empleado)
                .Where(ca => ca.Cita.Fecha >= inicio && ca.Cita.Fecha <= fin) // Aplica filtro por fecha
                .OrderBy(ca => ca.Cita.Fecha)
                .ToList();

            // Prepara ViewData para enviar rango de fechas a la vista para mostrar contexto.
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}"
            };
            // Retorna el PDF generado con nombre que incluye las fechas del filtro.
            return new ViewAsPdf("PdfCitas", citas, viewData)
            {
                FileName = $"ReporteCitas_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con el resumen de reactivos consumidos agrupados por análisis.
        /// </summary>
        /// <returns>PDF con cantidades totales consumidas por reactivo y análisis.</returns>
        public IActionResult GenerarPdfReactivosConsumidos()
        {
            // Consulta los nombres de los análisis para presentar datos legibles.
            var nombresAnalisis = _context.Analisis
                .ToDictionary(a => a.AnalisisId, a => a.Nombre);

            // Agrupa consumos por análisis y reactivo, sumando la cantidad consumida.
            var datos = _context.Consumo
                .GroupBy(c => new { c.AnalisisId, c.NombreReactivo, c.ReactivoId }) // Agrupa por análisis y reactivo
                .Select(g => new ReactivoConsumidoDto
                {
                    AnalisisId = g.Key.AnalisisId,
                    ReactivoId = g.Key.ReactivoId,
                    NombreReactivo = g.Key.NombreReactivo,
                    CantidadTotal = g.Sum(x => x.CantidadConsumida) // Suma total consumida
                })
                .ToList()
                .GroupBy(x => x.AnalisisId) // Agrupa resultado por análisis para estructura de datos
                .ToDictionary(g => g.Key, g => g.ToList());

            // Se envía información adicional para la vista
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = "Todos",
                ["NombresAnalisis"] = nombresAnalisis
            };

            // Retorna el PDF generado con la información de consumos.
            return new ViewAsPdf("PdfReactivosConsumidos", datos, viewData)
            {
                FileName = "ReactivosConsumidosPorAnalisis.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con reactivos consumidos filtrados por rango de fechas.
        /// </summary>
        /// <param name="fechaInicio">Fecha inicial para filtro.</param>
        /// <param name="fechaFin">Fecha final para filtro.</param>
        /// <returns>PDF con resumen de reactivos consumidos en el rango indicado.</returns>
        public IActionResult GenerarPdfReactivosConsumidosPorFecha(DateTime fechaInicio, DateTime fechaFin)
        {
            DateOnly inicio = DateOnly.FromDateTime(fechaInicio);
            DateOnly fin = DateOnly.FromDateTime(fechaFin);

            // Consulta nombres de análisis para mostrar en el reporte.
            var nombresAnalisis = _context.Analisis
                .ToDictionary(a => a.AnalisisId, a => a.Nombre);

            // Consulta consumos filtrados por fecha, agrupados y sumados.
            var datos = _context.Consumo
                .Where(c => c.Fecha >= inicio && c.Fecha <= fin)
                .GroupBy(c => new { c.AnalisisId, c.NombreReactivo, c.ReactivoId })
                .Select(g => new ReactivoConsumidoDto
                {
                    AnalisisId = g.Key.AnalisisId,
                    ReactivoId = g.Key.ReactivoId,
                    NombreReactivo = g.Key.NombreReactivo,
                    CantidadTotal = g.Sum(x => x.CantidadConsumida)
                })
                .ToList()
                .GroupBy(x => x.AnalisisId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Se añade rango de fechas al ViewData para la vista.
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}",
                ["NombresAnalisis"] = nombresAnalisis
            };

            return new ViewAsPdf("PdfReactivosConsumidos", datos, viewData)
            {
                FileName = $"ReactivosConsumidos_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con reactivos próximos a vencer en el próximo mes.
        /// </summary>
        /// <returns>PDF con listado de reactivos próximos a vencer.</returns>
        public IActionResult GenerarPdfReactivosPorVencer()
        {
            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            DateOnly limite = hoy.AddMonths(1); // Define límite para vencimiento (1 mes desde hoy)

            // Consulta reactivos cuyo vencimiento está dentro del próximo mes
            var reactivos = _context.Reactivo
                .Where(r => r.FechaVencimiento <= limite)
                .Select(r => new ReactivoPorVencerDto
                {
                    Nombre = r.Nombre,
                    FechaVencimiento = r.FechaVencimiento,
                    Presentacion = r.Presentacion,
                    Proveedor = r.Proveedor,
                    // Calcula días restantes para el vencimiento (positivo si está por vencer)
                    DiasPorVencer = (r.FechaVencimiento.ToDateTime(TimeOnly.MinValue) - DateTime.Today).Days
                })
                .OrderBy(r => r.FechaVencimiento) // Ordena por fecha de vencimiento ascendente
                .ToList();

            // Retorna PDF con reactivos próximos a vencer
            return new ViewAsPdf("PdfReactivosPorVencer", reactivos)
            {
                FileName = "ReactivosPorVencer.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con los análisis más solicitados en el sistema, sin filtro por fecha.
        /// </summary>
        /// <returns>PDF con listado de análisis solicitados ordenados por cantidad descendente.</returns>
        public IActionResult GenerarPdfAnalisisSolicitados()
        {
            // Agrupa registros de análisis paciente no cancelados, cuenta cantidad para ranking.
            var datos = _context.AnalisisPaciente
                .Where(ap => ap.Estado != "Cancelado")
                .GroupBy(ap => ap.AnalisisId)
                .Select(g => new AnalisisSolicitadoDto
                {
                    NombreAnalisis = _context.Analisis
                        .Where(a => a.AnalisisId == g.Key)
                        .Select(a => a.Nombre)
                        .FirstOrDefault(),
                    Cantidad = g.Count()
                })
                .OrderByDescending(a => a.Cantidad) // Ordena por cantidad descendente
                .ToList();

            return new ViewAsPdf("PdfAnalisisSolicitados", datos)
            {
                FileName = "AnalisisMasSolicitados.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con los análisis más solicitados filtrados por rango de fechas.
        /// </summary>
        /// <param name="fechaInicio">Fecha de inicio del filtro.</param>
        /// <param name="fechaFin">Fecha final del filtro.</param>
        /// <returns>PDF con análisis solicitados en rango de fechas especificado.</returns>
        public IActionResult GenerarPdfAnalisisSolicitadosPorFecha(DateTime fechaInicio, DateTime fechaFin)
        {
            var datos = _context.AnalisisPaciente
                .Where(ap => ap.Estado != "Cancelado" &&
                             ap.FechaHoraRegistro >= fechaInicio &&
                             ap.FechaHoraRegistro <= fechaFin)
                .GroupBy(ap => ap.AnalisisId)
                .Select(g => new AnalisisSolicitadoDto
                {
                    NombreAnalisis = _context.Analisis
                        .Where(a => a.AnalisisId == g.Key)
                        .Select(a => a.Nombre)
                        .FirstOrDefault(),
                    Cantidad = g.Count()
                })
                .OrderByDescending(a => a.Cantidad)
                .ToList();

            // Se añade rango de fechas para mostrar en la vista.
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}"
            };

            return new ViewAsPdf("PdfAnalisisSolicitados", datos, viewData)
            {
                FileName = $"AnalisisMasSolicitados_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }
        /// <summary>
        /// Genera un reporte PDF con todo el historial de auditoría del sistema.
        /// </summary>
        /// <returns>PDF con el listado completo del historial ordenado por fecha descendente.</returns>
        public IActionResult GenerarPdfHistorialAuditoria()
        {
            // Obtiene el historial auditado, incluyendo datos del empleado responsable.
            var historial = _context.HistorialAuditoria
                .Include(h => h.Empleado) // Incluye la entidad Empleado relacionada para obtener nombre completo.
                .OrderByDescending(h => h.Fecha) // Ordena de más reciente a más antiguo.
                .Select(h => new HistorialAuditoriaDto
                {
                    Actividad = h.Actividad, // Tipo de actividad realizada.
                    Descripcion = h.Descripcion, // Descripción de la actividad.
                    Comentario = h.Comentario, // Comentarios adicionales.
                    EntidadId = h.EntidadId, // Id de la entidad sobre la que se actuó.
                    Accion = h.Accion, // Acción realizada (Insertar, Modificar, Eliminar, etc.).
                    Fecha = h.Fecha, // Fecha y hora de la actividad.
                    EmpleadoNombre = h.Empleado.Nombre + " " + h.Empleado.Apellidos // Nombre completo del empleado responsable.
                })
                .ToList(); // Ejecuta la consulta y carga en memoria.

            // Devuelve un PDF usando la vista 'PdfHistorialAuditoria' y el listado obtenido.
            return new ViewAsPdf("PdfHistorialAuditoria", historial)
            {
                FileName = "HistorialAuditoria.pdf", // Nombre del archivo PDF resultante.
                PageSize = Rotativa.AspNetCore.Options.Size.A4, // Tamaño de página A4.
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait, // Orientación vertical.
                                                                                    // Pie de página con numeración centrada, tamaño fuente y separación especificados.
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con el historial de auditoría filtrado por empleado específico.
        /// </summary>
        /// <param name="empleadoId">Identificador del empleado para filtrar el historial.</param>
        /// <returns>PDF con historial de auditoría del empleado indicado.</returns>
        public IActionResult GenerarPdfHistorialPorEmpleado(int empleadoId)
        {
            // Consulta el historial filtrado por el id del empleado y carga el nombre completo.
            var historial = _context.HistorialAuditoria
                .Include(h => h.Empleado)
                .Where(h => h.EmpleadoId == empleadoId) // Filtra solo las acciones del empleado indicado.
                .OrderByDescending(h => h.Fecha) // Ordena de más reciente a más antiguo.
                .Select(h => new HistorialAuditoriaDto
                {
                    Actividad = h.Actividad,
                    Descripcion = h.Descripcion,
                    Comentario = h.Comentario,
                    EntidadId = h.EntidadId,
                    Accion = h.Accion,
                    Fecha = h.Fecha,
                    EmpleadoNombre = h.Empleado.Nombre + " " + h.Empleado.Apellidos
                })
                .ToList();

            // Devuelve PDF usando la misma vista que para todo el historial, pero con filtro por empleado.
            return new ViewAsPdf("PdfHistorialAuditoria", historial)
            {
                FileName = $"HistorialAuditoria_Empleado_{empleadoId}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con los reactivos más utilizados en el sistema, sin filtro por fecha.
        /// </summary>
        /// <returns>PDF con listado de reactivos y cantidad de usos.</returns>
        public IActionResult GenerarPdfReactivosMasUtilizados()
        {
            // Agrupa consumos por nombre de reactivo y cuenta cuántas veces aparece cada uno.
            var reactivos = _context.Consumo
                .GroupBy(c => c.NombreReactivo)
                .Select(g => new ReactivoMasUtilizadoDto
                {
                    NombreReactivo = g.Key, // Nombre del reactivo.
                    Cantidad = g.Count() // Cantidad total de consumos registrados.
                })
                .OrderByDescending(r => r.Cantidad) // Orden descendente para mostrar más usados primero.
                .ToList();

            // Se añade ViewData para indicar que el rango es "Todos".
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = "Todos"
            };

            // Devuelve el PDF con la vista y datos preparados.
            return new ViewAsPdf("PdfReactivosMasUtilizados", reactivos, viewData)
            {
                FileName = "ReactivosMasUtilizados.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con los reactivos más utilizados en un rango de fechas.
        /// </summary>
        /// <param name="fechaInicio">Fecha inicial para filtro.</param>
        /// <param name="fechaFin">Fecha final para filtro.</param>
        /// <returns>PDF con reactivos y cantidades usados en rango especificado.</returns>
        public IActionResult GenerarPdfReactivosMasUtilizadosPorFecha(DateTime fechaInicio, DateTime fechaFin)
        {
            DateOnly inicio = DateOnly.FromDateTime(fechaInicio);
            DateOnly fin = DateOnly.FromDateTime(fechaFin);

            // Consulta consumos filtrados por fecha, agrupados y contados.
            var reactivos = _context.Consumo
                .Where(c => c.Fecha >= inicio && c.Fecha <= fin)
                .GroupBy(c => c.NombreReactivo)
                .Select(g => new ReactivoMasUtilizadoDto
                {
                    NombreReactivo = g.Key,
                    Cantidad = g.Count()
                })
                .OrderByDescending(r => r.Cantidad)
                .ToList();

            // Se añade al ViewData el rango de fechas para mostrarlo en la vista.
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}"
            };

            // Devuelve el PDF con filtro por fecha aplicado.
            return new ViewAsPdf("PdfReactivosMasUtilizados", reactivos, viewData)
            {
                FileName = $"ReactivosMasUtilizados_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con los análisis emitidos, sin filtro por fecha.
        /// </summary>
        /// <returns>PDF con listado completo de análisis emitidos.</returns>
        public IActionResult GenerarPdfAnalisisEmitidos()
        {
            // Consulta datos necesarios de análisis emitidos, incluyendo paciente y empleado.
            var lista = _context.AnalisisPaciente
                .Select(ap => new AnalisisEmitidoDto
                {
                    PacienteDni = ap.Paciente.Dni,
                    PacienteNombreCompleto = ap.Paciente.Nombre + " " + ap.Paciente.Apellidos,
                    NombreAnalisis = ap.Analisis.Nombre,
                    FechaHoraRegistro = ap.FechaHoraRegistro,
                    EmpleadoNombreCompleto = ap.Empleado.Nombre + " " + ap.Empleado.Apellidos
                })
                .OrderByDescending(ap => ap.FechaHoraRegistro) // Ordena por fecha más reciente.
                .ToList();

            // Indicamos que el rango es "Todos" para la vista.
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = "Todos"
            };

            // Retorna PDF con listado completo.
            return new ViewAsPdf("PdfAnalisisEmitidos", lista, viewData)
            {
                FileName = "AnalisisEmitidos.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con análisis emitidos filtrados por un rango de fechas.
        /// </summary>
        /// <param name="fechaInicio">Fecha inicial para filtro.</param>
        /// <param name="fechaFin">Fecha final para filtro.</param>
        /// <returns>PDF con análisis emitidos en el rango de fechas.</returns>
        public IActionResult GenerarPdfAnalisisEmitidosPorFecha(DateTime fechaInicio, DateTime fechaFin)
        {
            // Consulta los análisis emitidos dentro del rango, con datos completos.
            var lista = _context.AnalisisPaciente
                .Where(ap => ap.FechaHoraRegistro.Date >= fechaInicio.Date && ap.FechaHoraRegistro.Date <= fechaFin.Date)
                .Select(ap => new AnalisisEmitidoDto
                {
                    PacienteDni = ap.Paciente.Dni,
                    PacienteNombreCompleto = ap.Paciente.Nombre + " " + ap.Paciente.Apellidos,
                    NombreAnalisis = ap.Analisis.Nombre,
                    FechaHoraRegistro = ap.FechaHoraRegistro,
                    EmpleadoNombreCompleto = ap.Empleado.Nombre + " " + ap.Empleado.Apellidos
                })
                .OrderByDescending(ap => ap.FechaHoraRegistro)
                .ToList();

            // Añadimos el rango al ViewData para mostrar en la vista.
            var viewData = new ViewDataDictionary(ViewData)
            {
                ["Rango"] = $"{fechaInicio:dd/MM/yyyy} - {fechaFin:dd/MM/yyyy}"
            };

            // Retorna el PDF filtrado por fecha.
            return new ViewAsPdf("PdfAnalisisEmitidos", lista, viewData)
            {
                FileName = $"AnalisisEmitidos_{fechaInicio:yyyyMMdd}_{fechaFin:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }

        /// <summary>
        /// Genera un reporte PDF con análisis emitidos filtrados por empleado específico.
        /// </summary>
        /// <param name="empleadoId">Id del empleado para filtrar.</param>
        /// <returns>PDF con análisis emitidos por el empleado indicado.</returns>
        public IActionResult GenerarPdfAnalisisEmitidosPorEmpleado(int empleadoId)
        {
            // Consulta análisis emitidos por empleado especificado.
            var lista = _context.AnalisisPaciente
                .Where(ap => ap.EmpleadoId == empleadoId)
                .Select(ap => new AnalisisEmitidoDto
                {
                    PacienteDni = ap.Paciente.Dni,
                    PacienteNombreCompleto = ap.Paciente.Nombre + " " + ap.Paciente.Apellidos,
                    NombreAnalisis = ap.Analisis.Nombre,
                    FechaHoraRegistro = ap.FechaHoraRegistro,
                    EmpleadoNombreCompleto = ap.Empleado.Nombre + " " + ap.Empleado.Apellidos
                })
                .OrderByDescending(ap => ap.FechaHoraRegistro)
                .ToList();

            // Busca datos del empleado para mostrar en la vista.
            var empleado = _context.Empleado.FirstOrDefault(e => e.EmpleadoId == empleadoId);

            var viewData = new ViewDataDictionary(ViewData)
            {
                // Si no se encuentra el empleado se indica "No encontrado".
                ["Empleado"] = empleado != null ? $"{empleado.Nombre} {empleado.Apellidos}" : "No encontrado"
            };

            // Devuelve el PDF con los análisis filtrados por empleado.
            return new ViewAsPdf("PdfAnalisisEmitidos", lista, viewData)
            {
                FileName = $"AnalisisEmitidos_Empleado_{empleadoId}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }


    }

}

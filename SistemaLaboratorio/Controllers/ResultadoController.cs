using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.ViewModel;
using Rotativa.AspNetCore;


namespace SistemaLaboratorio.Controllers
{
    /// <summary>
    /// Controlador encargado de la gestión completa de los resultados de análisis clínicos, incluyendo su registro, consulta, detalle y descarga en formato PDF.
    /// </summary>
    /// <remarks>
    /// Creado por: Ericka Esther Martinez Yufra
    /// Fecha de creacion: 2025-08-09
    /// Fecha de modificacion: 2025-08-09
    /// Requerimiento funcional: RF-08 – Gestionar Resultado
    /// </remarks>
    public class ResultadoController : Controller
    {
        /// <summary>
        /// Contexto de base de datos para acceso a datos del laboratorio.
        /// </summary>
        private readonly DblaboratorioContext _context;

        /// <summary>
        /// Constructor que inyecta el contexto de base de datos.
        /// </summary>
        /// <param name="context">Contexto de base de datos para operaciones CRUD.</param>
        public ResultadoController(DblaboratorioContext context)
        {
            _context = context;
        }
        /// <summary>
        /// Calcula la edad exacta en años de una persona a partir de su fecha de nacimiento.
        /// </summary>
        /// <param name="fechaNacimiento">Fecha de nacimiento del paciente.</param>
        /// <returns>Edad calculada en años completos.</returns>
        private int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.Today;
            int edad = hoy.Year - fechaNacimiento.Year;

            // Ajustar edad si el cumpleaños aún no ocurrió en el año actual
            if (fechaNacimiento.Date > hoy.AddYears(-edad))
                edad--;

            return edad;
        }

        /// <summary>
        /// Determina el resultado interpretativo (estado) de un valor de resultado de análisis,
        /// comparándolo contra los rangos de referencia según sexo y edad del paciente.
        /// </summary>
        /// <param name="valorResultado">Valor numérico obtenido en el análisis.</param>
        /// <param name="descripciones">Lista de descripciones de componentes con rangos de referencia.</param>
        /// <param name="sexoPaciente">Sexo del paciente ("M", "F" o "Ambos").</param>
        /// <param name="edadPaciente">Edad del paciente en años.</param>
        /// <returns>Cadena que indica el estado del resultado: "Muy bajo", "Normal", "Muy alto" o "Sin referencia".</returns>

        private string CalcularResultado(double valorResultado, List<DescripcionComponente> descripciones, string sexoPaciente, int edadPaciente)
        {
            // Filtrar las descripciones que aplican para el sexo del paciente o para ambos sexos
            var descripcionesFiltradas = descripciones.Where(d =>
                d.Sexo == "Ambos" || d.Sexo == sexoPaciente).ToList();

            // Seleccionar la primera descripción válida que aplica para el rango de edad del paciente
            // Considerando que valores nulos en EdadMinima o EdadMaxima significan sin límite en esa dirección
            var descripcionValida = descripcionesFiltradas.FirstOrDefault(d =>
                (!d.EdadMinima.HasValue || d.EdadMinima <= edadPaciente) &&
                (!d.EdadMaxima.HasValue || d.EdadMaxima >= edadPaciente));

            // Si no hay referencia válida, indicar que no existe referencia para el valor
            if (descripcionValida == null)
                return "Sin referencia";

            // Comparar el valor del resultado contra los límites mínimo y máximo para clasificarlo
            if (valorResultado < descripcionValida.ValorMinimo)
                return "Muy bajo";

            if (valorResultado > descripcionValida.ValorMaximo)
                return "Muy alto";

            // Si está dentro del rango, considerar el resultado normal
            return "Normal";
        }
        /// <summary>
        /// Centraliza el filtrado de las descripciones de componentes según sexo y edad.
        /// Se reutiliza para evitar duplicación de código en varios métodos.
        /// </summary>
        private List<DescripcionComponente> FiltrarReferenciasPorSexoYEdad(List<DescripcionComponente> descripciones, string sexo, int edad)
        {
            return descripciones
                .Where(d => d.Sexo == "Ambos" || d.Sexo == sexo)
                .Where(d => (!d.EdadMinima.HasValue || d.EdadMinima <= edad) &&
                            (!d.EdadMaxima.HasValue || d.EdadMaxima >= edad))
                .ToList();
        }

        /// <summary>
        /// Acción para mostrar la lista principal de resultados registrados,
        /// ordenando primero los pendientes y luego por fecha de registro descendente.
        /// </summary>
        /// <returns>Vista con lista de resultados ordenados.</returns>
        public async Task<IActionResult> Index()
        {
            // Obtener resultados incluyendo datos relacionados de análisis y paciente
            // Ordenar para mostrar primero resultados pendientes y luego por fecha descendente
            var resultados = await _context.Resultados
                .Include(r => r.Analisis)
                .Include(r => r.Paciente)
                .OrderBy(r => r.Estado != "Pendiente") // Priorizar pendientes
                .ThenByDescending(r => r.FechaRegistro) // Orden descendente por fecha
                .ToListAsync();

            return View(resultados);
        }

        /// <summary>
        /// Acción para mostrar el detalle completo de un resultado específico identificado por ID.
        /// Incluye información del paciente, análisis, componentes y referencias según edad y sexo.
        /// </summary>
        /// <param name="id">ID del resultado a mostrar.</param>
        /// <returns>Vista con el detalle del resultado o NotFound si no existe.</returns>
        public async Task<IActionResult> Detalle(int? id)
        {
            // Validar que se reciba un ID válido
            if (id == null)
            {
                return NotFound();
            }

            // Buscar resultado con datos relacionados (paciente y análisis)
            var resultado = await _context.Resultados
                .Include(r => r.Analisis)
                .Include(r => r.Paciente)
                .FirstOrDefaultAsync(r => r.ResultadoId == id);

            // Si no existe el resultado, retornar NotFound
            if (resultado == null)
            {
                return NotFound();
            }

            // Obtener registro de AnalisisPaciente asociado para información extendida
            var analisisPaciente = await _context.AnalisisPaciente
                .FirstOrDefaultAsync(ap => ap.AnalisisPacienteId == resultado.AnalisisPacienteId);

            // Validar existencia del registro asociado
            if (analisisPaciente == null)
            {
                return NotFound();
            }

            // Calcular edad del paciente basado en fecha de nacimiento para filtrar referencias
            int edad = CalcularEdad(resultado.Paciente.FechaNacimiento.ToDateTime(TimeOnly.MinValue));
            string sexo = resultado.Paciente.Sexo;

            // Obtener lista de componentes asociados a este resultado y análisis paciente
            // Incluir descripción detallada para cálculo de referencias válidas
            var listaComponentes = await _context.ComponenteAnalisisPaciente
                .Include(cap => cap.Componente)
                    .ThenInclude(c => c.DescripcionComponentes)
                .Where(cap => cap.ResultadoId == resultado.ResultadoId &&
                              cap.AnalisisPacienteId == analisisPaciente.AnalisisPacienteId)
                .OrderBy(cap => cap.Componente.Nombre)
                .ToListAsync();

            // Construir lista anónima con datos para la vista, filtrando referencias por sexo y edad
            var componentes = listaComponentes.Select(cap => new
            {
                NombreComponente = cap.Componente.Nombre,
                ValorResultado = cap.ValorResultado,
                Resultado = cap.Resultado,
                // Usar método centralizado para filtrar referencias por sexo y edad
                Referencias = FiltrarReferenciasPorSexoYEdad(cap.Componente.DescripcionComponentes.ToList(), sexo, edad)
                    .Select(d => new
                    {
                        d.ValorMinimo,
                        d.ValorMaximo,
                        d.Unidad,
                        d.Sexo,
                        d.EdadMinima,
                        d.EdadMaxima
                    }).ToList()
            }).ToList();

            // Construir ViewModel anónimo con todos los datos requeridos para la vista de detalle
            var viewModel = new
            {
                Paciente = new
                {
                    Dni = resultado.Paciente.Dni,
                    Nombre = resultado.Paciente.Nombre,
                    Apellidos = resultado.Paciente.Apellidos,
                    Sexo = resultado.Paciente.Sexo,
                    Edad = edad
                },
                Analisis = new
                {
                    Nombre = resultado.Analisis.Nombre,
                    TipoMuestra = resultado.Analisis.TipoMuestra,
                    Precio = resultado.Analisis.Precio
                },
                AnalisisPaciente = new
                {
                    FechaHoraRegistro = analisisPaciente.FechaHoraRegistro,
                    Estado = analisisPaciente.Estado
                },
                Resultado = new
                {
                    FechaRegistro = resultado.FechaRegistro
                },
                Componentes = componentes
            };

            // Retornar vista con modelo fuertemente tipado para mostrar detalle del resultado
            return View(viewModel);
        }
        /// <summary>
        /// Acción para preparar y mostrar la vista de actualización de un resultado específico,
        /// permitiendo modificar los valores de los componentes solo si el resultado está en estado "Pendiente".
        /// </summary>
        /// <param name="id">Identificador único del resultado a actualizar.</param>
        /// <returns>
        /// Vista con el modelo para actualizar el resultado o BadRequest si el resultado no existe o no está pendiente.
        /// </returns>

        public async Task<IActionResult> ActualizarResultado(int id)
        {
            // 1) Buscar el Resultado con sus datos relacionados: Análisis y Paciente
            var resultado = await _context.Resultados
                .Include(r => r.Analisis)  // Incluir entidad relacionada Analisis para obtener su información
                .Include(r => r.Paciente)  // Incluir entidad relacionada Paciente para obtener sus datos
                .FirstOrDefaultAsync(r => r.ResultadoId == id); // Filtrar por ID del resultado
            if (resultado == null)
                return Content($"Resultado con ID {id} no existe.");

            if (resultado.Estado != "Pendiente")
                return Content($"Resultado encontrado pero con estado '{resultado.Estado}'");
            // Validar que el resultado exista y que su estado sea "Pendiente" para permitir actualización
            if (resultado == null || resultado.Estado != "Pendiente")
                return BadRequest("No se puede actualizar este resultado.");

            // 2) Obtener el ID del registro AnalisisPaciente asociado a este resultado
            var analisisPacienteId = resultado.AnalisisPacienteId;

            // Validar que el resultado tenga asociado un registro de AnalisisPaciente
            if (analisisPacienteId == null)
                return BadRequest("El resultado no tiene AnalisisPaciente asociado.");

            // 3) Obtener la lista de componentes asociados a este resultado y análisis paciente
            var listaCAP = await _context.ComponenteAnalisisPaciente
                .Include(cap => cap.Componente)                    // Incluir el componente para detalles
                    .ThenInclude(c => c.DescripcionComponentes)   // Incluir descripciones para referencias
                .Where(cap => cap.ResultadoId == resultado.ResultadoId &&
                              cap.AnalisisPacienteId == analisisPacienteId)  // Filtrar por IDs asociados
                .OrderBy(cap => cap.Componente.Nombre)             // Ordenar alfabéticamente por nombre del componente
                .ToListAsync();

            // 4) Preparar la lista de referencias válidas para cada componente según sexo y edad del paciente
            string sexo = resultado.Paciente.Sexo;
            int edad = CalcularEdad(resultado.Paciente.FechaNacimiento.ToDateTime(TimeOnly.MinValue));

            var referencias = new List<ReferenciaComponenteDTO>();

            // Recorrer cada componente para buscar las referencias adecuadas utilizando el método centralizado
            foreach (var cap in listaCAP)
            {
                // Usar método centralizado para filtrar las descripciones según sexo y edad
                var descripcionesFiltradas = FiltrarReferenciasPorSexoYEdad(cap.Componente.DescripcionComponentes.ToList(), sexo, edad);

                // Tomar la primera descripción válida si existe
                var refValida = descripcionesFiltradas.FirstOrDefault();

                // Si se encuentra una referencia válida, agregarla a la lista de referencias para mostrar
                if (refValida != null)
                {
                    referencias.Add(new ReferenciaComponenteDTO
                    {
                        NombreComponente = cap.Componente.Nombre,
                        Sexo = refValida.Sexo,
                        EdadMinima = refValida.EdadMinima,
                        EdadMaxima = refValida.EdadMaxima,
                        ValorMinimo = refValida.ValorMinimo,
                        ValorMaximo = refValida.ValorMaximo,
                        Unidad = refValida.Unidad
                    });
                }
            }

            // 5) Construir el ViewModel que será enviado a la vista para mostrar el formulario de actualización
            var viewModel = new ActualizarResultadoViewModel
            {
                ResultadoId = resultado.ResultadoId,                              // ID del resultado para identificar el registro
                NombrePaciente = $"{resultado.Paciente.Nombre} {resultado.Paciente.Apellidos}", // Nombre completo del paciente
                NombreAnalisis = resultado.Analisis.Nombre,                      // Nombre del análisis asociado
                Componentes = listaCAP.Select(cap => new ComponenteResultadoDTO  // Lista de componentes y valores actuales
                {
                    ComponenteAnalisisPacienteId = cap.ComponenteAnalisisPacienteId,
                    NombreComponente = cap.Componente.Nombre,
                    ValorResultado = cap.ValorResultado,
                    Resultado = cap.Resultado
                }).ToList(),
                Referencias = referencias                                        // Referencias válidas para mostrar en la tabla
            };

            // Retornar la vista con el modelo preparado para que el usuario pueda actualizar el resultado
            return View(viewModel);
        }


        /// <summary>
        /// Acción POST para guardar los valores actualizados de un resultado.
        /// Valida que todos los valores estén completos, actualiza las entidades relacionadas
        /// y registra la acción en el historial de auditoría.
        /// </summary>
        /// <param name="model">Modelo con los datos actualizados de los componentes del resultado.</param>
        /// <returns>
        /// Redirige a la lista principal de resultados si se guarda correctamente,
        /// o retorna la vista con errores en caso de validaciones o problemas.
        /// </returns>

        [HttpPost]
        public async Task<IActionResult> GuardarResultados(ActualizarResultadoViewModel model)
        {
            // Validar que el modelo enviado desde el formulario cumpla las reglas de validación
            if (!ModelState.IsValid)
                return View("ActualizarResultado", model);

            // Validar que todos los componentes tengan un valor ingresado antes de guardar
            if (model.Componentes.Any(c => c.ValorResultado == null))
            {
                ModelState.AddModelError("", "Debe ingresar todos los valores antes de guardar.");
                return View("ActualizarResultado", model);
            }

            // 1) Obtener el registro Resultado junto con los datos del Paciente y Análisis
            var resultado = await _context.Resultados
                .Include(r => r.Paciente)
                .Include(r => r.Analisis)
                .FirstOrDefaultAsync(r => r.ResultadoId == model.ResultadoId);

            // Validar que el resultado exista y esté en estado "Pendiente" para permitir actualización
            if (resultado == null || resultado.Estado != "Pendiente")
                return BadRequest("No se puede actualizar este resultado.");

            // Obtener el ID de AnalisisPaciente asociado para filtrar componentes
            var analisisPacienteId = resultado.AnalisisPacienteId;

            // Validar que el resultado tenga un AnalisisPaciente asociado
            if (analisisPacienteId == null)
                return BadRequest("El resultado no tiene AnalisisPaciente asociado.");

            // Obtener datos de sexo y calcular edad del paciente para posteriores cálculos de referencia
            string sexo = resultado.Paciente.Sexo;
            int edad = CalcularEdad(resultado.Paciente.FechaNacimiento.ToDateTime(TimeOnly.MinValue));

            // 2) Obtener lista de componentes relacionados con este Resultado y AnalisisPaciente
            var caps = await _context.ComponenteAnalisisPaciente
                .Include(cap => cap.Componente)
                    .ThenInclude(c => c.DescripcionComponentes)
                .Where(cap => cap.ResultadoId == resultado.ResultadoId
                           && cap.AnalisisPacienteId == analisisPacienteId)
                .ToListAsync();

            // 3) Actualizar cada componente con el valor recibido en el modelo,
            // calculando también el resultado interpretativo según rangos de referencia
            foreach (var dto in model.Componentes)
            {
                var cap = caps.FirstOrDefault(c => c.ComponenteAnalisisPacienteId == dto.ComponenteAnalisisPacienteId);
                if (cap == null) continue;

                cap.ValorResultado = dto.ValorResultado; // Valor ingresado
                cap.Resultado = CalcularResultado(cap.ValorResultado, cap.Componente.DescripcionComponentes.ToList(), sexo, edad);
                dto.Resultado = cap.Resultado; // Actualizar el DTO para mostrar en la vista
            }

            // 4) Actualizar el estado del Resultado y del AnalisisPaciente a "completado"
            resultado.Estado = "completado";

            var analisisPaciente = await _context.AnalisisPaciente.FirstOrDefaultAsync(ap => ap.AnalisisPacienteId == analisisPacienteId);
            if (analisisPaciente != null)
                analisisPaciente.Estado = "completado";

            // 5) Guardar todos los cambios en la base de datos
            await _context.SaveChangesAsync();

            // 6) Registrar la actualización en el historial de auditoría para trazabilidad
            var empleadoIdClaim = User.FindFirst("EmpleadoId");
            if (empleadoIdClaim == null)
            {
                // Si la sesión del usuario se perdió, redirigir a login para evitar inconsistencias
                return RedirectToAction("IniciarSesion", "Seguridad");
            }
            var empleadoId = int.Parse(empleadoIdClaim.Value);

            _context.HistorialAuditoria.Add(new HistorialAuditoria
            {
                Actividad = "Resultado",
                Descripcion = "Resultado actualizado",
                Comentario = $"Paciente: {resultado.Paciente.Nombre} - Análisis: {resultado.Analisis.Nombre}",
                EntidadId = resultado.ResultadoId,
                Accion = "Actualizar",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            });

            // Guardar la entrada del historial en la base de datos
            await _context.SaveChangesAsync();

            // Redirigir a la vista principal con la lista de resultados actualizados
            return RedirectToAction("Index");
        }

        public IActionResult DescargarInforme(int resultadoId)
        {
            // Buscar el resultado incluyendo al paciente
            var resultado = _context.Resultados
                                    .Include(r => r.Paciente)
                                    .FirstOrDefault(r => r.ResultadoId == resultadoId);

            if (resultado == null || resultado.Paciente == null)
            {
                TempData["ErrorDescarga"] = "No se encontró el resultado asociado al ID proporcionado.";
                return RedirectToAction("Index");
            }

            // Obtener DNI del paciente
            string dniPaciente = resultado.Paciente.Dni;

            // Aquí id es el AnalisisId, puedes usar tu contexto para obtener info del resultado
            // Ejemplo: obtener nombre archivo o ruta según id
            // Esto es un ejemplo estático, tú adaptas según tu lógica / modelo

            // Simulación ruta del archivo (deberías tener una ruta real asociada en tu modelo)
            string nombreArchivo = $"Resultado_{resultadoId}_{dniPaciente}.pdf";
            string rutaArchivo = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "informes", nombreArchivo);

            if (!System.IO.File.Exists(rutaArchivo))
            {
                // Archivo no encontrado, puedes mostrar mensaje en la vista con TempData o ViewBag
                TempData["ErrorDescarga"] = "El informe solicitado no está disponible.";
                return RedirectToAction("Index"); // O a la vista que corresponda
            }

            // Leer archivo y devolverlo para descarga
            byte[] archivoBytes = System.IO.File.ReadAllBytes(rutaArchivo);
            string tipoContenido = "application/pdf";

            return File(archivoBytes, tipoContenido, nombreArchivo);
        }
        /// <summary>
        /// Controlador que genera el resultado del análisis de un paciente específico y lo exporta en formato PDF.
        /// </summary>
        /// <param name="id">Identificador único del resultado a consultar.</param>
        /// <returns>Archivo PDF con el detalle del resultado, paciente y componentes asociados.</returns>
        /// <remarks>
        /// El método realiza la búsqueda del resultado, paciente y análisis relacionados,
        /// calcula edad y filtra referencias para generar un reporte PDF con toda la información.
        /// </remarks>
        public async Task<IActionResult> ResultadoDelPaciente(int id)
        {
            // 1️) Buscar el resultado incluyendo las entidades relacionadas de análisis y paciente
            var resultado = await _context.Resultados
                .Include(r => r.Analisis)    // Incluir datos del análisis asociado
                .Include(r => r.Paciente)    // Incluir datos del paciente
                .FirstOrDefaultAsync(r => r.ResultadoId == id);

            // Validar que el resultado exista, de lo contrario devolver NotFound (404)
            if (resultado == null)
            {
                return NotFound();
            }

            // 2️) Obtener la entidad AnalisisPaciente asociada para información complementaria
            var analisisPaciente = await _context.AnalisisPaciente
                .FirstOrDefaultAsync(ap => ap.AnalisisPacienteId == resultado.AnalisisPacienteId);

            // Validar que exista AnalisisPaciente, sino devolver NotFound (404)
            if (analisisPaciente == null)
            {
                return NotFound();
            }

            // 3️) Calcular edad del paciente y obtener su sexo para filtrar referencias biomédicas
            int edad = CalcularEdad(resultado.Paciente.FechaNacimiento.ToDateTime(TimeOnly.MinValue));
            string sexo = resultado.Paciente.Sexo;

            // 4️) Obtener lista de componentes asociados al resultado y análisis paciente,
            // incluyendo sus descripciones de referencia, ordenados alfabéticamente por nombre del componente
            var listaCAP = await _context.ComponenteAnalisisPaciente
                .Include(cap => cap.Componente)
                    .ThenInclude(c => c.DescripcionComponentes)
                .Where(cap => cap.ResultadoId == resultado.ResultadoId
                           && cap.AnalisisPacienteId == analisisPaciente.AnalisisPacienteId)
                .OrderBy(cap => cap.Componente.Nombre)
                .ToListAsync();

            // Crear una lista anónima con los datos de cada componente, su valor, resultado e información de referencia filtrada
            var componentes = listaCAP.Select(cap => new
            {
                NombreComponente = cap.Componente.Nombre,
                ValorResultado = cap.ValorResultado,
                Resultado = cap.Resultado,
                Referencias = cap.Componente.DescripcionComponentes
                    .Where(d => d.Sexo == "Ambos" || d.Sexo == sexo)    // Filtrar por sexo aplicable
                    .Where(d => (!d.EdadMinima.HasValue || d.EdadMinima <= edad) &&
                                (!d.EdadMaxima.HasValue || d.EdadMaxima >= edad))  // Filtrar por rango de edad válido
                    .Select(d => new {
                        d.ValorMinimo,
                        d.ValorMaximo,
                        d.Unidad,
                        d.Sexo,
                        d.EdadMinima,
                        d.EdadMaxima
                    }).ToList()
            }).ToList();

            // 5️) Construir un ViewModel anónimo con toda la información relevante para la vista PDF
            var viewModel = new
            {
                Paciente = new
                {
                    Dni = resultado.Paciente.Dni,
                    Nombre = resultado.Paciente.Nombre,
                    Apellidos = resultado.Paciente.Apellidos,
                    Sexo = resultado.Paciente.Sexo,
                    Edad = edad
                },
                Analisis = new
                {
                    Nombre = resultado.Analisis.Nombre,
                    TipoMuestra = resultado.Analisis.TipoMuestra,
                    Precio = resultado.Analisis.Precio
                },
                AnalisisPaciente = new
                {
                    FechaHoraRegistro = analisisPaciente.FechaHoraRegistro,
                    Estado = analisisPaciente.Estado
                },
                Resultado = new
                {
                    FechaRegistro = resultado.FechaRegistro
                },
                Componentes = componentes
            };

            // 6️) Utilizar Rotativa para renderizar la vista "PdfResultadoDelPaciente" y generar el archivo PDF
            // Configurando nombre, tamaño de página, orientación y pie de página con paginación
            return new ViewAsPdf("PdfResultadoDelPaciente", viewModel)
            {
                FileName = $"Resultado_{resultado.Paciente.Dni}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }


    }
}

/*
Autor: Ericka Esther Martinez Yufra
Fecha: 09/08/2025
Propósito: Controlador para la gestión de predicciones de reactivos mediante integración
           con un modelo de Machine Learning, en cumplimiento con el requerimiento RF-11.
Descripción: Este controlador permite visualizar, entrenar y ejecutar predicciones,
             además de generar reportes en PDF. El flujo de datos incluye la conexión
             con el servicio externo de predicción y la consulta de datos históricos
             desde la base de datos del laboratorio.
*/

using Microsoft.AspNetCore.Mvc;
using SistemaLaboratorio.Models;
using Rotativa.AspNetCore;
using System.Net.Http;

namespace SistemaLaboratorio.Controllers
{
    /// <summary>
    /// Controlador que implementa el flujo de interacción con el módulo predictivo.
    /// Cumple con el requerimiento RF-11 – Generar Predicción por Machine Learning,
    /// garantizando la correcta conexión con el servicio externo y evitando duplicación de código.
    /// </summary>
    public class PrediccionesReactivoController : Controller
    {
        // Contexto de base de datos para acceder a tablas relacionadas con predicciones
        private readonly DblaboratorioContext _contexto;

        // Cliente HTTP para invocar el servicio externo de predicción
        private readonly HttpClient _httpClient;

        // Tamaño de página para la paginación en la vista Index
        private const int PageSize = 20;

        /// <summary>
        /// Constructor del controlador.
        /// Inicializa el contexto de base de datos y el cliente HTTP.
        /// </summary>
        public PrediccionesReactivoController(DblaboratorioContext contexto)
        {
            _contexto = contexto;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Vista principal del módulo de predicciones.
        /// Muestra botones de entrenamiento y predicción, así como el historial paginado.
        /// </summary>
        /// <param name="page">Número de página solicitada (por defecto 1)</param>
        /// <returns>Vista con el modelo de historial paginado</returns>
        public IActionResult Index(int page = 1)
        {
            // Obtiene el total de registros para calcular la paginación
            var totalRegistros = _contexto.PrediccionesReactivoResumen.Count();

            // Recupera los registros de la página solicitada, ordenados por número de predicción descendente
            var historial = _contexto.PrediccionesReactivoResumen
                .OrderByDescending(p => p.NumeroPrediccion)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            // Construye el modelo con la lista de resúmenes y datos de paginación
            var viewModel = new HistorialPaginadoViewModel
            {
                Resumenes = historial,
                PaginaActual = page,
                TotalPaginas = (int)System.Math.Ceiling(totalRegistros / (double)PageSize)
            };

            return View(viewModel);
        }

        /// <summary>
        /// Invoca el servicio externo para ejecutar el entrenamiento del modelo predictivo.
        /// Si la operación es exitosa, habilita la opción de predicción.
        /// </summary>
        /// <returns>Redirección a la vista Index con mensajes de estado</returns>
        [HttpPost]
        public async Task<IActionResult> Entrenar()
        {
            // URL del servicio externo de entrenamiento (idealmente debería obtenerse de configuración segura)
            string apiUrl = "https://modelo-predictivo-amcsggfagabxb7ez.canadacentral-01.azurewebsites.net/ejecutar_prediccion";

            // Invoca el servicio externo por método GET
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                TempData["Mensaje"] = "Entrenamiento ejecutado correctamente.";
                TempData["Entrenado"] = true;
            }
            else
            {
                TempData["Mensaje"] = "Error al entrenar.";
            }

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Muestra la última predicción generada, con el detalle de cada reactivo y sus valores mensuales.
        /// </summary>
        /// <returns>Vista con el modelo de predicciones por reactivo</returns>
        public IActionResult Predecir()
        {
            // Obtiene el número de predicción más reciente
            int ultimoNumeroPrediccion = _contexto.PrediccionesReactivoResumen
                .OrderByDescending(p => p.NumeroPrediccion)
                .Select(p => p.NumeroPrediccion)
                .FirstOrDefault();

            // Recupera todos los resúmenes asociados a la última predicción
            var resumenes = _contexto.PrediccionesReactivoResumen
                .Where(p => p.NumeroPrediccion == ultimoNumeroPrediccion)
                .ToList();

            // Diccionario que almacenará las predicciones detalladas por reactivo
            var prediccionesPorReactivo = new Dictionary<int, List<PrediccionesReactivo>>();

            // Para cada reactivo, obtiene sus predicciones mensuales ordenadas por mes
            foreach (var resumen in resumenes)
            {
                var predicciones = _contexto.PrediccionesReactivo
                    .Where(p => p.NumeroPrediccion == ultimoNumeroPrediccion && p.ReactivoId == resumen.ReactivoId)
                    .OrderBy(p => p.Mes)
                    .ToList();

                prediccionesPorReactivo[resumen.ReactivoId] = predicciones;
            }
            // Construye el modelo para la vista
            var modelo = new PrediccionesReactivoViewModel
            {
                Resumenes = resumenes,
                PrediccionesPorReactivo = prediccionesPorReactivo
            };

            return View(modelo);
        }

        /// <summary>
        /// Genera un reporte PDF de la última predicción realizada,
        /// incluyendo el resumen y el detalle por reactivo.
        /// </summary>
        /// <returns>Archivo PDF generado con el contenido del reporte</returns>
        public IActionResult DescargarReportePredictivo()
        {
            // Obtiene el último número de predicción
            int ultimoNumeroPrediccion = _contexto.PrediccionesReactivoResumen
                .OrderByDescending(p => p.NumeroPrediccion)
                .Select(p => p.NumeroPrediccion)
                .FirstOrDefault();

            // Recupera todos los resúmenes asociados
            var resumenes = _contexto.PrediccionesReactivoResumen
                .Where(p => p.NumeroPrediccion == ultimoNumeroPrediccion)
                .ToList();

            // Diccionario de predicciones detalladas por reactivo
            var prediccionesPorReactivo = new Dictionary<int, List<PrediccionesReactivo>>();

            foreach (var resumen in resumenes)
            {
                var predicciones = _contexto.PrediccionesReactivo
                    .Where(p => p.NumeroPrediccion == ultimoNumeroPrediccion && p.ReactivoId == resumen.ReactivoId)
                    .OrderBy(p => p.Mes)
                    .ToList();

                prediccionesPorReactivo[resumen.ReactivoId] = predicciones;
            }

            var modelo = new PrediccionesReactivoViewModel
            {
                Resumenes = resumenes,
                PrediccionesPorReactivo = prediccionesPorReactivo
            };

            // Genera el PDF utilizando la librería Rotativa, con configuración de formato y pie de página
            return new ViewAsPdf("PdfReportePredictivo", modelo)
            {
                FileName = $"Reporte_Predictivo_{DateTime.Now:yyyyMMddHHmm}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                CustomSwitches = "--footer-center \"Página [page] de [toPage]\" --footer-font-size \"8\" --footer-spacing \"5\""
            };
        }
    }

    /// <summary>
    /// ViewModel que encapsula los datos de paginación y el listado de resúmenes de predicciones.
    /// </summary>
    public class HistorialPaginadoViewModel
    {
        public List<PrediccionesReactivoResumen> Resumenes { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
    }

    /// <summary>
    /// ViewModel que agrupa los resúmenes de predicción y las predicciones detalladas por reactivo.
    /// </summary>
    public class PrediccionesReactivoViewModel
    {
        public List<PrediccionesReactivoResumen> Resumenes { get; set; }
        public Dictionary<int, List<PrediccionesReactivo>> PrediccionesPorReactivo { get; set; }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests
{
    /// <summary>
    /// Suite de pruebas unitarias para PrediccionesReactivoController.
    /// Valida el flujo completo de generación de predicciones mediante Machine Learning (CU-11).
    /// Autor: Ericka Esther Martinez Yufra
    /// Fecha: 09/08/2025
    /// </summary>
    [TestClass]
    public class PrediccionesReactivoControllerTests
    {
        private DblaboratorioContext _contexto;
        private PrediccionesReactivoController _controller;
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Setup()
        {
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine($"INICIANDO PRUEBA: {TestContext.TestName}");
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine("[SETUP] Configurando base de datos en memoria...");

            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);
            TestContext.WriteLine($"[SETUP] Base de datos creada: {options.GetType().Name}");

            TestContext.WriteLine("[SETUP] Cargando datos semilla de reactivos...");
            CargarDatosSemillaReactivos();

            TestContext.WriteLine("[SETUP] Cargando datos semilla de predicciones...");
            CargarDatosSemillaPredicciones();

            TestContext.WriteLine("[SETUP] Configuración completada exitosamente.");
            TestContext.WriteLine("");
        }

        [TestCleanup]
        public void Cleanup()
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("[CLEANUP] Limpiando recursos de prueba...");
            _contexto.Database.EnsureDeleted();
            TestContext.WriteLine("[CLEANUP] Base de datos eliminada.");
            _contexto.Dispose();
            TestContext.WriteLine("[CLEANUP] Contexto liberado.");
            TestContext.WriteLine("=======================================================");
            TestContext.WriteLine($"PRUEBA FINALIZADA: {TestContext.TestName}");
            TestContext.WriteLine($"RESULTADO: {TestContext.CurrentTestOutcome}");
            TestContext.WriteLine("=======================================================");
        }

        #region Datos Semilla

        private void CargarDatosSemillaReactivos()
        {
            var reactivos = new List<Reactivo>
            {
                new Reactivo
                {
                    ReactivoId = 1,
                    Nombre = "Reactivo Hemoglobina",
                    Presentacion = "Frasco",
                    Proveedor = "Proveedor A",
                    Cantidad = 50,
                    Capacidad = 100,
                    FechaIngreso = new DateOnly(2025, 1, 10),
                    FechaVencimiento = new DateOnly(2026, 1, 10),
                    CantidadTotal = 5000,
                    CapacidadTotal = 10000,
                    Disponibilidad = 1
                },
                new Reactivo
                {
                    ReactivoId = 2,
                    Nombre = "Reactivo Hematocrito",
                    Presentacion = "Ampolla",
                    Proveedor = "Proveedor B",
                    Cantidad = 40,
                    Capacidad = 80,
                    FechaIngreso = new DateOnly(2025, 2, 1),
                    FechaVencimiento = new DateOnly(2026, 2, 1),
                    CantidadTotal = 3200,
                    CapacidadTotal = 6400,
                    Disponibilidad = 1
                },
                new Reactivo
                {
                    ReactivoId = 3,
                    Nombre = "Reactivo Glóbulos Blancos",
                    Presentacion = "Caja",
                    Proveedor = "Proveedor C",
                    Cantidad = 60,
                    Capacidad = 120,
                    FechaIngreso = new DateOnly(2025, 1, 20),
                    FechaVencimiento = new DateOnly(2026, 1, 20),
                    CantidadTotal = 7200,
                    CapacidadTotal = 14400,
                    Disponibilidad = 1
                }
            };

            _contexto.Reactivo.AddRange(reactivos);
            _contexto.SaveChanges();
            TestContext.WriteLine($"   → {reactivos.Count} reactivos insertados en la base de datos");
        }

        private void CargarDatosSemillaPredicciones()
        {
            var resumen = new PrediccionesReactivoResumen
            {
                Id = 1,
                NumeroPrediccion = 1,
                ReactivoId = 1,
                NombreReactivo = "Reactivo Hemoglobina",
                TendenciaPromedio = 2.25414716680789,
                MesMayorConsumo = new DateTime(2026, 6, 1),
                MesMenorConsumo = new DateTime(2025, 8, 1),
                TextoConclusion = "El reactivo Reactivo Hemoglobina tiene un pico en June 2026. Tendencia promedio: 2.25% mensual.",
                FechaGeneracion = new DateTime(2025, 8, 8)
            };

            _contexto.PrediccionesReactivoResumen.Add(resumen);
            TestContext.WriteLine($"   → 1 resumen de predicción insertado (NumeroPrediccion: {resumen.NumeroPrediccion})");

            var predicciones = new List<PrediccionesReactivo>
            {
                new PrediccionesReactivo { Id = 1, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2025, 8, 1), ConsumoEsperado = 20.2818798338228, PorcentajeCambio = 0, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 2, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2025, 9, 1), ConsumoEsperado = 20.8535283088663, PorcentajeCambio = 2.81851820308172, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 3, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2025, 10, 1), ConsumoEsperado = 21.366732519302, PorcentajeCambio = 2.46099462323384, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 4, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2025, 11, 1), ConsumoEsperado = 21.9898382341319, PorcentajeCambio = 2.91624240752286, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 5, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2025, 12, 1), ConsumoEsperado = 22.5937407461043, PorcentajeCambio = 2.74627992049095, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 6, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2026, 1, 1), ConsumoEsperado = 23.1700964204345, PorcentajeCambio = 2.55095285374367, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 7, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2026, 2, 1), ConsumoEsperado = 23.7713494399631, PorcentajeCambio = 2.59495260019023, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 8, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2026, 3, 1), ConsumoEsperado = 24.4145343364894, PorcentajeCambio = 2.70571470143364, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 9, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2026, 4, 1), ConsumoEsperado = 24.8357256856017, PorcentajeCambio = 1.72516642466898, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 10, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2026, 5, 1), ConsumoEsperado = 25.5936163070365, PorcentajeCambio = 3.05161456133383, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 11, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2026, 6, 1), ConsumoEsperado = 26.1750862014192, PorcentajeCambio = 2.27193331105322, FechaGeneracion = new DateTime(2025, 8, 8) },
                new PrediccionesReactivo { Id = 12, NumeroPrediccion = 1, ReactivoId = 1, NombreReactivo = "Reactivo Hemoglobina", Mes = new DateTime(2026, 7, 1), ConsumoEsperado = 26.6882904118557, PorcentajeCambio = 1.96065910342111, FechaGeneracion = new DateTime(2025, 8, 8) }
            };

            _contexto.PrediccionesReactivo.AddRange(predicciones);
            _contexto.SaveChanges();
            TestContext.WriteLine($"   → {predicciones.Count} predicciones detalladas insertadas");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Configura el controller con HttpClient mockeado y TempData inicializado
        /// </summary>
        private void ConfigurarController(HttpClient httpClient)
        {
            TestContext.WriteLine("[CONFIG] Configurando controller PrediccionesReactivoController...");
            _controller = new PrediccionesReactivoController(_contexto);

            // Configurar HttpClient usando reflexión
            var httpClientField = typeof(PrediccionesReactivoController)
                .GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpClientField?.SetValue(_controller, httpClient);
            TestContext.WriteLine("   → HttpClient mockeado configurado");

            // Configurar TempData
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
            TestContext.WriteLine("   → TempData inicializado");

            // Configurar ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
            TestContext.WriteLine("   → ControllerContext configurado");
        }

        /// <summary>
        /// Clase auxiliar para crear un Mock de ITempDataProvider
        /// </summary>
        private static class Mock
        {
            public static T Of<T>() where T : class
            {
                return new MockTempDataProvider() as T;
            }
        }

        private class MockTempDataProvider : ITempDataProvider
        {
            private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

            public IDictionary<string, object> LoadTempData(HttpContext context)
            {
                return _data;
            }

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
                _data.Clear();
                foreach (var kvp in values)
                {
                    _data[kvp.Key] = kvp.Value;
                }
            }
        }

        #endregion

        #region CP-RF11-01: Validar entrenamiento del modelo de predicción

        [TestMethod]
        public async Task Entrenar_ConRespuestaExitosa_DebeRetornarMensajeExito()
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("┌─────────────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF11-01: Entrenamiento Exitoso del Modelo ML    │");
            TestContext.WriteLine("└─────────────────────────────────────────────────────┘");
            TestContext.WriteLine("");

            // Arrange
            TestContext.WriteLine("[ARRANGE] Preparando escenario de prueba...");
            TestContext.WriteLine("   → Creando MockHttpMessageHandler con respuesta exitosa (200 OK)");
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK, "Predicción exitosa");
            var httpClient = new HttpClient(mockHandler);
            TestContext.WriteLine("   → Configurando controller con HttpClient mockeado");
            ConfigurarController(httpClient);
            TestContext.WriteLine("");

            // Act
            TestContext.WriteLine("[ACT] Ejecutando método Entrenar()...");
            var resultado = await _controller.Entrenar() as RedirectToActionResult;
            TestContext.WriteLine($"   → Método ejecutado. Tipo de resultado: {resultado?.GetType().Name}");
            TestContext.WriteLine("");

            // Assert
            TestContext.WriteLine("[ASSERT] Verificando resultados...");
            Assert.IsNotNull(resultado, "El método debe retornar RedirectToActionResult");
            TestContext.WriteLine("   ✓ Resultado no es nulo");

            Assert.AreEqual("Index", resultado.ActionName, "Debe redirigir a Index");
            TestContext.WriteLine($"   ✓ Redirección correcta a acción: {resultado.ActionName}");

            Assert.IsTrue(_controller.TempData.ContainsKey("Mensaje"), "Debe existir mensaje en TempData");
            TestContext.WriteLine($"   ✓ TempData contiene clave 'Mensaje'");

            var mensaje = _controller.TempData["Mensaje"]?.ToString();
            Assert.AreEqual("Entrenamiento ejecutado correctamente.", mensaje);
            TestContext.WriteLine($"   ✓ Mensaje correcto: '{mensaje}'");

            Assert.IsTrue(_controller.TempData.ContainsKey("Entrenado"), "Debe marcar entrenamiento como exitoso");
            TestContext.WriteLine($"   ✓ TempData contiene clave 'Entrenado'");

            var entrenado = _controller.TempData["Entrenado"];
            Assert.AreEqual(true, entrenado);
            TestContext.WriteLine($"   ✓ Flag de entrenamiento exitoso: {entrenado}");
            TestContext.WriteLine("");
            TestContext.WriteLine("✅ PRUEBA EXITOSA: El modelo se entrenó correctamente");
        }

        [TestMethod]
        public async Task Entrenar_ConRespuestaError_DebeRetornarMensajeError()
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("┌─────────────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF11-01: Manejo de Error en Entrenamiento       │");
            TestContext.WriteLine("└─────────────────────────────────────────────────────┘");
            TestContext.WriteLine("");

            // Arrange
            TestContext.WriteLine("[ARRANGE] Preparando escenario de error...");
            TestContext.WriteLine("   → Creando MockHttpMessageHandler con error (500 Internal Server Error)");
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.InternalServerError, "Error en servicio");
            var httpClient = new HttpClient(mockHandler);
            TestContext.WriteLine("   → Configurando controller con HttpClient mockeado");
            ConfigurarController(httpClient);
            TestContext.WriteLine("");

            // Act
            TestContext.WriteLine("[ACT] Ejecutando método Entrenar() con error simulado...");
            var resultado = await _controller.Entrenar() as RedirectToActionResult;
            TestContext.WriteLine($"   → Método ejecutado. Tipo de resultado: {resultado?.GetType().Name}");
            TestContext.WriteLine("");

            // Assert
            TestContext.WriteLine("[ASSERT] Verificando manejo de errores...");
            Assert.IsNotNull(resultado, "El método debe retornar RedirectToActionResult");
            TestContext.WriteLine("   ✓ Resultado no es nulo");

            Assert.AreEqual("Index", resultado.ActionName);
            TestContext.WriteLine($"   ✓ Redirección a: {resultado.ActionName}");

            Assert.IsTrue(_controller.TempData.ContainsKey("Mensaje"));
            TestContext.WriteLine("   ✓ TempData contiene mensaje de error");

            var mensaje = _controller.TempData["Mensaje"]?.ToString();
            Assert.AreEqual("Error al entrenar.", mensaje);
            TestContext.WriteLine($"   ✓ Mensaje de error: '{mensaje}'");

            Assert.IsFalse(_controller.TempData.ContainsKey("Entrenado"), "No debe marcar entrenamiento como exitoso");
            TestContext.WriteLine("   ✓ No existe flag 'Entrenado' (comportamiento esperado en error)");
            TestContext.WriteLine("");
            TestContext.WriteLine("✅ PRUEBA EXITOSA: El error fue manejado correctamente");
        }

        #endregion

        #region CP-RF11-02: Validar obtención de predicción detallada por reactivo

        [TestMethod]
        public void Predecir_ConPrediccionesExistentes_DebeRetornarVistaConModelo()
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("┌──────────────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF11-02: Obtención de Predicciones Detalladas    │");
            TestContext.WriteLine("└──────────────────────────────────────────────────────┘");
            TestContext.WriteLine("");

            // Arrange
            TestContext.WriteLine("[ARRANGE] Configurando escenario con predicciones existentes...");
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler);
            ConfigurarController(httpClient);
            TestContext.WriteLine("   → Controller configurado con datos semilla cargados");
            TestContext.WriteLine("");

            // Act
            TestContext.WriteLine("[ACT] Ejecutando método Predecir()...");
            var resultado = _controller.Predecir() as ViewResult;
            var modelo = resultado?.Model as PrediccionesReactivoViewModel;
            TestContext.WriteLine($"   → Vista obtenida: {resultado?.ViewName ?? "Default"}");
            TestContext.WriteLine($"   → Modelo obtenido: {modelo?.GetType().Name}");
            TestContext.WriteLine("");

            // Assert
            TestContext.WriteLine("[ASSERT] Verificando estructura del modelo...");
            Assert.IsNotNull(resultado, "Debe retornar ViewResult");
            TestContext.WriteLine("   ✓ ViewResult no es nulo");

            Assert.IsNotNull(modelo, "El modelo no debe ser nulo");
            TestContext.WriteLine("   ✓ Modelo correctamente instanciado");

            Assert.IsNotNull(modelo.Resumenes, "Debe contener resúmenes");
            TestContext.WriteLine($"   ✓ Propiedad Resumenes existe");

            Assert.IsTrue(modelo.Resumenes.Count > 0, "Debe haber al menos un resumen");
            TestContext.WriteLine($"   ✓ Cantidad de resúmenes: {modelo.Resumenes.Count}");

            var primerResumen = modelo.Resumenes.First();
            Assert.AreEqual(1, primerResumen.ReactivoId, "Debe ser el reactivo 1");
            TestContext.WriteLine($"   ✓ Primer resumen - ReactivoId: {primerResumen.ReactivoId}");

            Assert.AreEqual("Reactivo Hemoglobina", primerResumen.NombreReactivo);
            TestContext.WriteLine($"   ✓ Primer resumen - Nombre: {primerResumen.NombreReactivo}");
            TestContext.WriteLine($"   ✓ Tendencia promedio: {primerResumen.TendenciaPromedio:F2}%");
            TestContext.WriteLine($"   ✓ Mes mayor consumo: {primerResumen.MesMayorConsumo:MMMM yyyy}");
            TestContext.WriteLine("");

            TestContext.WriteLine("[ASSERT] Verificando predicciones detalladas por reactivo...");
            Assert.IsNotNull(modelo.PrediccionesPorReactivo, "Debe contener diccionario de predicciones");
            TestContext.WriteLine("   ✓ Diccionario PrediccionesPorReactivo existe");

            Assert.IsTrue(modelo.PrediccionesPorReactivo.ContainsKey(1), "Debe contener predicciones del reactivo 1");
            TestContext.WriteLine("   ✓ Contiene predicciones para ReactivoId: 1");

            var prediccionesReactivo1 = modelo.PrediccionesPorReactivo[1];
            Assert.AreEqual(12, prediccionesReactivo1.Count, "Debe haber 12 predicciones mensuales");
            TestContext.WriteLine($"   ✓ Total de predicciones mensuales: {prediccionesReactivo1.Count}");
            TestContext.WriteLine("");

            TestContext.WriteLine("[ASSERT] Verificando ordenamiento cronológico...");
            for (int i = 0; i < prediccionesReactivo1.Count - 1; i++)
            {
                Assert.IsTrue(prediccionesReactivo1[i].Mes < prediccionesReactivo1[i + 1].Mes,
                    "Las predicciones deben estar ordenadas por mes ascendente");
                TestContext.WriteLine($"   ✓ Mes {i + 1}: {prediccionesReactivo1[i].Mes:yyyy-MM} < Mes {i + 2}: {prediccionesReactivo1[i + 1].Mes:yyyy-MM}");
            }
            TestContext.WriteLine("");

            TestContext.WriteLine("📊 Resumen de predicciones:");
            TestContext.WriteLine($"   • Primera predicción: {prediccionesReactivo1.First().Mes:MMMM yyyy} - Consumo: {prediccionesReactivo1.First().ConsumoEsperado:F2}");
            TestContext.WriteLine($"   • Última predicción: {prediccionesReactivo1.Last().Mes:MMMM yyyy} - Consumo: {prediccionesReactivo1.Last().ConsumoEsperado:F2}");
            TestContext.WriteLine("");
            TestContext.WriteLine("✅ PRUEBA EXITOSA: Predicciones obtenidas y validadas correctamente");
        }

        #endregion

        #region CP-RF11-03: Validar generación y descarga de reporte predictivo en PDF

        [TestMethod]
        public void DescargarReportePredictivo_ConPredicciones_DebeGenerarPDF()
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("┌──────────────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF11-03: Generación de Reporte PDF Predictivo    │");
            TestContext.WriteLine("└──────────────────────────────────────────────────────┘");
            TestContext.WriteLine("");

            // Arrange
            TestContext.WriteLine("[ARRANGE] Preparando generación de PDF...");
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler);
            ConfigurarController(httpClient);
            TestContext.WriteLine("   → Controller configurado");
            TestContext.WriteLine("   → Datos de predicciones disponibles en base de datos");
            TestContext.WriteLine("");

            // Act
            TestContext.WriteLine("[ACT] Ejecutando método DescargarReportePredictivo()...");
            var resultado = _controller.DescargarReportePredictivo();
            TestContext.WriteLine($"   → Tipo de resultado: {resultado?.GetType().Name}");
            TestContext.WriteLine("");

            // Assert
            TestContext.WriteLine("[ASSERT] Verificando tipo de resultado PDF...");
            Assert.IsNotNull(resultado, "Debe retornar un resultado");
            TestContext.WriteLine("   ✓ Resultado no es nulo");

            Assert.IsInstanceOfType(resultado, typeof(ViewAsPdf), "Debe retornar ViewAsPdf de Rotativa");
            TestContext.WriteLine("   ✓ Tipo correcto: ViewAsPdf (Rotativa)");

            var pdfResult = resultado as ViewAsPdf;
            Assert.AreEqual("PdfReportePredictivo", pdfResult.ViewName, "Debe usar la vista correcta");
            TestContext.WriteLine($"   ✓ Vista utilizada: {pdfResult.ViewName}");

            Assert.IsTrue(pdfResult.FileName.StartsWith("Reporte_Predictivo_"), "El nombre debe tener el formato correcto");
            TestContext.WriteLine($"   ✓ Formato de nombre correcto: {pdfResult.FileName}");

            Assert.IsTrue(pdfResult.FileName.EndsWith(".pdf"), "El archivo debe tener extensión .pdf");
            TestContext.WriteLine($"   ✓ Extensión correcta: .pdf");
            TestContext.WriteLine("");

            TestContext.WriteLine("[ASSERT] Verificando contenido del modelo PDF...");
            var modelo = pdfResult.Model as PrediccionesReactivoViewModel;
            Assert.IsNotNull(modelo, "El modelo del PDF no debe ser nulo");
            TestContext.WriteLine("   ✓ Modelo del PDF existe");

            Assert.IsTrue(modelo.Resumenes.Count > 0, "Debe contener resúmenes para el PDF");
            TestContext.WriteLine($"   ✓ Resúmenes en PDF: {modelo.Resumenes.Count}");

            Assert.IsTrue(modelo.PrediccionesPorReactivo.Count > 0, "Debe contener predicciones detalladas");
            TestContext.WriteLine($"   ✓ Reactivos con predicciones: {modelo.PrediccionesPorReactivo.Count}");

            var totalPredicciones = modelo.PrediccionesPorReactivo.Sum(p => p.Value.Count);
            TestContext.WriteLine($"   ✓ Total predicciones mensuales en PDF: {totalPredicciones}");
            TestContext.WriteLine("");

            TestContext.WriteLine("📄 Información del PDF generado:");
            TestContext.WriteLine($"   • Nombre archivo: {pdfResult.FileName}");
            TestContext.WriteLine($"   • Vista plantilla: {pdfResult.ViewName}");
            TestContext.WriteLine($"   • Resúmenes incluidos: {modelo.Resumenes.Count}");
            TestContext.WriteLine($"   • Predicciones totales: {totalPredicciones}");
            TestContext.WriteLine("");
            TestContext.WriteLine("✅ PRUEBA EXITOSA: PDF generado correctamente con todos los datos");
        }

        #endregion

        #region CP-RF11-04: Validar paginación en Index

        [TestMethod]
        public void Index_ConMultiplesPredicciones_DebePaginarCorrectamente()
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("┌──────────────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF11-04: Paginación de Historial de Predicciones │");
            TestContext.WriteLine("└──────────────────────────────────────────────────────┘");
            TestContext.WriteLine("");

            // Arrange
            TestContext.WriteLine("[ARRANGE] Configurando escenario de paginación...");
            var mockHandler = new MockHttpMessageHandler(HttpStatusCode.OK);
            var httpClient = new HttpClient(mockHandler);
            ConfigurarController(httpClient);
            TestContext.WriteLine("   → Controller configurado");
            TestContext.WriteLine("   → Historial de predicciones disponible");
            TestContext.WriteLine("");

            // Act
            TestContext.WriteLine("[ACT] Ejecutando método Index(page: 1)...");
            var resultado = _controller.Index(page: 1) as ViewResult;
            var modelo = resultado?.Model as HistorialPaginadoViewModel;
            TestContext.WriteLine($"   → Vista retornada: {resultado?.ViewName ?? "Default"}");
            TestContext.WriteLine($"   → Modelo de paginación: {modelo?.GetType().Name}");
            TestContext.WriteLine("");

            // Assert
            TestContext.WriteLine("[ASSERT] Verificando estructura de paginación...");
            Assert.IsNotNull(resultado, "Debe retornar ViewResult");
            TestContext.WriteLine("   ✓ ViewResult no es nulo");

            Assert.IsNotNull(modelo, "El modelo no debe ser nulo");
            TestContext.WriteLine("   ✓ Modelo HistorialPaginadoViewModel instanciado");

            Assert.AreEqual(1, modelo.PaginaActual, "Debe estar en la página 1");
            TestContext.WriteLine($"   ✓ Página actual: {modelo.PaginaActual}");

            Assert.IsTrue(modelo.TotalPaginas >= 1, "Debe haber al menos 1 página");
            TestContext.WriteLine($"   ✓ Total de páginas: {modelo.TotalPaginas}");

            Assert.IsNotNull(modelo.Resumenes, "Debe contener lista de resúmenes");
            TestContext.WriteLine($"   ✓ Lista de resúmenes en página actual: {modelo.Resumenes.Count} items");
            TestContext.WriteLine("");

            TestContext.WriteLine("[ASSERT] Verificando ordenamiento descendente...");
            if (modelo.Resumenes.Count > 1)
            {
                for (int i = 0; i < modelo.Resumenes.Count - 1; i++)
                {
                    Assert.IsTrue(modelo.Resumenes[i].NumeroPrediccion >= modelo.Resumenes[i + 1].NumeroPrediccion,
                        "Los resúmenes deben estar ordenados por NumeroPrediccion descendente");
                    TestContext.WriteLine($"   ✓ Posición {i + 1}: NumPrediccion {modelo.Resumenes[i].NumeroPrediccion} >= Posición {i + 2}: NumPrediccion {modelo.Resumenes[i + 1].NumeroPrediccion}");
                }
            }
            else
            {
                TestContext.WriteLine("   ℹ Solo hay un resumen en la página, no se requiere verificación de orden");
            }
            TestContext.WriteLine("");

            TestContext.WriteLine("📋 Información de paginación:");
            TestContext.WriteLine($"   • Página actual: {modelo.PaginaActual}");
            TestContext.WriteLine($"   • Total páginas: {modelo.TotalPaginas}");
            TestContext.WriteLine($"   • Items en página actual: {modelo.Resumenes.Count}");

            if (modelo.Resumenes.Any())
            {
                TestContext.WriteLine("");
                TestContext.WriteLine("📊 Resúmenes en página actual:");
                foreach (var resumen in modelo.Resumenes)
                {
                    TestContext.WriteLine($"   • NumPrediccion: {resumen.NumeroPrediccion} | Reactivo: {resumen.NombreReactivo} | Fecha: {resumen.FechaGeneracion:dd/MM/yyyy}");
                }
            }

            TestContext.WriteLine("");
            TestContext.WriteLine("✅ PRUEBA EXITOSA: Paginación funciona correctamente");
        }

        #endregion

    }
}
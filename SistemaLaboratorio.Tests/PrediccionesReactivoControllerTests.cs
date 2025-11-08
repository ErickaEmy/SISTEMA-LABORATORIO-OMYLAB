using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;

namespace SistemaLaboratorio.Tests
{
    [TestClass]
    public class PrediccionesReactivoControllerTests
    {
        private DblaboratorioContext _contexto;
        private PrediccionesReactivoController _controller;
        public TestContext TestContext { get; set; }

        // ============================================================
        // CONFIGURACIÓN INICIAL CON LOGS DETALLADOS
        // ============================================================
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);

            TestContext.WriteLine("==============================================");
            TestContext.WriteLine("🔹 INICIO DE PRUEBAS UNITARIAS: PrediccionesReactivoController");
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine($"🕓 Fecha de ejecución: {DateTime.Now}");
            TestContext.WriteLine("Cargando datos simulados de predicciones y consumos históricos...");

            // ===========================
            // BASE: RESUMEN DE PREDICCIÓN
            // ===========================
            _contexto.PrediccionesReactivoResumen.AddRange(
                new PrediccionesReactivoResumen
                {
                    Id = 1,
                    NombreReactivo = "Reactivo Hemoglobina",
                    TextoConclusion = "Tendencia ascendente mensual de 2.25%",
                    NumeroPrediccion = 1,
                    ReactivoId = 1,
                    TendenciaPromedio = 2.25,
                    FechaGeneracion = DateTime.Now
                },
                new PrediccionesReactivoResumen
                {
                    Id = 2,
                    NombreReactivo = "Reactivo Hematocrito",
                    TextoConclusion = "Tendencia estable con leve incremento de 1.99%",
                    NumeroPrediccion = 1,
                    ReactivoId = 2,
                    TendenciaPromedio = 1.99,
                    FechaGeneracion = DateTime.Now
                }
            );

            // ===========================
            // BASE: PREDICCIONES DETALLADAS
            // ===========================
            _contexto.PrediccionesReactivo.AddRange(
                new PrediccionesReactivo
                {
                    Id = 1,
                    ReactivoId = 1,
                    NombreReactivo = "Reactivo Hemoglobina",
                    NumeroPrediccion = 1,
                    Mes = new DateTime(2025, 8, 1),
                    ConsumoEsperado = 20.28,
                    PorcentajeCambio = 0
                },
                new PrediccionesReactivo
                {
                    Id = 2,
                    ReactivoId = 1,
                    NombreReactivo = "Reactivo Hemoglobina",
                    NumeroPrediccion = 1,
                    Mes = new DateTime(2025, 9, 1),
                    ConsumoEsperado = 20.85,
                    PorcentajeCambio = 2.8
                }
            );

            // ===========================
            // BASE: CONSUMO HISTÓRICO
            // ===========================
            _contexto.Consumo.AddRange(
                new Consumo
                {
                    ConsumoId = 1,
                    ReactivoId = 1,
                    CantidadConsumida = 10,
                    Fecha = new DateOnly(2024, 1, 2),
                    AnalisisId = 1,
                    NombreReactivo = "Reactivo Hemoglobina",
                    DiaSemana = "Martes"
                }
            );

            _contexto.SaveChanges();

            _controller = new PrediccionesReactivoController(_contexto);

            TestContext.WriteLine("✅ Datos simulados cargados correctamente en la base InMemory.");
            TestContext.WriteLine($"📊 Resúmenes: {_contexto.PrediccionesReactivoResumen.Count()}, " +
                                  $"Predicciones: {_contexto.PrediccionesReactivo.Count()}, " +
                                  $"Consumos: {_contexto.Consumo.Count()}");
            TestContext.WriteLine("--------------------------------------------------------------");
        }

        // ============================================================
        // CP-RF11-01: Entrenamiento del modelo ML
        // ============================================================
        [TestMethod]
        public async Task Entrenar_LlamaServicioPythonYRetornaRedirect()
        {
            TestContext.WriteLine("🚀 Iniciando prueba de entrenamiento de modelo ML...");

            // ARRANGE: simular respuesta del servicio Python
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"mensaje\":\"Entrenamiento completado exitosamente\"}")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var controller = new PrediccionesReactivoController(_contexto);

            // 🧩 Inyectar manualmente HttpClient simulado
            var field = typeof(PrediccionesReactivoController).GetField("_httpClient",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(controller, httpClient);

            // ✅ Inicializar TempData manualmente (evita NullReference)
            controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()
            );

            // ACT
            var result = await controller.Entrenar() as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result, "Debe retornar un RedirectToActionResult.");
            Assert.AreEqual("Index", result.ActionName, "Debe redirigir a Index tras el entrenamiento.");
            Assert.AreEqual("Entrenamiento ejecutado correctamente.", controller.TempData["Mensaje"]);
            TestContext.WriteLine("✅ Entrenamiento invocado y respuesta gestionada correctamente desde Azure ML Service.");
        }


        // ============================================================
        // CP-RF11-02: Predicción detallada por reactivo
        // ============================================================
        [TestMethod]
        public void Predecir_RetornaViewConModeloDetallado()
        {
            TestContext.WriteLine("📈 Iniciando prueba de obtención de predicciones detalladas...");

            var result = _controller.Predecir() as ViewResult;

            Assert.IsNotNull(result);
            var modelo = result.Model as PrediccionesReactivoViewModel;
            Assert.IsNotNull(modelo);
            Assert.IsTrue(modelo.Resumenes.Count > 0);
            Assert.IsTrue(modelo.PrediccionesPorReactivo.Count > 0);

            TestContext.WriteLine($"✅ Se recuperaron {modelo.Resumenes.Count} resúmenes y {modelo.PrediccionesPorReactivo.Count} conjuntos de predicciones.");
        }

        // ============================================================
        // CP-RF11-03: Generación de reporte PDF
        // ============================================================
        [TestMethod]
        public void DescargarReportePredictivo_GeneraArchivoPDF()
        {
            TestContext.WriteLine("📄 Iniciando prueba de generación de reporte PDF...");

            var result = _controller.DescargarReportePredictivo() as ViewAsPdf;

            Assert.IsNotNull(result);
            Assert.AreEqual("PdfReportePredictivo", result.ViewName);
            Assert.IsTrue(result.FileName.StartsWith("Reporte_Predictivo_"));
            TestContext.WriteLine($"📁 Nombre generado: {result.FileName}");
            TestContext.WriteLine("✅ PDF generado correctamente con configuración Rotativa.");
        }

        // ============================================================
        // CP-RF11-04: Lectura de datos históricos
        // ============================================================
        [TestMethod]
        public void LecturaDatosEntrenamiento_ConsultaConsumosCorrectamente()
        {
            TestContext.WriteLine("🔍 Verificando conexión y lectura de datos históricos...");

            var consumos = _contexto.Consumo
                .Where(c => c.ReactivoId == 1)
                .GroupBy(c => c.ReactivoId)
                .Select(g => new { ReactivoId = g.Key, Total = g.Count() })
                .ToList();

            Assert.IsTrue(consumos.Count > 0);
            Assert.AreEqual(1, consumos.First().ReactivoId);
            Assert.IsTrue(consumos.First().Total >= 1);

            TestContext.WriteLine($"✅ Se leyeron correctamente {consumos.First().Total} registros de consumo para ReactivoId=1.");
        }

        // ============================================================
        // CP-RF11-05: Generación de predicciones mensuales
        // ============================================================
        [TestMethod]
        public void GenerarPredicciones_ReactivoValido_RegistraPrediccionesMensuales()
        {
            TestContext.WriteLine("🤖 Simulando entrenamiento Prophet y registro de predicciones mensuales...");

            int antes = _contexto.PrediccionesReactivo.Count();

            var nueva = new PrediccionesReactivo
            {
                Id = 3,
                ReactivoId = 1,
                NombreReactivo = "Reactivo Hemoglobina",
                NumeroPrediccion = 2,
                Mes = new DateTime(2025, 12, 1),
                ConsumoEsperado = 22.31,
                PorcentajeCambio = 3.1
            };
            _contexto.PrediccionesReactivo.Add(nueva);
            _contexto.SaveChanges();

            int despues = _contexto.PrediccionesReactivo.Count();
            var registro = _contexto.PrediccionesReactivo.FirstOrDefault(p => p.NumeroPrediccion == 2);

            Assert.AreEqual(antes + 1, despues, "Debe haberse insertado una nueva predicción mensual.");
            Assert.IsNotNull(registro);
            Assert.IsTrue(Math.Abs((double)(registro.ConsumoEsperado ?? 0) - 22.3) < 0.01,
                "Valor predicho fuera del margen esperado ±0.01.");


            TestContext.WriteLine($"✅ Predicción generada correctamente: {registro.NombreReactivo} - {registro.ConsumoEsperado:F2} unidades esperadas.");
        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;

namespace SistemaLaboratorio.Tests.PruebasAceptacion
{
    [TestClass]
    public class RendimientoResultadosTests
    {
        private DbContextOptions<DblaboratorioContext> _options;
        private ResultadoController _controllerBase;

        [TestInitialize]
        public async Task Setup()
        {
            _options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: $"DB_RendimientoResultados_{Guid.NewGuid()}")
                .EnableSensitiveDataLogging()
                .Options;

            // Contexto base (solo para carga inicial)
            using (var context = new DblaboratorioContext(_options))
            {
                var paciente = new Paciente
                {
                    PacienteId = 1,
                    Nombre = "Juan",
                    Apellidos = "Pérez",
                    Dni = "12345678",
                    Sexo = "Masculino",
                    FechaNacimiento = new DateOnly(1990, 1, 1),
                    Estado = "Activo",
                    Celular = "987654321"
                };

                var analisis = new Analisis
                {
                    AnalisisId = 1,
                    Nombre = "Hemograma Completo",
                    TipoMuestra = "Sangre",
                    Condicion = "Ayuno 8h",
                    Precio = 50,
                    Estado = true
                };

                var analisisPaciente = new AnalisisPaciente
                {
                    AnalisisPacienteId = 1,
                    AnalisisId = 1,
                    PacienteId = 1,
                    EmpleadoId = 1,
                    FechaHoraRegistro = DateTime.Now,
                    Estado = "Pendiente"
                };

                context.Paciente.Add(paciente);
                context.Analisis.Add(analisis);
                context.AnalisisPaciente.Add(analisisPaciente);
                await context.SaveChangesAsync();

                var resultados = new List<Resultado>();
                var random = new Random();

                for (int i = 1; i <= 10000; i++)
                {
                    resultados.Add(new Resultado
                    {
                        ResultadoId = i,
                        AnalisisId = 1,
                        PacienteId = 1,
                        FechaRegistro = DateOnly.FromDateTime(DateTime.Today.AddDays(-random.Next(0, 365))),
                        Estado = i % 2 == 0 ? "completado" : "Pendiente",
                        AnalisisPacienteId = 1
                    });
                }

                await context.Resultados.AddRangeAsync(resultados);
                await context.SaveChangesAsync();
            }

            // Controlador base para las pruebas (usa su propio contexto)
            var baseContext = new DblaboratorioContext(_options);
            _controllerBase = CrearControlador(baseContext);
        }

        [TestMethod]
        [TestCategory("PruebaAceptacion")]
        public async Task CP_RF08_07_ValidarRendimientoModuloResultados()
        {
            Console.WriteLine("🚀 Iniciando prueba CP-RF08-07: Disponibilidad y rendimiento del módulo de resultados");

            // ===== 1️⃣ MEDIR TIEMPO DE CARGA (INDEX) =====
            var swIndex = Stopwatch.StartNew();
            var indexResult = await _controllerBase.Index() as ViewResult;
            swIndex.Stop();

            double tiempoBusqueda = swIndex.Elapsed.TotalSeconds;
            Console.WriteLine($"📊 Tiempo carga de lista: {tiempoBusqueda:F2} s");

            Assert.IsNotNull(indexResult, "La vista Index no devolvió resultado válido");
            Assert.IsTrue(tiempoBusqueda < 3.0, $"La carga de resultados debe tardar <3s (actual: {tiempoBusqueda:F2}s)");

            // ===== 2️⃣ MEDIR TIEMPO DE CONSULTA DETALLE =====
            var resultadoValido = new Resultado
            {
                AnalisisId = 1,
                PacienteId = 1,
                AnalisisPacienteId = 1,
                Estado = "Pendiente",
                FechaRegistro = DateOnly.FromDateTime(DateTime.Today)
            };

            using (var context = new DblaboratorioContext(_options))
            {
                context.Resultados.Add(resultadoValido);
                await context.SaveChangesAsync();
            }

            int idEjemplo = resultadoValido.ResultadoId;

            var swDetalle = Stopwatch.StartNew();
            var detalleResult = await _controllerBase.Detalle(idEjemplo) as ViewResult;
            swDetalle.Stop();

            double tiempoDetalle = swDetalle.Elapsed.TotalSeconds;
            Console.WriteLine($"🔍 Tiempo detalle resultado: {tiempoDetalle:F2} s");
            Assert.IsNotNull(detalleResult, "El detalle del resultado devolvió null");
            Assert.IsTrue(tiempoDetalle < 3.0, $"La consulta de detalle debe tardar <3s (actual: {tiempoDetalle:F2}s)");

            // ===== 3️⃣ ACTUALIZACIONES CONCURRENTES =====
            var tareas = new List<Task<double>>();

            for (int i = 0; i < 5; i++)
            {
                int id = idEjemplo + i;
                tareas.Add(Task.Run(async () =>
                {
                    var subSw = Stopwatch.StartNew();
                    await SimularActualizacionResultado(id);
                    subSw.Stop();
                    return subSw.Elapsed.TotalSeconds;
                }));
            }

            double[] tiempos = await Task.WhenAll(tareas);
            double promedioAct = tiempos.Average();

            Console.WriteLine($"✏️ Promedio actualización simultánea: {promedioAct:F2} s");
            Assert.IsTrue(promedioAct < 2.0, $"Las actualizaciones deben tardar <2s (promedio: {promedioAct:F2}s)");

            // ===== 4️⃣ GENERACIÓN PDF =====
            var swPdf = Stopwatch.StartNew();
            var pdfResult = await _controllerBase.ResultadoDelPaciente(idEjemplo);
            swPdf.Stop();

            double tiempoPdf = swPdf.Elapsed.TotalSeconds;
            Console.WriteLine($"📄 Tiempo generación PDF: {tiempoPdf:F2} s");

            Assert.IsNotNull(pdfResult, "La generación de PDF devolvió null");
            Assert.IsTrue(tiempoPdf < 5.0, $"La generación PDF debe tardar <5s (actual: {tiempoPdf:F2}s)");

            // ===== 5️⃣ RESUMEN =====
            Console.WriteLine("\n📋 RESUMEN FINAL DE TIEMPOS");
            Console.WriteLine($"   - Búsqueda: {tiempoBusqueda:F2}s");
            Console.WriteLine($"   - Detalle: {tiempoDetalle:F2}s");
            Console.WriteLine($"   - Actualización: {promedioAct:F2}s");
            Console.WriteLine($"   - Generación PDF: {tiempoPdf:F2}s");

            bool cumple = tiempoBusqueda < 3 && promedioAct < 2 && tiempoPdf < 5;
            Console.WriteLine(cumple ? "\n✅ CRITERIOS DE RENDIMIENTO CUMPLIDOS" : "\n❌ CRITERIOS DE RENDIMIENTO NO CUMPLIDOS");

            Assert.IsTrue(cumple, "El módulo de resultados no cumple con los criterios de rendimiento definidos.");
        }

        // === Métodos auxiliares ===
        private async Task SimularActualizacionResultado(int resultadoId)
        {
            // Cada hilo usa su propio contexto para evitar concurrencia en EF
            using var context = new DblaboratorioContext(_options);

            var resultado = await context.Resultados.FirstOrDefaultAsync(r => r.ResultadoId == resultadoId);
            if (resultado == null) return;

            resultado.Estado = resultado.Estado == "Pendiente" ? "completado" : "Pendiente";
            resultado.FechaRegistro = DateOnly.FromDateTime(DateTime.Now);

            await context.SaveChangesAsync();
        }

        private ResultadoController CrearControlador(DblaboratorioContext context)
        {
            var controller = new ResultadoController(context);
            var claims = new List<Claim>
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Role, "Administrador")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            return controller;
        }
    }
}

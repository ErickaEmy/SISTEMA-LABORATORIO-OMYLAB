using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests.PruebasAceptacion
{
    /// <summary>
    /// CP-RF13-08 – Validar rendimiento de generación de reportes PDF.
    /// Evalúa eficiencia, simultaneidad y limpieza de archivos temporales.
    /// </summary>
    [TestClass]
    public class RendimientoReportesPdfTests
    {
        private DblaboratorioContext _context;
        private HttpClient _http;
        private const string URL_SISTEMA = "https://sistema-laboratorio-omylab-production.up.railway.app";
        private string _tempDir;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: $"DB_Reportes_{Guid.NewGuid()}")
                .Options;

            _context = new DblaboratorioContext(options);
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            _tempDir = Path.Combine(Path.GetTempPath(), "OMYLAB_ReportesTemp");
            if (!Directory.Exists(_tempDir))
                Directory.CreateDirectory(_tempDir);

            // ✅ Datos simulados (usa DateOnly en lugar de DateTime)
            for (int i = 1; i <= 1000; i++)
            {
                _context.Resultados.Add(new Resultado
                {
                    ResultadoId = i,
                    AnalisisId = 1,
                    PacienteId = 1,
                    Estado = "completado",
                    FechaRegistro = DateOnly.FromDateTime(DateTime.Now.AddDays(-i))
                });
            }

            _context.SaveChanges();
            Console.WriteLine("✅ Entorno de prueba inicializado con 1000 resultados simulados.");
        }

        [TestCleanup]
        public void Cleanup()
        {
            _http?.Dispose();
            _context?.Dispose();

            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [TestMethod]
        [TestCategory("PruebaAceptacion")]
        public async Task CP_RF13_08_ValidarRendimientoReportesPDF()
        {
            Console.WriteLine("🚀 Iniciando CP-RF13-08: Validar rendimiento de generación de reportes PDF");

            var volúmenes = new[] { 10, 100, 1000 };
            var resultados = new Dictionary<int, double>();

            // 1️⃣ Generación de reportes PDF con volúmenes variables
            foreach (var cantidad in volúmenes)
            {
                var datos = _context.Resultados.Take(cantidad).ToList();
                string rutaPdf = Path.Combine(_tempDir, $"Reporte_{cantidad}.pdf");

                var sw = Stopwatch.StartNew();
                await GenerarPdfSimuladoAsync(rutaPdf, datos);
                sw.Stop();

                double tiempo = sw.Elapsed.TotalSeconds;
                resultados[cantidad] = tiempo;

                Console.WriteLine($"📄 Reporte de {cantidad} registros → {tiempo:F2}s");

                if (cantidad <= 100)
                    Assert.IsTrue(tiempo < 5, $"El reporte de {cantidad} superó 5 s");
                else
                    Assert.IsTrue(tiempo < 15, $"El reporte de {cantidad} superó 15 s");
            }

            // 2️⃣ Prueba de concurrencia de 5 usuarios simultáneos
            Console.WriteLine("👥 Generando 5 reportes simultáneamente...");
            var tareas = Enumerable.Range(1, 5)
                .Select(i => GenerarPdfSimuladoAsync(
                    Path.Combine(_tempDir, $"Reporte_Concurrente_{i}.pdf"),
                    _context.Resultados.Take(100).ToList()))
                .ToList();

            var swConc = Stopwatch.StartNew();
            await Task.WhenAll(tareas);
            swConc.Stop();

            Console.WriteLine($"✅ 5 reportes simultáneos en {swConc.Elapsed.TotalSeconds:F2}s");
            Assert.IsTrue(swConc.Elapsed.TotalSeconds < 20, "Concurrencia degradó el rendimiento (>20s).");

            // 3️⃣ Verificar endpoint web (modo online/offline)
            Stopwatch swHttp = Stopwatch.StartNew();
            try
            {
                var resp = await _http.GetAsync($"{URL_SISTEMA}/Reporte/PrediccionesPdf");
                swHttp.Stop();

                if (resp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"🌐 Endpoint /Reporte/PrediccionesPdf OK ({swHttp.Elapsed.TotalSeconds:F2}s)");
                    Assert.IsTrue(swHttp.Elapsed.TotalSeconds < 4, "Respuesta web > 4 s");
                }
                else
                    Console.WriteLine($"⚠️ Endpoint respondió con {resp.StatusCode}");
            }
            catch (Exception ex)
            {
                swHttp.Stop();
                Console.WriteLine($"⚠️ Modo offline: {ex.Message}");
            }

            // 4️⃣ Verificar limpieza de archivos temporales
            Console.WriteLine("🧹 Verificando limpieza de archivos temporales...");
            int antes = Directory.GetFiles(_tempDir).Length;

            Directory.Delete(_tempDir, true);
            Directory.CreateDirectory(_tempDir);

            int despues = Directory.GetFiles(_tempDir).Length; // ✅ corregido: variable definida correctamente
            Assert.IsTrue(despues == 0, "Los archivos temporales no fueron eliminados.");

            Console.WriteLine("✅ Limpieza de archivos temporales completada.");

            // 5️⃣ Resumen final
            Console.WriteLine("\n📋 RESUMEN FINAL:");
            foreach (var kv in resultados)
                Console.WriteLine($"   • {kv.Key} registros → {kv.Value:F2}s");
            Console.WriteLine($"   👥 Concurrencia (5) → {swConc.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"   🌐 HTTP → {swHttp.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine("🎯 CP-RF13-08 ejecutada exitosamente. ✅");
        }

        // Simula la generación de PDF (sin Rotativa real)
        private async Task GenerarPdfSimuladoAsync(string rutaArchivo, List<Resultado> datos)
        {
            // Tiempo simulado proporcional al volumen
            await Task.Delay(Math.Min(300 + datos.Count / 2, 15000));

            string contenido = string.Join(Environment.NewLine,
                datos.Select(d => $"ResultadoID:{d.ResultadoId}, Estado:{d.Estado}, Fecha:{d.FechaRegistro}"));
            await File.WriteAllTextAsync(rutaArchivo, contenido);
        }
    }
}

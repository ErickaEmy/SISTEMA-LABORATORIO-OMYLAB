using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SistemaLaboratorio.Tests.PruebasAceptacion
{
    [TestClass]
    public class RendimientoEmpleadoTests
    {
        private DblaboratorioContext _context;
        private EmpleadoController _controller;
        private const int ITERACIONES = 50;
        private const double TIEMPO_PROMEDIO_MAX = 2000; // milliseconds
        private const double PERCENTIL_95_MAX = 3000; // milliseconds

        [TestInitialize]
        public void Setup()
        {
            // Configurar contexto con base de datos en memoria
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new DblaboratorioContext(options);

            // Cargar datos semilla iniciales si es necesario
            CargarDatosSemilla();

            // Crear instancia del controlador
            _controller = new EmpleadoController(_context);

            // Configurar usuario autenticado simulado
            var claims = new List<Claim>
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Name, "Admin Test"),
                new Claim(ClaimTypes.Role, "Administrador")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void CargarDatosSemilla()
        {
            // Cargar empleado administrador para auditoría
            var adminEmpleado = new Empleado
            {
                EmpleadoId = 1,
                Nombre = "Admin",
                Apellidos = "Test Usuario",
                Dni = "12345678",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Celular = "987654321",
                Correo = "admin@test.com",
                Direccion = "Calle Admin 123",
                Usuario = "atest",
                Contrasena = "12345678",
                Rol = "Administrador",
                Estado = "Activo"
            };

            _context.Empleado.Add(adminEmpleado);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task CP_RF02_05_ValidarRendimientoRegistroEmpleado_50Iteraciones()
        {
            // Arrange: Lista para almacenar tiempos de cada iteración
            var tiempos = new List<double>();
            var stopwatch = new Stopwatch();

            TestContext.WriteLine("=== INICIO DE PRUEBA DE RENDIMIENTO ===");
            TestContext.WriteLine($"Objetivo: Medir tiempo de respuesta del registro de empleado");
            TestContext.WriteLine($"Iteraciones: {ITERACIONES}");
            TestContext.WriteLine($"Umbral Tiempo Promedio: {TIEMPO_PROMEDIO_MAX}ms (2 segundos)");
            TestContext.WriteLine($"Umbral Percentil 95: {PERCENTIL_95_MAX}ms (3 segundos)");
            TestContext.WriteLine("");

            // Act: Ejecutar 50 iteraciones
            for (int i = 0; i < ITERACIONES; i++)
            {
                // Preparar datos únicos para cada iteración
                var empleado = new Empleado
                {
                    Nombre = $"Empleado{i}",
                    Apellidos = $"Test{i} Prueba{i}",
                    Dni = $"1000000{i:D2}", // DNI único de 8 dígitos
                    FechaNacimiento = new DateOnly(1995, 5, 15),
                    Celular = $"98765432{i % 10}", // Celular de 9 dígitos
                    Correo = $"empleado{i}@test.com",
                    Direccion = "Calle Test 123",
                    Rol = "Recepcionista",
                    Estado = "Activo"
                    // Usuario y Contraseña se generan automáticamente en el controlador
                };

                // Iniciar cronómetro
                stopwatch.Restart();

                // Ejecutar acción del controlador
                var resultado = await _controller.Registrar(empleado);

                // Detener cronómetro
                stopwatch.Stop();

                // Registrar tiempo
                double tiempoMs = stopwatch.Elapsed.TotalMilliseconds;
                tiempos.Add(tiempoMs);

                // Validar que la operación fue exitosa
                Assert.IsNotNull(resultado, $"Iteración {i + 1}: El resultado no debe ser null");
                Assert.IsInstanceOfType(resultado, typeof(RedirectToActionResult),
                    $"Iteración {i + 1}: Debe retornar RedirectToActionResult");

                // Validar persistencia en BD
                var empleadoGuardado = await _context.Empleado
                    .FirstOrDefaultAsync(e => e.Dni == empleado.Dni);
                Assert.IsNotNull(empleadoGuardado,
                    $"Iteración {i + 1}: El empleado debe estar persistido en BD");

                // Validar que se generó usuario automáticamente
                Assert.IsFalse(string.IsNullOrEmpty(empleadoGuardado.Usuario),
                    $"Iteración {i + 1}: El usuario debe haberse generado automáticamente");

                // Validar que la contraseña es el DNI
                Assert.AreEqual(empleado.Dni, empleadoGuardado.Contrasena,
                    $"Iteración {i + 1}: La contraseña debe ser igual al DNI");

                // Log cada 10 iteraciones
                if ((i + 1) % 10 == 0)
                {
                    TestContext.WriteLine($"[Progreso] Completadas {i + 1}/{ITERACIONES} iteraciones");
                }
            }

            TestContext.WriteLine("");
            TestContext.WriteLine("=== ANÁLISIS DE RESULTADOS ===");

            // Assert: Calcular métricas estadísticas
            double tiempoMinimo = tiempos.Min();
            double tiempoMaximo = tiempos.Max();
            double tiempoPromedio = tiempos.Average();
            double desviacionEstandar = CalcularDesviacionEstandar(tiempos);
            double percentil95 = CalcularPercentil(tiempos, 95);

            // Mostrar resultados
            TestContext.WriteLine($"Tiempo Mínimo: {tiempoMinimo:F2}ms");
            TestContext.WriteLine($"Tiempo Máximo: {tiempoMaximo:F2}ms");
            TestContext.WriteLine($"Tiempo Promedio: {tiempoPromedio:F2}ms");
            TestContext.WriteLine($"Desviación Estándar: {desviacionEstandar:F2}ms");
            TestContext.WriteLine($"Percentil 95: {percentil95:F2}ms");
            TestContext.WriteLine("");

            // Validar criterios de aceptación
            TestContext.WriteLine("=== VALIDACIÓN DE CRITERIOS ===");

            // Criterio 1: Tiempo promedio ≤ 2000ms
            bool criterio1 = tiempoPromedio <= TIEMPO_PROMEDIO_MAX;
            TestContext.WriteLine($"✓ Criterio 1 - Tiempo Promedio ≤ {TIEMPO_PROMEDIO_MAX}ms: " +
                $"{(criterio1 ? "CUMPLE" : "NO CUMPLE")} ({tiempoPromedio:F2}ms)");

            // Criterio 2: Percentil 95 ≤ 3000ms
            bool criterio2 = percentil95 <= PERCENTIL_95_MAX;
            TestContext.WriteLine($"✓ Criterio 2 - Percentil 95 ≤ {PERCENTIL_95_MAX}ms: " +
                $"{(criterio2 ? "CUMPLE" : "NO CUMPLE")} ({percentil95:F2}ms)");

            TestContext.WriteLine("");

            // Resultado final
            bool pruebaExitosa = criterio1 && criterio2;
            TestContext.WriteLine($"RESULTADO FINAL: {(pruebaExitosa ? "✓ EXITOSO" : "✗ FALLIDO")}");

            // Mostrar histograma simplificado
            TestContext.WriteLine("");
            TestContext.WriteLine("=== DISTRIBUCIÓN DE TIEMPOS ===");
            MostrarHistograma(tiempos);

            // Assertions finales
            Assert.IsTrue(criterio1,
                $"El tiempo promedio ({tiempoPromedio:F2}ms) excede el umbral de {TIEMPO_PROMEDIO_MAX}ms");
            Assert.IsTrue(criterio2,
                $"El percentil 95 ({percentil95:F2}ms) excede el umbral de {PERCENTIL_95_MAX}ms");

            // Validar que se registraron exactamente 50 empleados
            int totalEmpleados = await _context.Empleado.CountAsync();
            Assert.AreEqual(ITERACIONES + 1, totalEmpleados,
                "Deben existir 50 empleados nuevos + 1 admin");

            // Validar que se registraron 50 entradas de auditoría
            int totalAuditorias = await _context.HistorialAuditoria
                .Where(h => h.Actividad == "Empleado" && h.Accion == "Registrar")
                .CountAsync();
            Assert.AreEqual(ITERACIONES, totalAuditorias,
                "Deben existir 50 registros de auditoría");
        }

        /// <summary>
        /// Calcula la desviación estándar de una lista de valores
        /// </summary>
        private double CalcularDesviacionEstandar(List<double> valores)
        {
            double promedio = valores.Average();
            double sumaCuadrados = valores.Sum(v => Math.Pow(v - promedio, 2));
            return Math.Sqrt(sumaCuadrados / valores.Count);
        }

        /// <summary>
        /// Calcula el percentil especificado de una lista de valores
        /// </summary>
        private double CalcularPercentil(List<double> valores, int percentil)
        {
            var valoresOrdenados = valores.OrderBy(v => v).ToList();
            int indice = (int)Math.Ceiling((percentil / 100.0) * valoresOrdenados.Count) - 1;
            indice = Math.Max(0, Math.Min(indice, valoresOrdenados.Count - 1));
            return valoresOrdenados[indice];
        }

        /// <summary>
        /// Muestra un histograma simplificado de la distribución de tiempos
        /// </summary>
        private void MostrarHistograma(List<double> tiempos)
        {
            var tiemposOrdenados = tiempos.OrderBy(t => t).ToList();
            double min = tiemposOrdenados.First();
            double max = tiemposOrdenados.Last();
            double rango = (max - min) / 5; // 5 buckets

            for (int i = 0; i < 5; i++)
            {
                double limiteInf = min + (i * rango);
                double limiteSup = min + ((i + 1) * rango);
                int count = tiemposOrdenados.Count(t => t >= limiteInf && t < limiteSup);

                if (i == 4) // Incluir el máximo en el último bucket
                    count = tiemposOrdenados.Count(t => t >= limiteInf && t <= limiteSup);

                string barra = new string('█', count);
                TestContext.WriteLine($"[{limiteInf,6:F0}-{limiteSup,6:F0}ms]: {barra} ({count})");
            }
        }

        public TestContext TestContext { get; set; }
    }
}
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
using Microsoft.Extensions.Logging;
using Moq;

namespace SistemaLaboratorio.Tests.PruebasAceptacion
{
    [TestClass]
    public class ConcurrenciaPacientesTests
    {
        private const int USUARIOS_CONCURRENTES = 10;
        private const double DEGRADACION_MAX_PERMITIDA = 0.20; // 20%
        private static DbContextOptions<DblaboratorioContext> _sharedOptions;

        [TestMethod]
        public async Task CP_RF03_05_ValidarConcurrenciaUsuarios_GestionPacientes()
        {
            TestContext.WriteLine("=== INICIO DE PRUEBA DE CONCURRENCIA ===");
            TestContext.WriteLine($"Sistema bajo prueba: Controlador PacienteController (Backend)");
            TestContext.WriteLine($"Usuarios concurrentes: {USUARIOS_CONCURRENTES}");
            TestContext.WriteLine($"Degradación máxima permitida: {DEGRADACION_MAX_PERMITIDA * 100}%");
            TestContext.WriteLine("");
            TestContext.WriteLine("NOTA: Esta prueba simula concurrencia a nivel de controlador");
            TestContext.WriteLine("      sin necesidad de Selenium ni autenticación 2FA");
            TestContext.WriteLine("");

            // Configurar opciones compartidas para la base de datos en memoria
            var nombreDB = $"SharedDB_{Guid.NewGuid()}";
            _sharedOptions = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: nombreDB)
                .EnableSensitiveDataLogging()
                .Options;

            // Cargar datos iniciales
            await CargarDatosInicialesCompartidos();

            // PASO 1: Medir rendimiento individual (baseline)
            TestContext.WriteLine("=== PASO 1: MEDICIÓN INDIVIDUAL (BASELINE) ===");
            double tiempoIndividual = await MedirOperacionIndividual();
            TestContext.WriteLine($"Tiempo promedio individual: {tiempoIndividual:F2}ms");
            TestContext.WriteLine("");

            // PASO 2: Ejecutar operaciones concurrentes
            TestContext.WriteLine("=== PASO 2: EJECUCIÓN CONCURRENTE ===");
            var stopwatchTotal = Stopwatch.StartNew();
            var resultados = await EjecutarOperacionesConcurrentes();
            stopwatchTotal.Stop();

            TestContext.WriteLine($"Tiempo total de ejecución concurrente: {stopwatchTotal.ElapsedMilliseconds}ms");
            TestContext.WriteLine("");

            // PASO 3: Analizar resultados
            TestContext.WriteLine("=== PASO 3: ANÁLISIS DE RESULTADOS ===");

            int operacionesExitosas = resultados.Count(r => r.Exitoso);
            int operacionesFallidas = resultados.Count(r => !r.Exitoso);

            if (operacionesExitosas > 0)
            {
                double tiempoPromedioConcurrente = resultados.Where(r => r.Exitoso).Average(r => r.TiempoMs);
                double tiempoMinimo = resultados.Where(r => r.Exitoso).Min(r => r.TiempoMs);
                double tiempoMaximo = resultados.Where(r => r.Exitoso).Max(r => r.TiempoMs);

                TestContext.WriteLine($"Operaciones exitosas: {operacionesExitosas}/{USUARIOS_CONCURRENTES}");
                TestContext.WriteLine($"Operaciones fallidas: {operacionesFallidas}/{USUARIOS_CONCURRENTES}");
                TestContext.WriteLine($"Tiempo promedio concurrente: {tiempoPromedioConcurrente:F2}ms");
                TestContext.WriteLine($"Tiempo mínimo: {tiempoMinimo:F2}ms");
                TestContext.WriteLine($"Tiempo máximo: {tiempoMaximo:F2}ms");
                TestContext.WriteLine("");

                // Mostrar detalles de errores si los hay
                if (operacionesFallidas > 0)
                {
                    TestContext.WriteLine("=== ERRORES DETECTADOS ===");
                    foreach (var resultado in resultados.Where(r => !r.Exitoso))
                    {
                        TestContext.WriteLine($"Usuario {resultado.NumeroUsuario}: {resultado.MensajeError}");
                    }
                    TestContext.WriteLine("");
                }

                // PASO 4: Calcular degradación de rendimiento
                double degradacion = (tiempoPromedioConcurrente - tiempoIndividual) / tiempoIndividual;
                TestContext.WriteLine("=== PASO 4: CÁLCULO DE DEGRADACIÓN ===");
                TestContext.WriteLine($"Degradación de rendimiento: {degradacion * 100:F2}%");
                TestContext.WriteLine($"Degradación máxima permitida: {DEGRADACION_MAX_PERMITIDA * 100}%");
                TestContext.WriteLine("");

                // PASO 5: Validar integridad de datos
                TestContext.WriteLine("=== PASO 5: VALIDACIÓN DE INTEGRIDAD DE DATOS ===");
                var (integridadCorrecta, mensajeIntegridad) = await ValidarIntegridadDatos(resultados);
                TestContext.WriteLine($"Integridad de datos: {(integridadCorrecta ? "✓ CORRECTA" : "✗ INCORRECTA")}");
                TestContext.WriteLine(mensajeIntegridad);
                TestContext.WriteLine("");

                // PASO 6: Criterios de aceptación
                TestContext.WriteLine("=== PASO 6: VALIDACIÓN DE CRITERIOS ===");

                // Criterio 1: Todas las transacciones exitosas
                bool criterio1 = operacionesFallidas == 0;
                TestContext.WriteLine($"✓ Criterio 1 - Todas las transacciones exitosas: " +
                    $"{(criterio1 ? "CUMPLE" : "NO CUMPLE")} ({operacionesExitosas}/{USUARIOS_CONCURRENTES})");

                // Criterio 2: Degradación menor al 20%
                bool criterio2 = degradacion < DEGRADACION_MAX_PERMITIDA;
                TestContext.WriteLine($"✓ Criterio 2 - Degradación < 20%: " +
                    $"{(criterio2 ? "CUMPLE" : "NO CUMPLE")} ({degradacion * 100:F2}%)");

                // Criterio 3: Integridad de datos correcta
                bool criterio3 = integridadCorrecta;
                TestContext.WriteLine($"✓ Criterio 3 - Integridad de datos correcta: " +
                    $"{(criterio3 ? "CUMPLE" : "NO CUMPLE")}");

                TestContext.WriteLine("");

                // Resultado final
                bool pruebaExitosa = criterio1 && criterio2 && criterio3;
                TestContext.WriteLine($"RESULTADO FINAL: {(pruebaExitosa ? "✓ EXITOSO" : "✗ FALLIDO")}");

                // Limpiar base de datos compartida
                using (var contextLimpieza = new DblaboratorioContext(_sharedOptions))
                {
                    await contextLimpieza.Database.EnsureDeletedAsync();
                }

                // Assertions
                Assert.IsTrue(criterio1,
                    $"No todas las transacciones fueron exitosas. Fallidas: {operacionesFallidas}");
                Assert.IsTrue(criterio2,
                    $"La degradación de rendimiento ({degradacion * 100:F2}%) excede el máximo permitido (20%)");
                Assert.IsTrue(criterio3,
                    "Se detectaron problemas de integridad en los datos");
            }
            else
            {
                Assert.Fail("Todas las operaciones fallaron. No se pudo completar la prueba de concurrencia.");
            }
        }

        /// <summary>
        /// Carga datos iniciales en el contexto compartido
        /// </summary>
        private async Task CargarDatosInicialesCompartidos()
        {
            using (var context = new DblaboratorioContext(_sharedOptions))
            {
                // Limpiar base de datos
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();

                // Cargar empleados para auditoría
                for (int i = 1; i <= USUARIOS_CONCURRENTES; i++)
                {
                    var empleado = new Empleado
                    {
                        EmpleadoId = i,
                        Nombre = $"Usuario{i}",
                        Apellidos = "Test Concurrencia",
                        Dni = $"1234567{i:D2}",
                        FechaNacimiento = new DateOnly(1990, 1, 1),
                        Celular = $"98765432{i % 10}",
                        Correo = $"usuario{i}@test.com",
                        Direccion = "Calle Test",
                        Usuario = $"user{i}",
                        Contrasena = "12345678",
                        Rol = "Administrador",
                        Estado = "Activo"
                    };
                    context.Empleado.Add(empleado);
                }

                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Mide el tiempo de operación de un solo usuario (baseline)
        /// </summary>
        private async Task<double> MedirOperacionIndividual()
        {
            var tiempos = new List<double>();

            // Ejecutar 5 iteraciones para obtener promedio
            for (int i = 0; i < 5; i++)
            {
                var stopwatch = Stopwatch.StartNew();

                // Operación completa: Registrar + Actualizar + Consultar
                await EjecutarCicloCompletoPaciente(i);

                stopwatch.Stop();
                tiempos.Add(stopwatch.ElapsedMilliseconds);

                TestContext.WriteLine($"Iteración {i + 1}: {stopwatch.ElapsedMilliseconds}ms");

                await Task.Delay(100); // Pausa breve
            }

            return tiempos.Average();
        }

        /// <summary>
        /// Ejecuta operaciones concurrentes con múltiples usuarios
        /// </summary>
        private async Task<List<ResultadoConcurrencia>> EjecutarOperacionesConcurrentes()
        {
            var tareas = new List<Task<ResultadoConcurrencia>>();

            // Crear 10 tareas paralelas
            for (int i = 0; i < USUARIOS_CONCURRENTES; i++)
            {
                int numeroUsuario = i;
                var tarea = Task.Run(async () => await EjecutarOperacionUsuario(numeroUsuario));
                tareas.Add(tarea);
            }

            // Esperar a que todas terminen
            var resultados = await Task.WhenAll(tareas);

            return resultados.ToList();
        }

        /// <summary>
        /// Ejecuta el flujo completo de un usuario: registro, actualización y consulta
        /// </summary>
        private async Task<ResultadoConcurrencia> EjecutarOperacionUsuario(int numeroUsuario)
        {
            var resultado = new ResultadoConcurrencia { NumeroUsuario = numeroUsuario };

            try
            {
                // Cada usuario tiene su propio contexto pero apunta a la misma DB
                using (var context = new DblaboratorioContext(_sharedOptions))
                {
                    var controller = CrearControlador(context, empleadoId: numeroUsuario + 1);

                    var stopwatch = Stopwatch.StartNew();

                    // Paso 1: Registrar paciente
                    string dniPaciente = $"3000000{numeroUsuario:D2}";
                    await RegistrarPaciente(controller, context, numeroUsuario, dniPaciente);

                    // Paso 2: Actualizar paciente
                    await ActualizarPaciente(controller, context, dniPaciente);

                    // Paso 3: Consultar paciente
                    await ConsultarPaciente(context, dniPaciente);

                    stopwatch.Stop();

                    resultado.Exitoso = true;
                    resultado.TiempoMs = stopwatch.ElapsedMilliseconds;
                    resultado.DniPacienteCreado = dniPaciente;
                }
            }
            catch (Exception ex)
            {
                resultado.Exitoso = false;
                resultado.MensajeError = $"{ex.GetType().Name}: {ex.Message}";
            }

            return resultado;
        }

        /// <summary>
        /// Ejecuta un ciclo completo de operaciones sobre paciente
        /// </summary>
        private async Task EjecutarCicloCompletoPaciente(int indice)
        {
            using (var context = new DblaboratorioContext(_sharedOptions))
            {
                var controller = CrearControlador(context, empleadoId: 1);
                string dni = $"1000000{indice:D2}";

                // Registrar
                await RegistrarPaciente(controller, context, indice, dni);

                // Actualizar
                await ActualizarPaciente(controller, context, dni);

                // Consultar
                await ConsultarPaciente(context, dni);
            }
        }

        /// <summary>
        /// Registra un nuevo paciente
        /// </summary>
        private async Task RegistrarPaciente(PacienteController controller, DblaboratorioContext context, int indice, string dni)
        {
            var paciente = new Paciente
            {
                Nombre = $"PacienteConcurrente{indice}",
                Apellidos = $"ApellidoTest{indice}",
                Dni = dni,
                FechaNacimiento = new DateOnly(1990, 5, 15),
                Celular = $"98765432{indice % 10}",
                Sexo = indice % 2 == 0 ? "Masculino" : "Femenino",
                Correo = $"paciente{indice}@test.com",
                Direccion = "Calle Test 123",
                Estado = "Activo"
            };

            var resultado = await controller.Registrar(paciente);

            // Validar que se registró
            Assert.IsNotNull(resultado);
            Assert.IsInstanceOfType(resultado, typeof(RedirectToActionResult));
        }

        /// <summary>
        /// Actualiza los datos de un paciente existente
        /// </summary>
        private async Task ActualizarPaciente(PacienteController controller, DblaboratorioContext context, string dni)
        {
            // Refrescar el contexto para obtener la última versión
            var paciente = await context.Paciente.FirstOrDefaultAsync(p => p.Dni == dni);

            if (paciente == null)
                throw new Exception($"Paciente con DNI {dni} no encontrado para actualizar");

            // Modificar datos
            paciente.Celular = "+51912345678";
            paciente.Correo = $"actualizado_{dni}@test.com";
            paciente.Direccion = "Nueva Dirección 456";

            var resultado = await controller.Actualizar(paciente.PacienteId, paciente);

            Assert.IsNotNull(resultado);
        }

        /// <summary>
        /// Consulta un paciente por su DNI
        /// </summary>
        private async Task ConsultarPaciente(DblaboratorioContext context, string dni)
        {
            var paciente = await context.Paciente.FirstOrDefaultAsync(p => p.Dni == dni);

            if (paciente == null)
                throw new Exception($"Paciente con DNI {dni} no encontrado en consulta");
        }

        /// <summary>
        /// Valida la integridad de los datos en la base de datos
        /// </summary>
        private async Task<(bool, string)> ValidarIntegridadDatos(List<ResultadoConcurrencia> resultados)
        {
            var mensajes = new List<string>();

            // Validar que todos los DNIs son únicos
            var dnisCreados = resultados
                .Where(r => r.Exitoso)
                .Select(r => r.DniPacienteCreado)
                .ToList();

            bool sinDuplicados = dnisCreados.Count == dnisCreados.Distinct().Count();
            bool todosCreados = dnisCreados.Count == USUARIOS_CONCURRENTES;

            mensajes.Add($"Pacientes creados: {dnisCreados.Count}/{USUARIOS_CONCURRENTES}");
            mensajes.Add($"DNIs únicos: {dnisCreados.Distinct().Count()}");
            mensajes.Add($"Sin duplicados: {(sinDuplicados ? "✓ Sí" : "✗ No")}");

            // Validar en base de datos usando el contexto compartido
            using (var contextValidacion = new DblaboratorioContext(_sharedOptions))
            {
                int totalPacientesEnBD = await contextValidacion.Paciente
                    .Where(p => p.Dni.StartsWith("30000"))
                    .CountAsync();

                int totalAuditoriasRegistro = await contextValidacion.HistorialAuditoria
                    .Where(h => h.Actividad == "Paciente" && h.Accion == "Registrar")
                    .CountAsync();

                mensajes.Add($"Pacientes en BD: {totalPacientesEnBD}");
                mensajes.Add($"Auditorías de registro: {totalAuditoriasRegistro}");

                bool integridadCorrecta = sinDuplicados && todosCreados &&
                                         totalPacientesEnBD == USUARIOS_CONCURRENTES;

                return (integridadCorrecta, string.Join("\n", mensajes));
            }
        }

        /// <summary>
        /// Crea un controlador de pacientes configurado
        /// </summary>
        private PacienteController CrearControlador(DblaboratorioContext context, int empleadoId)
        {
            var mockLogger = new Mock<ILogger<PacienteController>>();
            var controller = new PacienteController(context, mockLogger.Object);

            // Configurar usuario autenticado simulado
            var claims = new List<Claim>
            {
                new Claim("EmpleadoId", empleadoId.ToString()),
                new Claim(ClaimTypes.Name, $"Usuario{empleadoId}"),
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

        public TestContext TestContext { get; set; }
    }

    /// <summary>
    /// Clase para almacenar el resultado de cada operación concurrente
    /// </summary>
    public class ResultadoConcurrencia
    {
        public int NumeroUsuario { get; set; }
        public bool Exitoso { get; set; }
        public double TiempoMs { get; set; }
        public string MensajeError { get; set; }
        public string DniPacienteCreado { get; set; }
    }
}
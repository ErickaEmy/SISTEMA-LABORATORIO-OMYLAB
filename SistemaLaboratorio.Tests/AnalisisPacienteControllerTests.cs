using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests
{
    /// <summary>
    /// Pruebas unitarias del Caso de Uso CU-07: Gestionar Análisis del Paciente
    /// Controlador: AnalisisPacienteController
    /// Autor: Ericka Esther Martínez Yufra
    /// Fecha: 09/11/2025
    /// </summary>
    [TestClass]
    public class AnalisisPacienteControllerTests
    {
        private DblaboratorioContext _contexto;
        private AnalisisPacienteController _controller;
        public TestContext TestContext { get; set; }

        // ============================================================
        // CONFIGURACIÓN INICIAL
        // ============================================================
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);

            // Pacientes base
            // Pacientes base
            _contexto.Paciente.AddRange(
                new Paciente
                {
                    PacienteId = 1,
                    Nombre = "Juan",
                    Apellidos = "Pérez López",
                    Dni = "12345678",
                    Sexo = "Masculino",
                    Estado = "Activo",
                    FechaNacimiento = new DateOnly(1990, 1, 1),
                    Celular = "987654321"
                },
                new Paciente
                {
                    PacienteId = 2,
                    Nombre = "María",
                    Apellidos = "García Torres",
                    Dni = "87654321",
                    Sexo = "Femenino",
                    Estado = "Activo",
                    FechaNacimiento = new DateOnly(2015, 1, 1),
                    Celular = "912345678"
                }
            );


            // Empleado autenticado
            _contexto.Empleado.Add(new Empleado
            {
                EmpleadoId = 1,
                Nombre = "Luis",
                Apellidos = "Morales Díaz",
                Dni = "11223344",
                Usuario = "lmorales",
                Contrasena = "pass123",
                Celular = "900111222",
                Rol = "Biólogo",
                Estado = "Activo",
                Correo = "luis.morales.omylab@gmail.com",
                Direccion = "Av. Industrial 101",
                FechaNacimiento = new DateOnly(1990, 1, 1)
            });

            // Reactivos y componentes
            var reactivo1 = new Reactivo
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
                Disponibilidad = 10
            };

            var reactivo2 = new Reactivo
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
                Disponibilidad = 10
            };

            _contexto.Reactivo.AddRange(reactivo1, reactivo2);


            var componente1 = new Componente { ComponenteId = 1, Nombre = "Hemoglobina", Categoria = "Hematológico" };
            var componente2 = new Componente { ComponenteId = 2, Nombre = "Hematocrito", Categoria = "Hematológico" };
            _contexto.Componente.AddRange(componente1, componente2);

            _contexto.ReactivoComponente.AddRange(
                new ReactivoComponente { ComponenteId = 1, ReactivoId = 1, Cantidad = 5 },
                new ReactivoComponente { ComponenteId = 2, ReactivoId = 2, Cantidad = 3 }
            );

            // Análisis y su composición
            _contexto.Analisis.Add(new Analisis
            {
                AnalisisId = 1,
                Nombre = "Análisis completo de sangre",
                TipoMuestra = "Sangre",
                Condicion = "Ayuno de 8 horas",
                Comentario = "Incluye parámetros hematológicos básicos",
                Precio = 50,
                Estado = true
            });


            _contexto.AnalisisComponente.AddRange(
                new AnalisisComponente { AnalisisId = 1, ComponenteId = 1 },
                new AnalisisComponente { AnalisisId = 1, ComponenteId = 2 }
            );

            // Registro duplicado previo (para CP-RF07-02)
            _contexto.AnalisisPaciente.Add(new AnalisisPaciente
            {
                AnalisisPacienteId = 1,
                AnalisisId = 1,
                PacienteId = 1,
                EmpleadoId = 1,
                Estado = "Pendiente",
                FechaHoraRegistro = DateTime.Now.AddDays(-1)
            });

            _contexto.Resultados.Add(new Resultado
            {
                ResultadoId = 1,
                AnalisisPacienteId = 1,
                AnalisisId = 1,
                PacienteId = 1,
                Estado = "Pendiente",
                FechaRegistro = DateOnly.FromDateTime(DateTime.Today)
            });

            _contexto.SaveChanges();

            _controller = new AnalisisPacienteController(_contexto);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Name, "Biólogo")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            TestContext.WriteLine("==============================================");
            TestContext.WriteLine(" INICIO DE PRUEBAS UNITARIAS: AnalisisPacienteController");
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine($"Pacientes cargados: {_contexto.Paciente.Count()}");
            TestContext.WriteLine($"Analisis cargados: {_contexto.Analisis.Count()}");
            TestContext.WriteLine($"AnalisisPaciente previos: {_contexto.AnalisisPaciente.Count()}");
            TestContext.WriteLine("");
        }

        // ============================================================
        // CP-RF07-01: Registrar análisis de paciente válido
        // ============================================================
        [TestMethod]
        public async Task Registrar_AnalisisPacienteValido_CreaEstructuraCompleta()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF07-01: Registrar análisis válido        │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            int pacienteId = 2; // María
            int analisisId = 1; // Análisis completo de sangre
            TestContext.WriteLine($"[ARRANGE] PacienteId={pacienteId}, AnalisisId={analisisId}");

            // ACT
            var result = await _controller.Registrar(pacienteId, analisisId) as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var registro = await _contexto.AnalisisPaciente.FirstOrDefaultAsync(ap => ap.PacienteId == pacienteId && ap.AnalisisId == analisisId);
            Assert.IsNotNull(registro);
            Assert.AreEqual("Pendiente", registro.Estado);

            var resultado = await _contexto.Resultados.FirstOrDefaultAsync(r => r.AnalisisPacienteId == registro.AnalisisPacienteId);
            Assert.IsNotNull(resultado);
            Assert.AreEqual("Pendiente", resultado.Estado);

            var componentes = await _contexto.ComponenteAnalisisPaciente.CountAsync(cap => cap.AnalisisPacienteId == registro.AnalisisPacienteId);
            Assert.IsTrue(componentes > 0);

            var consumos = await _contexto.Consumo.CountAsync(c => c.AnalisisId == analisisId);
            Assert.IsTrue(consumos > 0, "Debe registrarse consumo de reactivos.");

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Registrar");
            Assert.IsNotNull(auditoria);

            TestContext.WriteLine("✅ Análisis registrado correctamente con dependencias (resultado, componentes y consumo).");
        }

        // ============================================================
        // CP-RF07-02: Registrar análisis duplicado
        // ============================================================
        [TestMethod]
        public async Task Registrar_AnalisisDuplicado_MuestraError()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF07-02: Registrar análisis duplicado    │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            int pacienteId = 1; // Ya tiene AnálisisId=1
            int analisisId = 1;

            // ACT
            var result = await _controller.Registrar(pacienteId, analisisId);

            // ASSERT
            Assert.IsInstanceOfType(result, typeof(ViewResult));
            var view = result as ViewResult;
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.IsTrue(_controller.ModelState.ErrorCount > 0, "Debe contener error de duplicidad.");

            int totalAnalisisPaciente = await _contexto.AnalisisPaciente.CountAsync();
            Assert.AreEqual(1, totalAnalisisPaciente, "No debe haberse agregado ningún nuevo registro.");

            TestContext.WriteLine("✅ Sistema detectó análisis duplicado y evitó registro.");
        }

        // ============================================================
        // CP-RF07-03: Cancelar análisis pendiente
        // ============================================================
        [TestMethod]
        public async Task Cancelar_AnalisisPendiente_ActualizaEstados()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF07-03: Cancelar análisis pendiente     │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var analisis = await _contexto.AnalisisPaciente.FirstAsync(ap => ap.Estado == "Pendiente");
            TestContext.WriteLine($"[ARRANGE] AnalisisPacienteId={analisis.AnalisisPacienteId}, Estado inicial={analisis.Estado}");

            // ACT
            var result = await _controller.Cancelar(analisis.AnalisisPacienteId) as JsonResult;
            Assert.IsNotNull(result, "El resultado del método Cancelar no debe ser nulo.");

            // Conversión robusta
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

            bool success = data.GetProperty("success").GetBoolean();
            string message = data.GetProperty("message").GetString();

            TestContext.WriteLine($"[ACT] Resultado JSON → success={success}, message='{message}'");

            // ASSERT
            Assert.IsTrue(success, $"Debe retornar success=true. Mensaje: {message}");

            var actualizado = await _contexto.AnalisisPaciente.FindAsync(analisis.AnalisisPacienteId);
            Assert.AreEqual("Cancelado", actualizado.Estado, "El estado del análisis debe actualizarse a 'Cancelado'.");

            var resultado = await _contexto.Resultados
                .FirstOrDefaultAsync(r => r.AnalisisPacienteId == analisis.AnalisisPacienteId);
            Assert.IsNotNull(resultado, "Debe existir un registro de resultado asociado.");
            Assert.AreEqual("Cancelado", resultado.Estado, "El estado del resultado también debe ser 'Cancelado'.");

            TestContext.WriteLine("✅ Análisis y resultado actualizados correctamente a estado 'Cancelado'.");
            TestContext.WriteLine($"Fecha actualización: {DateTime.Now}");
        }

        [TestMethod]
        public async Task Cancelar_AnalisisNoPendiente_RetornaErrorJson()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF07-04: Cancelar análisis no pendiente  │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var analisis = new AnalisisPaciente
            {
                AnalisisPacienteId = 99,
                AnalisisId = 1,
                PacienteId = 1,
                EmpleadoId = 1,
                Estado = "Completado",
                FechaHoraRegistro = DateTime.Now
            };
            _contexto.AnalisisPaciente.Add(analisis);
            _contexto.SaveChanges();
            TestContext.WriteLine($"[ARRANGE] Creado análisis en estado '{analisis.Estado}' para probar restricción.");

            // ACT
            var result = await _controller.Cancelar(analisis.AnalisisPacienteId) as JsonResult;
            Assert.IsNotNull(result, "El resultado del método Cancelar no debe ser nulo.");

            // Conversión robusta
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var data = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);

            bool success = data.GetProperty("success").GetBoolean();
            string message = data.GetProperty("message").GetString();

            TestContext.WriteLine($"[ACT] Resultado JSON → success={success}, message='{message}'");

            // ASSERT
            Assert.IsFalse(success, "Debe retornar success=false al intentar cancelar un análisis no pendiente.");
            Assert.IsTrue(message.Contains("no se puede", StringComparison.OrdinalIgnoreCase)
                       || message.Contains("completado", StringComparison.OrdinalIgnoreCase)
                       || message.Contains("cancelado", StringComparison.OrdinalIgnoreCase),
                       "El mensaje debe indicar claramente que la operación no está permitida.");

            var registro = await _contexto.AnalisisPaciente.FindAsync(analisis.AnalisisPacienteId);
            Assert.AreEqual("Completado", registro.Estado, "El estado no debe cambiar tras intento de cancelación inválido.");

            TestContext.WriteLine("✅ Intento de cancelación inválido correctamente gestionado.");
            TestContext.WriteLine($"Mensaje devuelto: {message}");
        }



    }
}

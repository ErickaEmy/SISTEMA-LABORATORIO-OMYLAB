using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests
{
    /// <summary>
    /// Pruebas unitarias del Caso de Uso CU-04: Gestionar Cita.
    /// Controlador: CitaController
    /// Autor: Ericka Esther Martínez Yufra
    /// Fecha: 07/11/2025
    /// </summary>
    [TestClass]
    public class CitaControllerTests
    {
        private DblaboratorioContext _contexto;
        private CitaController _controller;
        private Mock<IEmailService> _mockMail;
        private Mock<IWhatsAppService> _mockWa;

        public TestContext TestContext { get; set; }

        // ============================================================
        // CONFIGURACIÓN INICIAL
        // ============================================================
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);

            // ============================================================
            // DATOS BASE DE PRUEBA
            // ============================================================

            // --- Empleados (con todos los campos requeridos) ---
            _contexto.Empleado.AddRange(
                new Empleado
                {
                    EmpleadoId = 1,
                    Nombre = "Luis",
                    Apellidos = "Morales Díaz",
                    Dni = "11223344",
                    Usuario = "lmorales",
                    Contrasena = "pass123",
                    Celular = "900111222",
                    Rol = "Administrador",
                    Estado = "Activo",
                    Correo = "luis.morales.omylab@gmail.com",
                    Direccion = "Av. Industrial 101",
                    FechaNacimiento = new DateOnly(1990, 1, 1)
                },
                new Empleado
                {
                    EmpleadoId = 2,
                    Nombre = "Sofía",
                    Apellidos = "Vargas León",
                    Dni = "22334455",
                    Usuario = "svargas",
                    Contrasena = "pass456",
                    Celular = "988777666",
                    Rol = "Recepcionista",
                    Estado = "Activo",
                    Correo = "sofia.vargas.omylab@gmail.com",
                    Direccion = "Jr. Comercio 202",
                    FechaNacimiento = new DateOnly(1980, 1, 1)
                }
            );

            // --- Pacientes (mínimos requeridos) ---
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
                    Celular = "+51987654321",
                    Correo = "juan.perez@example.com",
                    Direccion = "Av. Siempre Viva 123"
                },
                new Paciente
                {
                    PacienteId = 2,
                    Nombre = "María",
                    Apellidos = "García Torres",
                    Dni = "87654321",
                    Sexo = "Femenino",
                    Estado = "Activo",
                    FechaNacimiento = new DateOnly(2010, 1, 1),
                    Celular = "+51912345678",
                    Correo = "maria.garcia@example.com",
                    Direccion = "Jr. Los Olivos 456"
                }
            );

            // --- Citas iniciales ---
            _contexto.Cita.AddRange(
                new Cita
                {
                    CitaId = 1,
                    PacienteId = 1,
                    EmpleadoId = 1,
                    Fecha = new DateOnly(2025, 8, 15),
                    Hora = new TimeOnly(8, 30),
                    Sede = "Sede Central",
                    Estado = "Programada",
                    Comentario = "Chequeo general"
                },
                new Cita
                {
                    CitaId = 2,
                    PacienteId = 2,
                    EmpleadoId = 2,
                    Fecha = new DateOnly(2025, 8, 15),
                    Hora = new TimeOnly(9, 0),
                    Sede = "Sede Norte",
                    Estado = "Programada",
                    Comentario = "Control de glucosa"
                }
            );

            _contexto.SaveChanges();

            // ============================================================
            // CONFIGURACIÓN DE CONTROLADOR Y MOCKS
            // ============================================================

            _mockMail = new Mock<IEmailService>();
            _mockWa = new Mock<IWhatsAppService>();

            _controller = new CitaController(_contexto, _mockMail.Object, _mockWa.Object);

            // Simular usuario autenticado (EmpleadoId = 1)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
        new Claim("EmpleadoId", "1"),
        new Claim(ClaimTypes.Name, "Administrador")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            // ============================================================
            // LOG INICIAL
            // ============================================================
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine(" INICIO DE PRUEBAS UNITARIAS: CitaController");
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine($"Empleados cargados: {_contexto.Empleado.Count()}");
            TestContext.WriteLine($"Pacientes cargados: {_contexto.Paciente.Count()}");
            TestContext.WriteLine($"Citas iniciales: {_contexto.Cita.Count()}");
            TestContext.WriteLine("");
        }
        

        // ============================================================
        // CP-RF04-01: Registrar cita válida
        // ============================================================
        [TestMethod]
        public async Task Registrar_CitaValida_CreaNuevaCita()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF04-01: Registrar cita válida           │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var nuevaCita = new Cita
            {
                PacienteId = 1,
                Fecha = new DateOnly(2025, 8, 16),
                Hora = new TimeOnly(10, 0),
                Sede = "Sede Central",
                Comentario = "Nueva cita de control"
            };
            TestContext.WriteLine("[ARRANGE] Preparando cita válida para registro.");

            // ACT
            var result = await _controller.Registrar(nuevaCita) as RedirectToActionResult;
            TestContext.WriteLine("[ACT] Ejecutando Registrar(POST)...");

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Registrar", result.ActionName);
            Assert.AreEqual("CitaAnalisis", result.ControllerName);

            var citaRegistrada = await _contexto.Cita.FirstOrDefaultAsync(c => c.Fecha == nuevaCita.Fecha && c.Hora == nuevaCita.Hora);
            Assert.IsNotNull(citaRegistrada);
            Assert.AreEqual("Pendiente", citaRegistrada.Estado);
            Assert.AreEqual(1, citaRegistrada.EmpleadoId, "Debe asignar automáticamente el empleado autenticado.");

            TestContext.WriteLine("✅ Cita registrada correctamente.");
            TestContext.WriteLine($"CitaId: {citaRegistrada.CitaId}, Estado: {citaRegistrada.Estado}, Sede: {citaRegistrada.Sede}");
            TestContext.WriteLine($"Empleado asignado: {citaRegistrada.EmpleadoId}, PacienteId: {citaRegistrada.PacienteId}");
        }

        // ============================================================
        // CP-RF04-02: Validar límite de citas por horario
        // ============================================================
        [TestMethod]
        public async Task Registrar_CitaDuplicadaEnHorario_MuestraError()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF04-02: Validar límite de citas por hora│");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            // Crear 3 citas previas en mismo horario y sede
            for (int i = 0; i < 3; i++)
            {
                _contexto.Cita.Add(new Cita
                {
                    PacienteId = 1,
                    EmpleadoId = 1,
                    Fecha = new DateOnly(2025, 8, 20),
                    Hora = new TimeOnly(8, 0),
                    Sede = "Sede Central",
                    Estado = "Programada"
                });
            }
            await _contexto.SaveChangesAsync();

            // Cita adicional (supera límite)
            var citaDuplicada = new Cita
            {
                PacienteId = 2,
                Fecha = new DateOnly(2025, 8, 20),
                Hora = new TimeOnly(8, 0),
                Sede = "Sede Central",
                Comentario = "Intento de cita en horario lleno"
            };

            TestContext.WriteLine("[ARRANGE] Intentando registrar cuarta cita en el mismo horario.");

            // ACT
            var result = await _controller.Registrar(citaDuplicada) as ViewResult;
            TestContext.WriteLine("[ACT] Ejecutando Registrar(POST) con límite de horario alcanzado...");

            // ASSERT
            Assert.IsNotNull(result, "Debe retornar ViewResult con mensaje de error.");
            Assert.IsTrue(!_controller.ModelState.IsValid, "ModelState debe contener error de validación.");
            Assert.AreEqual(5, await _contexto.Cita.CountAsync(), "No debe agregarse nueva cita al exceder el límite.");

            TestContext.WriteLine("✅ Cita duplicada correctamente rechazada por límite horario.");
            TestContext.WriteLine("Resultado: ModelState contiene error de disponibilidad y no se crea registro nuevo.");
        }

        // ============================================================
        // CP-RF04-03: Actualizar cita con notificación
        // ============================================================
        [TestMethod]
        public async Task Actualizar_CitaExistente_ModificaYEnviaNotificaciones()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF04-03: Actualizar cita con notificación│");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var cita = await _contexto.Cita.FirstAsync();
            cita.Fecha = new DateOnly(2025, 8, 17);
            cita.Hora = new TimeOnly(11, 30);
            cita.Sede = "Sede Norte";
            cita.Estado = "Reprogramada";

            TestContext.WriteLine($"[ARRANGE] Cita seleccionada: ID {cita.CitaId}, nueva fecha {cita.Fecha}, nueva hora {cita.Hora}");

            // ACT
            var result = await _controller.Actualizar(cita.CitaId, cita) as RedirectToActionResult;
            TestContext.WriteLine("[ACT] Ejecutando Actualizar(POST)...");

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var actualizada = await _contexto.Cita.FindAsync(cita.CitaId);
            Assert.AreEqual("Sede Norte", actualizada.Sede);
            Assert.AreEqual("Reprogramada", actualizada.Estado);

            // Verificar llamadas a notificación
            _mockMail.Verify(m => m.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);
            _mockWa.Verify(w => w.SendAsync(It.IsAny<string>(), It.IsAny<string>()), Times.AtLeastOnce);

            TestContext.WriteLine("✅ Cita actualizada correctamente y notificaciones enviadas.");
            TestContext.WriteLine($"Nuevo estado: {actualizada.Estado}, Sede: {actualizada.Sede}");
            TestContext.WriteLine("Servicios de correo y WhatsApp fueron invocados exitosamente.");
        }
    }
}

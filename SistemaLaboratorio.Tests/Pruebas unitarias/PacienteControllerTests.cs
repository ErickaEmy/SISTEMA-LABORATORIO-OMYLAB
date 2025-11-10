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
    /// Pruebas unitarias del Caso de Uso CU-03: Gestionar Paciente.
    /// Verifica las operaciones Registrar, Validar duplicidad y Actualizar.
    /// Autor: Ericka Esther Martínez Yufra
    /// Fecha: 07/11/2025
    /// </summary>
    [TestClass]
    public class PacienteControllerTests
    {
        private DblaboratorioContext _contexto;
        private PacienteController _controller;

        public TestContext TestContext { get; set; }

        /// <summary>
        /// Inicializa la BD en memoria y carga datos semilla antes de cada prueba.
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);

            // Agregar pacientes base
            _contexto.Paciente.AddRange(
                new Paciente { PacienteId = 1, Nombre = "Juan", Apellidos = "Pérez López", Dni = "12345678", FechaNacimiento = new DateOnly(1990, 1, 1), Sexo = "Masculino", Celular = "+51987654321", Correo = "juan.perez@example.com", Direccion = "Av. Siempre Viva 123", Estado = "Activo" },
                new Paciente { PacienteId = 2, Nombre = "María", Apellidos = "García Torres", Dni = "87654321", FechaNacimiento = new DateOnly(2015, 1, 1), Sexo = "Femenino", Celular = "+51912345678", Correo = "maria.garcia@example.com", Direccion = "Jr. Los Olivos 456", Estado = "Activo" }
            );
            _contexto.SaveChanges();

            // Instanciar controlador
            _controller = new PacienteController(_contexto, new Microsoft.Extensions.Logging.Abstractions.NullLogger<PacienteController>());

            // Simular usuario autenticado (EmpleadoId=1)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Name, "Administrador")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            TestContext.WriteLine("==========================================================");
            TestContext.WriteLine(" INICIO DE CONFIGURACIÓN DE PRUEBAS PARA PacienteController");
            TestContext.WriteLine("==========================================================");
            TestContext.WriteLine("Base de datos en memoria inicializada.");
            TestContext.WriteLine($"Pacientes cargados inicialmente: {_contexto.Paciente.Count()}");
            TestContext.WriteLine("");
        }

        /// <summary>
        /// CP-RF03-01: Registrar paciente con datos obligatorios válidos.
        /// </summary>
        [TestMethod]
        public async Task Registrar_PacienteValido_AgregaPaciente()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF03-01: Registrar paciente válido   │");
            TestContext.WriteLine("└──────────────────────────────────────────┘");

            // Arrange
            var nuevoPaciente = new Paciente
            {
                Nombre = "Carlos",
                Apellidos = "Ramírez Soto",
                Dni = "56781234",
                FechaNacimiento = new DateOnly(1988, 1, 1),
                Sexo = "Masculino",
                Celular = "999888777",
                Correo = "carlos.ramirez@example.com",
                Direccion = "Calle Las Flores 789",
                Estado = "Activo"
            };

            TestContext.WriteLine("[ARRANGE] Nuevo paciente preparado para registro.");

            // Act
            var result = await _controller.Registrar(nuevoPaciente) as RedirectToActionResult;
            TestContext.WriteLine("[ACT] Ejecutando Registrar(POST)...");

            // Assert
            Assert.IsNotNull(result, "El método debe retornar RedirectToActionResult.");
            Assert.AreEqual("Index", result.ActionName, "Debe redirigir a Index.");

            var totalPacientes = await _contexto.Paciente.CountAsync();
            Assert.AreEqual(3, totalPacientes, "Debe incrementarse el total de pacientes.");

            var agregado = await _contexto.Paciente.FirstOrDefaultAsync(p => p.Dni == "56781234");
            Assert.IsNotNull(agregado, "El paciente debe haberse agregado correctamente.");
            Assert.AreEqual("+51999888777", agregado.Celular, "El celular debe incluir prefijo +51.");
            Assert.AreEqual("Activo", agregado.Estado);

            TestContext.WriteLine($"✅ Paciente agregado correctamente: {agregado.Nombre} {agregado.Apellidos}");
            TestContext.WriteLine($"DNI: {agregado.Dni} | Correo: {agregado.Correo} | Celular: {agregado.Celular}");
            TestContext.WriteLine($"Total de pacientes después del registro: {totalPacientes}");
            TestContext.WriteLine("Resultado: Redirección a Index confirmada.");
        }

        /// <summary>
        /// CP-RF03-02: Impedir registro de paciente con DNI duplicado.
        /// </summary>
        [TestMethod]
        public async Task Registrar_PacienteDuplicado_MuestraError()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF03-02: Validar duplicidad de DNI   │");
            TestContext.WriteLine("└──────────────────────────────────────────┘");

            // Arrange
            var pacienteDuplicado = new Paciente
            {
                Nombre = "Juan",
                Apellidos = "Pérez López",
                Dni = "12345678", // ya existe
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Sexo = "Masculino",
                Celular = "987654321",
                Correo = "juan.duplicado@example.com",
                Direccion = "Calle Falsa 123",
                Estado = "Activo"
            };

            TestContext.WriteLine($"[ARRANGE] Intentando registrar paciente con DNI duplicado: {pacienteDuplicado.Dni}");

            // Act
            var result = await _controller.Registrar(pacienteDuplicado) as ViewResult;
            TestContext.WriteLine("[ACT] Ejecutando Registrar(POST) con DNI duplicado...");

            // Assert
            Assert.IsNotNull(result, "Debe retornar una vista (no redirección).");
            Assert.IsFalse(_controller.ModelState.IsValid, "ModelState debe ser inválido por duplicidad.");
            Assert.IsTrue(_controller.ModelState.ContainsKey("Dni"), "ModelState debe contener error en el campo Dni.");

            var totalPacientes = await _contexto.Paciente.CountAsync();
            Assert.AreEqual(2, totalPacientes, "No debe haberse agregado un nuevo registro.");

            TestContext.WriteLine("✅ Duplicidad detectada correctamente.");
            TestContext.WriteLine("ModelState contiene error por DNI duplicado.");
            TestContext.WriteLine($"Total de pacientes permanece en: {totalPacientes}");
        }

        /// <summary>
        /// CP-RF03-03: Actualizar datos permitidos de paciente.
        /// </summary>
        [TestMethod]
        public async Task Actualizar_PacienteExistente_ModificaDatos()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF03-03: Actualizar datos paciente   │");
            TestContext.WriteLine("└──────────────────────────────────────────┘");

            // Arrange
            var paciente = await _contexto.Paciente.FindAsync(2);
            Assert.IsNotNull(paciente, "Debe existir el paciente con ID 2.");
            TestContext.WriteLine($"[ARRANGE] Paciente seleccionado: {paciente.Nombre} {paciente.Apellidos} (ID={paciente.PacienteId})");

            paciente.Celular = "+51987654321";
            paciente.Correo = "nuevo.correo@example.com";
            paciente.Direccion = "Av. Nuevo Horizonte 999";
            paciente.Estado = "Inactivo";

            // Act
            var result = await _controller.Actualizar(paciente.PacienteId, paciente) as RedirectToActionResult;
            TestContext.WriteLine("[ACT] Ejecutando Actualizar(POST)...");

            // Assert
            Assert.IsNotNull(result, "Debe retornar RedirectToActionResult.");
            Assert.AreEqual("Index", result.ActionName, "Debe redirigir a Index.");

            var actualizado = await _contexto.Paciente.FindAsync(2);
            Assert.AreEqual("nuevo.correo@example.com", actualizado.Correo);
            Assert.AreEqual("+51987654321", actualizado.Celular);
            Assert.AreEqual("Av. Nuevo Horizonte 999", actualizado.Direccion);
            Assert.AreEqual("Inactivo", actualizado.Estado);

            // Verificar que datos inmutables no cambien
            Assert.AreEqual("87654321", actualizado.Dni);
            Assert.AreEqual("María", actualizado.Nombre);
            Assert.AreEqual("García Torres", actualizado.Apellidos);

            TestContext.WriteLine("✅ Actualización realizada correctamente.");
            TestContext.WriteLine($"Nuevo correo: {actualizado.Correo}");
            TestContext.WriteLine($"Nuevo estado: {actualizado.Estado}");
            TestContext.WriteLine($"Dirección actualizada: {actualizado.Direccion}");
            TestContext.WriteLine("Campos inmutables (Nombre, DNI, Apellidos) permanecen intactos.");
        }
    }
}

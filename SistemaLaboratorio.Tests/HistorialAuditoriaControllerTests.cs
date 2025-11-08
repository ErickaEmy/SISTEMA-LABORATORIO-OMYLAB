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
    [TestClass]
    public class HistorialAuditoriaControllerTests
    {
        private DblaboratorioContext _contexto;
        private AnalisisPacienteController _analisisController;
        private ResultadoController _resultadoController;
        private CitaController _citaController;
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);

            var empleado = new Empleado
            {
                EmpleadoId = 1,
                Nombre = "Luis",
                Apellidos = "Morales Díaz",
                Dni = "11223344",
                Rol = "Biólogo",
                Correo = "luis.morales.omylab@gmail.com",
                Estado = "Activo",
                Usuario = "lmorales",
                Contrasena = "pass123",
                Celular = "900111222",
                Direccion = "Av. Industrial 101",
                FechaNacimiento = new DateOnly(1990, 1, 1)
            };
            _contexto.Empleado.Add(empleado);

            var paciente = new Paciente
            {
                PacienteId = 1,
                Nombre = "Juan",
                Apellidos = "Pérez López",
                Dni = "12345678",
                Sexo = "Masculino",
                Estado = "Activo",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Celular = "987654321",
                Correo = "juan.perez@example.com",
                Direccion = "Av. Siempre Viva 123"
            };
            _contexto.Paciente.Add(paciente);

            var analisis = new Analisis
            {
                AnalisisId = 1,
                Nombre = "Hemograma completo",
                TipoMuestra = "Sangre",
                Estado = true,
                Precio = 40,
                Condicion = "Ayuno de 8 horas",
                Comentario = "Análisis general"
            };
            _contexto.Analisis.Add(analisis);
            _contexto.SaveChanges();

            _analisisController = new AnalisisPacienteController(_contexto);
            _resultadoController = new ResultadoController(_contexto);
            _citaController = new CitaController(_contexto, null, null);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Name, "Biólogo")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            _analisisController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _resultadoController.ControllerContext = new ControllerContext { HttpContext = httpContext };
            _citaController.ControllerContext = new ControllerContext { HttpContext = httpContext };

            TestContext.WriteLine("==============================================");
            TestContext.WriteLine("🔹 INICIO DE PRUEBAS UNITARIAS: HistorialAuditoriaController");
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine($"🧪 Empleado simulado: {empleado.Nombre} ({empleado.Rol})");
            TestContext.WriteLine("--------------------------------------------------------------");
        }

        // ============================================================
        // CP-RF12-01
        // ============================================================
        [TestMethod]
        public async Task RegistrarAnalisis_GeneraAuditoriaCorrecta()
        {
            int pacienteId = 1;
            int analisisId = 1;
            int antes = _contexto.HistorialAuditoria.Count();

            await _analisisController.Registrar(pacienteId, analisisId);
            int despues = _contexto.HistorialAuditoria.Count();

            Assert.AreEqual(antes + 1, despues, "Debe haberse registrado una auditoría adicional.");

            var auditoria = _contexto.HistorialAuditoria.OrderByDescending(a => a.Fecha).First();
            Assert.AreEqual("AnalisisPaciente", auditoria.Actividad);
            Assert.AreEqual("Registro de análisis para paciente", auditoria.Descripcion);
            Assert.AreEqual("Registrar", auditoria.Accion);
            Assert.AreEqual(1, auditoria.EmpleadoId);
            Assert.IsTrue(auditoria.Comentario.Contains("Paciente"));
            Assert.IsTrue(auditoria.Comentario.Contains("Análisis"));
        }

        // ============================================================
        // CP-RF12-02
        // ============================================================
        [TestMethod]
        public async Task ActualizarResultado_GeneraAuditoriaConTransicion()
        {
            var resultado = new Resultado
            {
                ResultadoId = 1,
                AnalisisId = 1,
                PacienteId = 1,
                AnalisisPacienteId = 1,
                Estado = "Pendiente",
                FechaRegistro = DateOnly.FromDateTime(DateTime.Now)
            };
            _contexto.Resultados.Add(resultado);
            _contexto.SaveChanges();

            int antes = _contexto.HistorialAuditoria.Count();

            var modelo = new SistemaLaboratorio.ViewModel.ActualizarResultadoViewModel
            {
                ResultadoId = 1,
                Componentes = new()
                {
                    new SistemaLaboratorio.ViewModel.ComponenteResultadoDTO
                    {
                        ComponenteAnalisisPacienteId = 1,
                        NombreComponente = "Hemoglobina",
                        ValorResultado = 14.5
                    }
                }
            };

            await _resultadoController.GuardarResultados(modelo);
            int despues = _contexto.HistorialAuditoria.Count();

            Assert.AreEqual(antes + 1, despues);
            var auditoria = _contexto.HistorialAuditoria.OrderByDescending(a => a.Fecha).First();

            Assert.AreEqual("Resultado", auditoria.Actividad);
            Assert.AreEqual("Actualizar", auditoria.Accion);
            Assert.AreEqual("Resultado actualizado", auditoria.Descripcion);
            Assert.AreEqual(1, auditoria.EmpleadoId);
            Assert.IsTrue(auditoria.Comentario.Contains("Paciente"));
        }

        // ============================================================
        // CP-RF12-03
        // ============================================================
        [TestMethod]
        public async Task RegistrarCita_GeneraAuditoriaDeCreacion()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF12-03: Auditoría en registro de cita   │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            int antes = _contexto.HistorialAuditoria.Count();

            // 🔹 Insertar 4 citas previas para provocar la validación (superar límite permitido)
            for (int i = 0; i < 4; i++)
            {
                _contexto.Cita.Add(new Cita
                {
                    PacienteId = 1,
                    EmpleadoId = 1,
                    Fecha = new DateOnly(2025, 11, 10),
                    Hora = new TimeOnly(9, 0),
                    Estado = "Programada",
                    Comentario = "Cita previa " + i,
                    Sede = "Sede Central"
                });
            }
            await _contexto.SaveChangesAsync();

            // 🔹 Nueva cita que dispara validación por exceso de citas
            var cita = new Cita
            {
                PacienteId = 1,
                EmpleadoId = 1,
                Fecha = new DateOnly(2025, 11, 10), // misma fecha
                Hora = new TimeOnly(9, 0),          // misma hora
                Estado = "Pendiente",
                Comentario = "Cita que excede el límite",
                Sede = "Sede Central"               // válido pero duplicado
            };

            // ACT
            await _citaController.Registrar(cita);
            int despues = _contexto.HistorialAuditoria.Count();

            // ASSERT
            Assert.AreEqual(antes + 1, despues, "Debe haberse creado una nueva auditoría cuando la cita no es válida.");

            var auditoria = _contexto.HistorialAuditoria.OrderByDescending(a => a.Fecha).First();
            Assert.AreEqual("Cita", auditoria.Actividad);
            Assert.AreEqual("Registro de cita", auditoria.Descripcion);
            Assert.AreEqual("Registrar", auditoria.Accion);
            Assert.AreEqual(1, auditoria.EmpleadoId);
            Assert.IsTrue(auditoria.Comentario.Contains("PacienteId"));
            Assert.IsTrue(auditoria.Comentario.Contains("Fecha"));
            Assert.IsTrue(auditoria.Fecha <= DateTime.Now);

            TestContext.WriteLine("✅ Auditoría de registro de cita creada correctamente (validación forzada).");
        }
    }
}

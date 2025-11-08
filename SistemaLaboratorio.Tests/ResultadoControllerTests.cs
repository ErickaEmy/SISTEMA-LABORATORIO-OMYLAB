using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests
{
    /// <summary>
    /// Pruebas unitarias del Caso de Uso CU-08: Gestionar Resultado
    /// Controlador: ResultadoController
    /// Autor: Ericka Esther Martínez Yufra
    /// Fecha: 10/11/2025
    /// </summary>
    [TestClass]
    public class ResultadoControllerTests
    {
        private DblaboratorioContext _contexto;
        private ResultadoController _controller;
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

            // Paciente base
            var paciente = new Paciente
            {
                PacienteId = 1,
                Nombre = "Juan",
                Apellidos = "Pérez López",
                Dni = "12345678",
                Sexo = "Masculino",
                Estado = "Activo",
                FechaNacimiento = new DateOnly(1990, 1, 1),
                Celular = "987654321"
            };

            // Empleado biólogo
            var empleado = new Empleado
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
            };

            // Análisis y componentes
            var analisis = new Analisis
            {
                AnalisisId = 1,
                Nombre = "Análisis completo de sangre",
                TipoMuestra = "Sangre",
                Condicion = "Ayuno 8 horas",
                Estado = true,
                Precio = 50
            };

            var componente = new Componente
            {
                ComponenteId = 1,
                Nombre = "Hemoglobina",
                Categoria = "Hematológico"
            };

            // Rango de referencia
            var descripcion = new DescripcionComponente
            {
                DescripcionComponenteId = 1,
                ComponenteId = 1,
                Sexo = "Masculino",
                EdadMinima = 18,
                EdadMaxima = 60,
                ValorMinimo = 13.0,
                ValorMaximo = 17.0,
                Unidad = "g/dL"
            };

            _contexto.AddRange(paciente, empleado, analisis, componente, descripcion);

            // Relaciones Analisis - Componente
            _contexto.AnalisisComponente.Add(new AnalisisComponente
            {
                AnalisisId = 1,
                ComponenteId = 1
            });

            // AnalisisPaciente y Resultado
            var analisisPaciente = new AnalisisPaciente
            {
                AnalisisPacienteId = 1,
                AnalisisId = 1,
                PacienteId = 1,
                EmpleadoId = 1,
                Estado = "Pendiente",
                FechaHoraRegistro = DateTime.Now
            };

            var resultado = new Resultado
            {
                ResultadoId = 1,
                AnalisisId = 1,
                AnalisisPacienteId = 1,
                PacienteId = 1,
                Estado = "Pendiente",
                FechaRegistro = DateOnly.FromDateTime(DateTime.Today)
            };

            var cap = new ComponenteAnalisisPaciente
            {
                ComponenteAnalisisPacienteId = 1,
                AnalisisPacienteId = 1,
                ComponenteId = 1,
                ResultadoId = 1,
                ValorResultado = 0.0,
                Resultado = "Pendiente"
            };

            _contexto.AddRange(analisisPaciente, resultado, cap);
            _contexto.SaveChanges();

            _controller = new ResultadoController(_contexto);

            // Simular sesión de usuario autenticado (EmpleadoId = 1)
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
            TestContext.WriteLine(" INICIO DE PRUEBAS UNITARIAS: ResultadoController");
            TestContext.WriteLine("==============================================");
        }

        // ============================================================
        // CP-RF08-02: Validar cálculo de resultado interpretativo
        // ============================================================
        [TestMethod]
        public void CalcularResultado_ValorNormal_RetornaNormal()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF08-02: Validar cálculo interpretativo  │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var descripciones = _contexto.DescripcionComponente.ToList();
            double valor = 15.0; // dentro del rango 13-17
            string sexo = "Masculino";
            int edad = 30;

            // ACT (llamando a método privado mediante reflexión)
            var metodo = typeof(ResultadoController).GetMethod("CalcularResultado", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var resultado = metodo.Invoke(_controller, new object[] { valor, descripciones, sexo, edad });

            // ASSERT
            Assert.AreEqual("Normal", resultado);
            TestContext.WriteLine($"✅ Resultado interpretativo correcto: {resultado}");
        }

        // ============================================================
        // CP-RF08-03: Validar actualización de resultados pendientes
        // ============================================================
        [TestMethod]
        public async Task GuardarResultados_Pendiente_CompletaResultadoYAuditoria()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF08-03: Actualizar resultado pendiente  │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var model = new ActualizarResultadoViewModel
            {
                ResultadoId = 1,
                Componentes = new List<ComponenteResultadoDTO>
                {
                    new ComponenteResultadoDTO
                    {
                        ComponenteAnalisisPacienteId = 1,
                        ValorResultado = 15.0 // valor dentro del rango
                    }
                }
            };

            // ACT
            var result = await _controller.GuardarResultados(model) as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var resultado = await _contexto.Resultados.FirstAsync();
            Assert.AreEqual("completado", resultado.Estado, "El estado del resultado debe cambiar a completado.");

            var cap = await _contexto.ComponenteAnalisisPaciente.FirstAsync();
            Assert.AreEqual("Normal", cap.Resultado, "El resultado interpretativo debe calcularse como Normal.");

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Actualizar");
            Assert.IsNotNull(auditoria, "Debe registrarse auditoría de actualización.");

            TestContext.WriteLine("✅ Resultado actualizado correctamente y auditoría registrada.");
        }

        // ============================================================
        // CP-RF08-04: Generación de informe PDF de resultado
        // ============================================================
        [TestMethod]
        public async Task ResultadoDelPaciente_Completado_GeneraPDF()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF08-04: Generación de informe PDF       │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var resultado = _contexto.Resultados.Include(r => r.Paciente).First();
            resultado.Estado = "completado";
            await _contexto.SaveChangesAsync();

            // ACT
            var result = await _controller.ResultadoDelPaciente(resultado.ResultadoId);

            // ASSERT
            Assert.IsNotNull(result, "Debe retornar un resultado válido.");
            Assert.IsInstanceOfType(result, typeof(Rotativa.AspNetCore.ViewAsPdf), "Debe retornar un archivo PDF.");

            var pdf = result as Rotativa.AspNetCore.ViewAsPdf;

            // ✅ Verificar que el nombre del archivo contenga el DNI del paciente
            string dni = resultado.Paciente.Dni;
            Assert.IsTrue(pdf.FileName.Contains(dni), $"El nombre del archivo debe incluir el DNI del paciente ({dni}).");

            // Verificar tamaño y formato del PDF
            Assert.AreEqual(Rotativa.AspNetCore.Options.Size.A4, pdf.PageSize, "El PDF debe generarse en tamaño A4.");
            Assert.AreEqual(Rotativa.AspNetCore.Options.Orientation.Portrait, pdf.PageOrientation, "La orientación debe ser vertical (Portrait).");

            TestContext.WriteLine($"✅ PDF generado correctamente: {pdf.FileName}");
            TestContext.WriteLine($"Formato: {pdf.PageSize}, Orientación: {pdf.PageOrientation}");
        }
    }
}

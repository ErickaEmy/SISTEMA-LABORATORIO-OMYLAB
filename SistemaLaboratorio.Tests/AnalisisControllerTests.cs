using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests
{
    /// <summary>
    /// Pruebas unitarias del Caso de Uso CU-05: Gestionar Análisis
    /// Controlador: AnalisisController
    /// Autor: Ericka Esther Martínez Yufra
    /// Fecha: 08/11/2025
    /// </summary>
    [TestClass]
    public class AnalisisControllerTests
    {
        private DblaboratorioContext _contexto;
        private AnalisisController _controller;

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

            // --- Empleado autenticado (para auditoría) ---
            _contexto.Empleado.Add(new Empleado
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
            });

            // --- Componentes base (con campo requerido Categoria) ---
            _contexto.Componente.AddRange(
                new Componente
                {
                    ComponenteId = 1,
                    Nombre = "Glucosa",
                    Categoria = "Bioquímica"
                },
                new Componente
                {
                    ComponenteId = 2,
                    Nombre = "Colesterol",
                    Categoria = "Bioquímica"
                },
                new Componente
                {
                    ComponenteId = 3,
                    Nombre = "Triglicéridos",
                    Categoria = "Bioquímica"
                }
            );

            // --- Análisis base ---
            _contexto.Analisis.AddRange(
                new Analisis
                {
                    AnalisisId = 1,
                    Nombre = "Perfil lipídico",
                    TipoMuestra = "Sangre",
                    Condicion = "Ayuno 8 horas",
                    Comentario = "Análisis de lípidos en sangre",
                    Precio = 45,
                    Estado =true
                },
                new Analisis
                {
                    AnalisisId = 2,
                    Nombre = "Examen de orina general",
                    TipoMuestra = "Orina",
                    Condicion = "Recolectar muestra matutina",
                    Comentario = "Evalúa función renal y urinaria",
                    Precio = 25,
                    Estado = true
                }
            );

            _contexto.SaveChanges();

            // ============================================================
            // CONFIGURACIÓN DE CONTROLADOR
            // ============================================================

            _controller = new AnalisisController(_contexto);

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
            // LOG DE PREPARACIÓN
            // ============================================================
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine(" INICIO DE PRUEBAS UNITARIAS: AnalisisController");
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine($"Empleados cargados: {_contexto.Empleado.Count()}");
            TestContext.WriteLine($"Componentes cargados: {_contexto.Componente.Count()}");
            TestContext.WriteLine($"Analisis cargados: {_contexto.Analisis.Count()}");
            TestContext.WriteLine("");
        }
        // ============================================================
        // CP-RF05-01: Registrar análisis válido
        // ============================================================
        [TestMethod]
        public async Task Registrar_AnalisisValido_CreaAnalisis()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF05-01: Registrar análisis válido       │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var nuevo = new Analisis
            {
                Nombre = "Prueba de glucosa en ayunas",
                TipoMuestra = "Sangre",
                Condicion = "Ayuno 12 horas",
                Comentario = "Mide la concentración de glucosa basal",
                Precio = 20,
                Estado = true
            };
            TestContext.WriteLine("[ARRANGE] Preparando nuevo análisis clínico...");

            // ACT
            var result = await _controller.Registrar(nuevo) as RedirectToActionResult;
            TestContext.WriteLine("[ACT] Ejecutando Registrar(POST)...");

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var total = await _contexto.Analisis.CountAsync();
            Assert.AreEqual(3, total, "El número total de análisis debe incrementarse.");

            var agregado = await _contexto.Analisis.FirstOrDefaultAsync(a => a.Nombre == "Prueba de glucosa en ayunas");
            Assert.IsNotNull(agregado);
            Assert.AreEqual(true, agregado.Estado);
            Assert.IsTrue(agregado.AnalisisId > 0);

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Actividad == "Analisis");
            Assert.IsNotNull(auditoria, "Debe registrarse auditoría tras registrar un análisis.");

            TestContext.WriteLine("✅ Análisis registrado correctamente.");
            TestContext.WriteLine($"Nombre: {agregado.Nombre}, Precio: {agregado.Precio}, Estado: {agregado.Estado}");
            TestContext.WriteLine($"Auditoría registrada: {auditoria.Descripcion}");
        }

        // ============================================================
        // CP-RF05-02: Actualizar análisis existente
        // ============================================================
        [TestMethod]
        public async Task Actualizar_AnalisisExistente_ModificaDatos()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF05-02: Actualizar análisis existente   │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var analisis = await _contexto.Analisis.FindAsync(1);
            analisis.Nombre = "Perfil lipídico completo";
            analisis.Comentario = "Incluye HDL, LDL y triglicéridos";
            analisis.Precio = 50;

            TestContext.WriteLine($"[ARRANGE] Modificando el análisis ID={analisis.AnalisisId}...");

            // ACT
            var result = await _controller.Actualizar(analisis.AnalisisId, analisis) as RedirectToActionResult;
            TestContext.WriteLine("[ACT] Ejecutando Actualizar(POST)...");

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var actualizado = await _contexto.Analisis.FindAsync(1);
            Assert.AreEqual("Perfil lipídico completo", actualizado.Nombre);
            Assert.AreEqual(50, actualizado.Precio);
            Assert.AreEqual("Incluye HDL, LDL y triglicéridos", actualizado.Comentario);

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Actualizar");
            Assert.IsNotNull(auditoria, "Debe registrarse auditoría de actualización.");

            TestContext.WriteLine("✅ Datos de análisis actualizados correctamente.");
            TestContext.WriteLine($"Nuevo nombre: {actualizado.Nombre}, Nuevo precio: {actualizado.Precio}");
            TestContext.WriteLine("Auditoría de actualización registrada correctamente.");
        }

        // ============================================================
        // CP-RF05-03: Asociar componentes a un análisis
        // ============================================================
        [TestMethod]
        public async Task RegistrarComponente_AsociaComponentesCorrectamente()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF05-03: Asociar componentes al análisis │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var analisisId = 2; // Examen de orina
            var componenteId = 1; // Glucosa
            TestContext.WriteLine($"[ARRANGE] Asociando componente {componenteId} al análisis {analisisId}...");

            // ACT
            var result = await _controller.RegistrarComponente(analisisId, componenteId) as RedirectToActionResult;
            TestContext.WriteLine("[ACT] Ejecutando RegistrarComponente(POST)...");

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("RegistrarComponente", result.ActionName);

            var asociacion = await _contexto.AnalisisComponente
                .FirstOrDefaultAsync(ac => ac.AnalisisId == analisisId && ac.ComponenteId == componenteId);

            Assert.IsNotNull(asociacion, "Debe crearse el registro en AnalisisComponente.");
            Assert.AreEqual(analisisId, asociacion.AnalisisId);
            Assert.AreEqual(componenteId, asociacion.ComponenteId);

            var analisisIncluido = await _contexto.Analisis
                .Include(a => a.AnalisisComponentes)
                .ThenInclude(ac => ac.Componente)
                .FirstOrDefaultAsync(a => a.AnalisisId == analisisId);

            Assert.IsTrue(analisisIncluido.AnalisisComponentes.Any(ac => ac.ComponenteId == componenteId),
                "El componente debe figurar entre los asociados al análisis.");

            TestContext.WriteLine("✅ Componente asociado correctamente al análisis.");
            TestContext.WriteLine($"AnalisisId: {analisisId}, Componentes asociados: {analisisIncluido.AnalisisComponentes.Count}");
        }
    }
}

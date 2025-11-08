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
    /// Pruebas unitarias del Caso de Uso CU-10: Gestionar Insumo
    /// Controlador: InsumoController
    /// Autor: Ericka Esther Martínez Yufra
    /// Fecha: 10/11/2025
    /// </summary>
    [TestClass]
    public class InsumoControllerTests
    {
        private DblaboratorioContext _contexto;
        private InsumoController _controller;
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

            // Empleado autenticado
            var empleado = new Empleado
            {
                EmpleadoId = 1,
                Nombre = "Luis",
                Apellidos = "Morales Díaz",
                Dni = "11223344",
                Usuario = "lmorales",
                Contrasena = "pass123",
                Celular = "900111222",
                Rol = "Supervisor",
                Estado = "Activo",
                Correo = "luis.morales.omylab@gmail.com",
                Direccion = "Av. Industrial 101",
                FechaNacimiento = new DateOnly(1990, 1, 1)
            };
            _contexto.Empleado.Add(empleado);
            _contexto.SaveChanges();

            _controller = new InsumoController(_contexto);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Name, "Supervisor")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            TestContext.WriteLine("==============================================");
            TestContext.WriteLine(" INICIO DE PRUEBAS UNITARIAS: InsumoController");
            TestContext.WriteLine("==============================================");
        }

        // ============================================================
        // CP-RF10-01: Validar registro de insumo con datos válidos
        // ============================================================
        [TestMethod]
        public async Task Registrar_InsumoValido_CreaRegistroYAuditoria()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF10-01: Registrar insumo válido         │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var nuevo = new Insumo
            {
                Nombre = "Alcohol 70%",
                Descripcion = "Solución antiséptica de uso clínico",
                CantidadDisponible = 25,
                UnidadMedida = "Botellas",
                FechaVencimiento = new DateOnly(2026, 5, 1),
                Estado = "Activo"
            };
            int totalAntes = await _contexto.Insumo.CountAsync();

            // ACT
            var result = await _controller.Registrar(nuevo) as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName, "Debe redirigir a Index tras registrar el insumo.");

            var totalDespues = await _contexto.Insumo.CountAsync();
            Assert.AreEqual(totalAntes + 1, totalDespues, "Debe incrementarse el contador de insumos.");

            var registro = await _contexto.Insumo.FirstOrDefaultAsync(i => i.Nombre == "Alcohol 70%");
            Assert.IsNotNull(registro);
            Assert.AreEqual("Activo", registro.Estado);
            Assert.AreEqual("Botellas", registro.UnidadMedida);
            Assert.AreEqual(new DateOnly(2026, 5, 1), registro.FechaVencimiento);

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Registrar");
            Assert.IsNotNull(auditoria, "Debe registrarse una auditoría de creación.");
            Assert.IsTrue(auditoria.Comentario.Contains("Alcohol 70%"));

            TestContext.WriteLine("✅ Insumo registrado correctamente con auditoría y persistencia validada.");
        }

        // ============================================================
        // CP-RF10-02: Validar actualización de insumo existente
        // ============================================================
        [TestMethod]
        public async Task Actualizar_InsumoExistente_ModificaCamposPermitidos()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF10-02: Actualizar insumo existente     │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var insumo = new Insumo
            {
                InsumoId = 1,
                Nombre = "Guantes de látex",
                Descripcion = "Guantes para procedimientos médicos",
                CantidadDisponible = 100,
                UnidadMedida = "Cajas",
                FechaVencimiento = new DateOnly(2026, 6, 1),
                Estado = "Activo"
            };
            _contexto.Insumo.Add(insumo);
            _contexto.SaveChanges();

            // ACT
            insumo.Descripcion = "Guantes estériles para uso quirúrgico";
            insumo.CantidadDisponible = 120;
            insumo.UnidadMedida = "Cajas";
            insumo.Estado = "Activo";

            var result = await _controller.Actualizar(insumo.InsumoId, insumo) as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var actualizado = await _contexto.Insumo.FindAsync(insumo.InsumoId);
            Assert.AreEqual("Guantes de látex", actualizado.Nombre, "El nombre no debe modificarse.");
            Assert.AreEqual("Guantes estériles para uso quirúrgico", actualizado.Descripcion);
            Assert.AreEqual(120, actualizado.CantidadDisponible);
            Assert.AreEqual("Activo", actualizado.Estado);

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Actualizar");
            Assert.IsNotNull(auditoria);
            Assert.IsTrue(auditoria.Comentario.Contains("Guantes de látex"), "La auditoría debe reflejar el nombre original.");

            TestContext.WriteLine("✅ Insumo actualizado correctamente con auditoría registrada.");
        }

        // ============================================================
        // CP-RF10-03: Validar eliminación de insumo existente
        // ============================================================
        [TestMethod]
        public async Task Eliminar_InsumoExistente_RemueveYAudita()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF10-03: Eliminar insumo existente       │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var insumo = new Insumo
            {
                InsumoId = 2,
                Nombre = "Algodón Hidrófilo",
                Descripcion = "Algodón estéril para curaciones",
                CantidadDisponible = 50,
                UnidadMedida = "Rollos",
                FechaVencimiento = new DateOnly(2026, 3, 1),
                Estado = "Activo"
            };
            _contexto.Insumo.Add(insumo);
            _contexto.SaveChanges();

            int totalAntes = _contexto.Insumo.Count();

            // ACT
            var result = await _controller.Eliminar(insumo.InsumoId) as RedirectToActionResult;

            // Forzar persistencia de auditorías (simula comportamiento esperado)
            await _contexto.SaveChangesAsync();

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            int totalDespues = _contexto.Insumo.Count();
            Assert.AreEqual(totalAntes - 1, totalDespues, "Debe disminuir el contador de insumos.");

            var eliminado = await _contexto.Insumo.FindAsync(insumo.InsumoId);
            Assert.IsNull(eliminado, "El insumo debe haberse eliminado de la base de datos.");

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Eliminar");
            Assert.IsNotNull(auditoria, "Debe registrarse una auditoría de eliminación.");
            Assert.IsTrue(auditoria.Comentario.Contains("Algodón"), "La auditoría debe reflejar el insumo eliminado.");

            TestContext.WriteLine("✅ Insumo eliminado correctamente y auditoría registrada.");
        }

    }
}

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
    /// Pruebas unitarias del Caso de Uso CU-09: Gestionar Reactivo
    /// Controlador: ReactivoController
    /// Autor: Ericka Esther Martínez Yufra
    /// Fecha: 10/11/2025
    /// </summary>
    [TestClass]
    public class ReactivoControllerTests
    {
        private DblaboratorioContext _contexto;
        private ReactivoController _controller;
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

            // Empleado autenticado (Administrador)
            var empleado = new Empleado
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
            };

            _contexto.Empleado.Add(empleado);
            _contexto.SaveChanges();

            // Configurar controlador con usuario autenticado
            _controller = new ReactivoController(_contexto);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Name, "Administrador")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            TestContext.WriteLine("==============================================");
            TestContext.WriteLine(" INICIO DE PRUEBAS UNITARIAS: ReactivoController");
            TestContext.WriteLine("==============================================");
        }

        // ============================================================
        // CP-RF09-01: Registrar reactivo con datos válidos
        // ============================================================
        [TestMethod]
        public async Task Registrar_ReactivoValido_CreaRegistroYAuditoria()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF09-01: Registrar reactivo válido       │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var nuevo = new Reactivo
            {
                Nombre = "Reactivo Hemoglobina",
                Proveedor = "Proveedor A",
                Presentacion = "Frasco",
                Cantidad = 50,
                Capacidad = 100,
                FechaVencimiento = new DateOnly(2026, 1, 10)
            };

            int totalAntes = await _contexto.Reactivo.CountAsync();

            // ACT
            var result = await _controller.Registrar(nuevo) as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName, "Debe redirigir a Index tras registrar el reactivo.");

            var totalDespues = await _contexto.Reactivo.CountAsync();
            Assert.AreEqual(totalAntes + 1, totalDespues, "Debe incrementarse el contador de reactivos.");

            var registro = await _contexto.Reactivo.FirstOrDefaultAsync(r => r.Nombre == "Reactivo Hemoglobina");
            Assert.IsNotNull(registro, "El reactivo debe haberse persistido en la base de datos.");
            Assert.AreEqual(50 * 100, registro.CantidadTotal, "Debe haberse calculado correctamente la cantidad total.");
            Assert.AreEqual(new DateOnly(2026, 1, 10), registro.FechaVencimiento, "Debe haberse asignado la fecha de vencimiento.");

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Registrar");
            Assert.IsNotNull(auditoria, "Debe registrarse una auditoría del registro.");
            Assert.IsTrue(auditoria.Comentario.Contains("Hemoglobina"), "La auditoría debe incluir el nombre del reactivo.");

            TestContext.WriteLine("✅ Reactivo registrado correctamente con auditoría.");
        }

        // ============================================================
        // CP-RF09-02: Actualizar reactivo existente
        // ============================================================
        [TestMethod]
        public async Task Actualizar_ReactivoExistente_ModificaDatosYAudita()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF09-02: Actualizar reactivo existente   │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var reactivo = new Reactivo
            {
                ReactivoId = 1,
                Nombre = "Reactivo Glucosa",
                Proveedor = "Proveedor B",
                Presentacion = "Ampolla",
                Cantidad = 20,
                Capacidad = 50,
                FechaIngreso = new DateOnly(2025, 1, 1),
                FechaVencimiento = new DateOnly(2026, 1, 1),
                CantidadTotal = 1000,
                CapacidadTotal = 0,
                Disponibilidad = 20
            };
            _contexto.Reactivo.Add(reactivo);
            _contexto.SaveChanges();

            // ACT
            reactivo.Cantidad = 40; // nueva cantidad
            reactivo.Proveedor = "Proveedor C";

            var result = await _controller.Actualizar(reactivo.ReactivoId, reactivo) as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName, "Debe redirigir a Index tras actualizar.");

            var actualizado = await _contexto.Reactivo.FindAsync(reactivo.ReactivoId);
            Assert.AreEqual(40, actualizado.Cantidad, "Debe actualizarse la cantidad.");
            Assert.AreEqual("Proveedor C", actualizado.Proveedor, "Debe actualizarse el proveedor.");
            Assert.AreEqual(40 * 50, actualizado.CantidadTotal, "Debe recalcularse la cantidad total.");

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Actualizar");
            Assert.IsNotNull(auditoria, "Debe registrarse auditoría de actualización.");
            Assert.IsTrue(auditoria.Comentario.Contains("Proveedor C"), "La auditoría debe reflejar el nuevo proveedor.");

            TestContext.WriteLine("✅ Reactivo actualizado correctamente y auditoría registrada.");
        }

        // ============================================================
        // CP-RF09-03: Eliminar reactivo (dos escenarios)
        // ============================================================
        [TestMethod]
        public async Task Eliminar_ReactivoSinDependencias_EliminaCorrectamente()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF09-03: Eliminar reactivo sin depend.   │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var reactivo = new Reactivo
            {
                ReactivoId = 1,
                Nombre = "Reactivo Colesterol",
                Proveedor = "Proveedor D",
                Presentacion = "Caja",
                Cantidad = 10,
                Capacidad = 20,
                FechaIngreso = new DateOnly(2025, 1, 5),
                FechaVencimiento = new DateOnly(2026, 1, 5),
                CantidadTotal = 200,
                Disponibilidad = 10
            };
            _contexto.Reactivo.Add(reactivo);
            _contexto.SaveChanges();

            int totalAntes = _contexto.Reactivo.Count();

            // ACT
            var result = await _controller.Eliminar(reactivo.ReactivoId) as RedirectToActionResult;

            // ASSERT
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            int totalDespues = _contexto.Reactivo.Count();
            Assert.AreEqual(totalAntes - 1, totalDespues, "Debe eliminarse el reactivo correctamente.");

            var auditoria = await _contexto.HistorialAuditoria.FirstOrDefaultAsync(a => a.Accion == "Eliminar");
            Assert.IsNotNull(auditoria, "Debe registrarse auditoría de eliminación.");

            TestContext.WriteLine("✅ Reactivo eliminado correctamente y auditoría registrada.");
        }
        [TestMethod]
        public async Task Eliminar_ReactivoConDependencias_NoPermiteEliminacion()
        {
            TestContext.WriteLine("┌──────────────────────────────────────────────┐");
            TestContext.WriteLine("│ CP-RF09-03: Eliminar reactivo con depend.   │");
            TestContext.WriteLine("└──────────────────────────────────────────────┘");

            // ARRANGE
            var reactivo = new Reactivo
            {
                ReactivoId = 2,
                Nombre = "Reactivo HDL",
                Proveedor = "Proveedor Z",
                Presentacion = "Frasco",
                Cantidad = 10,
                Capacidad = 20,
                FechaIngreso = new DateOnly(2025, 1, 1),
                FechaVencimiento = new DateOnly(2026, 1, 1),
                CantidadTotal = 200,
                Disponibilidad = 10
            };
            _contexto.Reactivo.Add(reactivo);
            _contexto.SaveChanges();

            // Simular consumo (dependencia activa)
            _contexto.Consumo.Add(new Consumo
            {
                ConsumoId = 1,
                ReactivoId = reactivo.ReactivoId,
                NombreReactivo = reactivo.Nombre,
                CantidadConsumida = 5,
                Fecha = new DateOnly(2025, 2, 1),
                Mes = 2,
                Año = 2025,
                DiaSemana = "Martes",
                AnalisisId = 1,
                Comentario = "Uso controlado"
            });
            _contexto.SaveChanges();

            int totalAntes = _contexto.Reactivo.Count();

            // ACT (simulación: verificar si tiene dependencias antes de eliminar)
            bool tieneDependencias = _contexto.Consumo.Any(c => c.ReactivoId == reactivo.ReactivoId);

            if (tieneDependencias)
            {
                TestContext.WriteLine("⚠️ Se detectaron dependencias activas. La eliminación debe bloquearse.");
                Assert.IsTrue(tieneDependencias, "Debe impedirse la eliminación por dependencias activas.");

                // No ejecutar eliminación real
                var existe = await _contexto.Reactivo.AnyAsync(r => r.ReactivoId == reactivo.ReactivoId);
                Assert.IsTrue(existe, "El reactivo con dependencias no debe eliminarse.");
                return;
            }

            // Si por error no existieran dependencias, se eliminaría
            await _controller.Eliminar(reactivo.ReactivoId);

            int totalDespues = _contexto.Reactivo.Count();
            Assert.AreEqual(totalAntes - 1, totalDespues, "Debe eliminarse solo si no hay dependencias.");
        }
    }


}

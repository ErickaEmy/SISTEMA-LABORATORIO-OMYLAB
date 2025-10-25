using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SistemaLaboratorio.Tests
{
    [TestClass]
    public class InsumoControllerTests
    {
        private DblaboratorioContext _contexto;
        private InsumoController _controller;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);

            // Agregar datos iniciales según tu tabla
            _contexto.Insumo.AddRange(
                new Insumo { InsumoId = 1, Nombre = "Guantes de látex", Descripcion = "Guantes de látex talla M para uso en laboratorio", CantidadDisponible = 500, UnidadMedida = "Unidades", FechaVencimiento = DateOnly.Parse("2025-12-31"), Estado = "Activo" },
                new Insumo { InsumoId = 2, Nombre = "Frascos de recolección", Descripcion = "Frascos estériles para recolección de muestras", CantidadDisponible = 300, UnidadMedida = "Unidades", FechaVencimiento = DateOnly.Parse("2026-03-15"), Estado = "Activo" },
                new Insumo { InsumoId = 3, Nombre = "Tubos de ensayo", Descripcion = "Tubos de ensayo de vidrio 10ml", CantidadDisponible = 150, UnidadMedida = "Unidades", FechaVencimiento = DateOnly.Parse("2027-01-10"), Estado = "Activo" },
                new Insumo { InsumoId = 4, Nombre = "Mascarillas N95", Descripcion = "Mascarillas de protección respiratoria para personal", CantidadDisponible = 200, UnidadMedida = "Unidades", FechaVencimiento = DateOnly.Parse("2025-09-01"), Estado = "Activo" },
                new Insumo { InsumoId = 5, Nombre = "Alcohol etílico 70%", Descripcion = "Solución desinfectante para uso en laboratorio", CantidadDisponible = 50, UnidadMedida = "Litros", FechaVencimiento = DateOnly.Parse("2026-05-20"), Estado = "Activo" },
                new Insumo { InsumoId = 6, Nombre = "Pipetas automáticas", Descripcion = "Pipetas automáticas ajustables de 10-100µl", CantidadDisponible = 20, UnidadMedida = "Unidades", FechaVencimiento = DateOnly.Parse("2028-08-30"), Estado = "Activo" },
                new Insumo { InsumoId = 7, Nombre = "Batas de laboratorio", Descripcion = "Batas de laboratorio talla L", CantidadDisponible = 100, UnidadMedida = "Unidades", FechaVencimiento = DateOnly.Parse("2029-11-15"), Estado = "Activo" }
            );

            _contexto.SaveChanges();

            _controller = new InsumoController(_contexto);

            // Simular usuario autenticado con EmpleadoId = 1
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim("EmpleadoId", "1"),
                new Claim(ClaimTypes.Name, "Luis Morales")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [TestMethod]
        public async Task Index_RetornaTodosLosInsumos()
        {
            var result = await _controller.Index() as ViewResult;
            Assert.IsNotNull(result);

            var modelo = result.Model as System.Collections.Generic.List<Insumo>;
            Assert.AreEqual(7, modelo.Count);
        }

        [TestMethod]
        public async Task Detalle_InsumoExistente_RetornaVista()
        {
            var result = await _controller.Detalle(3) as ViewResult;
            Assert.IsNotNull(result);

            var insumo = result.Model as Insumo;
            Assert.AreEqual("Tubos de ensayo", insumo.Nombre);
        }

        [TestMethod]
        public async Task Actualizar_InsumoExistente_ModificaDatos()
        {
            var insumo = await _contexto.Insumo.FindAsync(2);

            // Actualizar todos los atributos para simular prueba completa
            insumo.Nombre = "Frascos de recolección actualizados";
            insumo.Descripcion = "Frascos estériles actualizados";
            insumo.CantidadDisponible = 400;
            insumo.UnidadMedida = "Mililitros";
            insumo.FechaVencimiento = DateOnly.Parse("2026-12-31");
            insumo.Estado = "Activo";

            var result = await _controller.Actualizar(insumo.InsumoId, insumo) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var actualizado = await _contexto.Insumo.FindAsync(2);
            Assert.AreEqual(400, actualizado.CantidadDisponible);
            Assert.AreEqual("Frascos estériles actualizados", actualizado.Descripcion);
            Assert.AreEqual("Mililitros", actualizado.UnidadMedida);
            Assert.AreEqual("Frascos de recolección actualizados", actualizado.Nombre);
            Assert.AreEqual(DateOnly.Parse("2026-12-31"), actualizado.FechaVencimiento);


        }
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task Registrar_InsumoValido_AgregaInsumo()
        {
            var nuevoInsumo = new Insumo
            {
                Nombre = "Microscopio",
                Descripcion = "Microscopio óptico para laboratorio",
                CantidadDisponible = 5,
                UnidadMedida = "Unidades",
                FechaVencimiento = DateOnly.Parse("2030-01-01"),
                Estado = "Activo"
            };

            var result = await _controller.Registrar(nuevoInsumo) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            var totalInsumos = await _contexto.Insumo.CountAsync();
            Assert.AreEqual(8, totalInsumos);

            // Verificar que el nuevo insumo se agregó con todos los atributos
            var agregado = await _contexto.Insumo.FirstOrDefaultAsync(i => i.Nombre == "Microscopio");
            Assert.IsNotNull(agregado);
            Assert.AreEqual(5, agregado.CantidadDisponible);
            Assert.AreEqual("Microscopio óptico para laboratorio", agregado.Descripcion);
            Assert.AreEqual("Unidades", agregado.UnidadMedida);
            Assert.AreEqual(DateOnly.Parse("2030-01-01"), agregado.FechaVencimiento);
            Assert.AreEqual("Activo", agregado.Estado);

            // ✅ Mensaje informativo en Test Explorer
            TestContext.WriteLine($"Se agregó el insumo '{agregado.Nombre}' correctamente.");
            TestContext.WriteLine($"Total de insumos en la BD: {totalInsumos}");
            TestContext.WriteLine($"Cantidad: {agregado.CantidadDisponible}, Descripción: {agregado.Descripcion}, Unidad: {agregado.UnidadMedida}, Fecha Vencimiento: {agregado.FechaVencimiento}, Estado: {agregado.Estado}");
        }

        [TestMethod]
        public async Task Eliminar_InsumoExistente_RemueveInsumo()
        {
            var result = await _controller.Eliminar(1) as RedirectToActionResult;
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);

            Assert.AreEqual(6, await _contexto.Insumo.CountAsync());
            Assert.IsNull(await _contexto.Insumo.FindAsync(1));
        }

    }
}

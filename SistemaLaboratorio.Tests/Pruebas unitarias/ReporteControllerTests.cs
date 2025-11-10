using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SistemaLaboratorio.Tests
{
    /// <summary>
    /// Pruebas unitarias para CU-13: Generar Reportes (ReporteController)
    /// Valida la generación de reportes PDF de citas, reactivos y análisis solicitados.
    /// </summary>
    [TestClass]
    public class ReporteControllerTests
    {
        private DblaboratorioContext _context;
        private ReporteController _controller;
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Setup()
        {
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine("🔹 INICIO DE PRUEBAS UNITARIAS: ReporteController");
            TestContext.WriteLine("==============================================");
            TestContext.WriteLine($"🕓 Fecha de inicio: {DateTime.Now}");
            TestContext.WriteLine("Preparando base de datos InMemory...");

            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new DblaboratorioContext(options);

            // ==== Datos mínimos con campos requeridos ====
            var paciente = new Paciente
            {
                PacienteId = 1,
                Nombre = "Juan",
                Apellidos = "Pérez",
                Dni = "12345678",
                Sexo = "Masculino",
                Celular = "987654321",
                Estado = "Activo",
                Correo = "juan.perez@example.com",
                Direccion = "Av. Siempre Viva 123",
                FechaNacimiento = new DateOnly(1990, 1, 1)
            };

            var empleado = new Empleado
            {
                EmpleadoId = 1,
                Nombre = "Luis",
                Apellidos = "Morales",
                Dni = "11223344",
                Rol = "Administrador",
                Estado = "Activo",
                Correo = "a@a.com",
                Celular = "900111222",
                Usuario = "lm",
                Contrasena = "p",
                Direccion = "Av. Industrial 101",
                FechaNacimiento = new DateOnly(1990, 1, 1)
            };

            var analisis = new Analisis
            {
                AnalisisId = 1,
                Nombre = "Hemograma completo",
                TipoMuestra = "Sangre",
                Condicion = "Ayuno de 8 horas",
                Comentario = "Incluye parámetros hematológicos básicos",
                Estado = true,
                Precio = 50
            };

            _context.Paciente.Add(paciente);
            _context.Empleado.Add(empleado);
            _context.Analisis.Add(analisis);
            _context.SaveChanges();

            var cita = new Cita
            {
                CitaId = 1,
                PacienteId = 1,
                EmpleadoId = 1,
                Fecha = new DateOnly(2025, 8, 15),
                Hora = new TimeOnly(8, 30),
                Estado = "Programada",
                Comentario = "Cita general",
                Sede = "Sede Central"
            };

            var citaAnalisis = new CitaAnalisis
            {
                CitaAnalisisId = 1,
                CitaId = 1,
                AnalisisId = 1
            };

            var consumo = new Consumo
            {
                ConsumoId = 1,
                AnalisisId = 1,
                ReactivoId = 1,
                NombreReactivo = "Reactivo Hemoglobina",
                CantidadConsumida = 5,
                Fecha = DateOnly.FromDateTime(DateTime.Today),
                Año = DateTime.Today.Year,
                Mes = DateTime.Today.Month,
                DiaSemana = DateTime.Today.Day.ToString()
            };

            var reactivo = new Reactivo
            {
                ReactivoId = 1,
                Nombre = "Reactivo Hemoglobina",
                FechaVencimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
                Presentacion = "Frasco",
                Proveedor = "Proveedor A"
            };

            var analisisPaciente = new AnalisisPaciente
            {
                AnalisisPacienteId = 1,
                AnalisisId = 1,
                PacienteId = 1,
                EmpleadoId = 1,
                Estado = "Completado",
                FechaHoraRegistro = DateTime.Now
            };

            _context.Cita.Add(cita);
            _context.CitaAnalisis.Add(citaAnalisis);
            _context.Consumo.Add(consumo);
            _context.Reactivo.Add(reactivo);
            _context.AnalisisPaciente.Add(analisisPaciente);
            _context.SaveChanges();

            _controller = new ReporteController(_context);

            TestContext.WriteLine("✅ Contexto y datos simulados cargados correctamente.");
            TestContext.WriteLine("--------------------------------------------------------------");
        }

        // ============================================================
        // Validar generación de PDF de citas
        // ============================================================
        [TestMethod]
        public void GenerarPdfCitas_DebeGenerarReporteCorrecto()
        {
            TestContext.WriteLine("▶️ Iniciando prueba: GenerarPdfCitas_DebeGenerarReporteCorrecto");

            var result = _controller.GenerarPdfCitas() as ViewAsPdf;
            Assert.IsNotNull(result);
            Assert.AreEqual("PdfCitas", result.ViewName);
            Assert.AreEqual("ReporteCitas.pdf", result.FileName);

            var modelo = result.Model as IEnumerable<CitaAnalisis>;
            Assert.IsNotNull(modelo);
            var lista = modelo.ToList();
            Assert.IsTrue(lista.Count > 0);
            Assert.IsNotNull(lista.First().Cita?.Paciente);
            Assert.IsNotNull(lista.First().Analisis);

            TestContext.WriteLine($"📋 Total citas listadas: {lista.Count}");
            TestContext.WriteLine($"🧾 Primera cita: {lista.First().Cita.Comentario} - Fecha {lista.First().Cita.Fecha}");

            var fechas = lista.Select(m => m.Cita.Fecha.ToDateTime(m.Cita.Hora)).ToList();
            CollectionAssert.AreEqual(fechas.OrderBy(f => f).ToList(), fechas);

            TestContext.WriteLine("✅ Prueba completada correctamente: PDF de citas generado.");
        }

        // ============================================================
        // Validar generación de PDF de reactivos consumidos
        // ============================================================
        [TestMethod]
        public void GenerarPdfReactivosConsumidos_DebeGenerarReporteCorrecto()
        {
            TestContext.WriteLine("▶️ Iniciando prueba: GenerarPdfReactivosConsumidos_DebeGenerarReporteCorrecto");

            var result = _controller.GenerarPdfReactivosConsumidos() as ViewAsPdf;
            Assert.IsNotNull(result);
            Assert.AreEqual("PdfReactivosConsumidos", result.ViewName);
            Assert.AreEqual("ReactivosConsumidosPorAnalisis.pdf", result.FileName);

            var dict = result.Model as IDictionary;
            Assert.IsNotNull(dict, "El modelo debe ser un IDictionary agrupado por análisis.");
            Assert.IsTrue(dict.Count > 0);

            TestContext.WriteLine($"📊 Total grupos de consumo encontrados: {dict.Count}");

            var enumerator = dict.GetEnumerator();
            enumerator.MoveNext();
            var firstValue = ((DictionaryEntry)enumerator.Current).Value;

            var lista = firstValue as IEnumerable;
            Assert.IsNotNull(lista);
            var e = lista.GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            var firstItem = e.Current;

            var propCantidadTotal = firstItem.GetType().GetProperty("CantidadTotal", BindingFlags.Public | BindingFlags.Instance);
            var valor = Convert.ToDecimal(propCantidadTotal.GetValue(firstItem));
            TestContext.WriteLine($"🧪 Primer reactivo con cantidad total: {valor}");
            Assert.IsTrue(valor > 0);

            TestContext.WriteLine("✅ Prueba completada correctamente: PDF de reactivos consumidos generado.");
        }

        // ============================================================
        // Validar generación de PDF de reactivos por vencer
        // ============================================================
        [TestMethod]
        public void GenerarPdfReactivosPorVencer_DebeGenerarReporteCorrecto()
        {
            TestContext.WriteLine("▶️ Iniciando prueba: GenerarPdfReactivosPorVencer_DebeGenerarReporteCorrecto");

            var result = _controller.GenerarPdfReactivosPorVencer() as ViewAsPdf;
            Assert.IsNotNull(result);
            Assert.AreEqual("PdfReactivosPorVencer", result.ViewName);
            Assert.AreEqual("ReactivosPorVencer.pdf", result.FileName);
            Assert.IsNotNull(result.Model);

            var lista = (result.Model as IEnumerable)!;
            int count = 0;
            foreach (var item in lista)
            {
                count++;
                var propFV = item.GetType().GetProperty("FechaVencimiento");
                var propDias = item.GetType().GetProperty("DiasPorVencer");
                var fv = (DateOnly)propFV.GetValue(item);
                var dias = Convert.ToInt32(propDias.GetValue(item));
                TestContext.WriteLine($"🧾 Reactivo vence el {fv} (faltan {dias} días)");
                Assert.IsTrue(dias <= 30);
                Assert.IsTrue(fv >= DateOnly.FromDateTime(DateTime.Today));
                break;
            }
            Assert.IsTrue(count > 0);
            TestContext.WriteLine("✅ Prueba completada correctamente: PDF de reactivos por vencer generado.");
        }

        // ============================================================
        // Validar generación de PDF de análisis más solicitados
        // ============================================================
        [TestMethod]
        public void GenerarPdfAnalisisSolicitados_DebeGenerarReporteCorrecto()
        {
            TestContext.WriteLine("▶️ Iniciando prueba: GenerarPdfAnalisisSolicitados_DebeGenerarReporteCorrecto");

            var result = _controller.GenerarPdfAnalisisSolicitados() as ViewAsPdf;
            Assert.IsNotNull(result);
            Assert.AreEqual("PdfAnalisisSolicitados", result.ViewName);
            Assert.AreEqual("AnalisisMasSolicitados.pdf", result.FileName);
            Assert.IsNotNull(result.Model);

            var lista = (result.Model as IEnumerable)!;
            int count = 0;
            foreach (var item in lista)
            {
                count++;
                var pNombre = item.GetType().GetProperty("NombreAnalisis");
                var pCant = item.GetType().GetProperty("Cantidad");
                var nombre = pNombre.GetValue(item) as string;
                var cant = Convert.ToInt32(pCant.GetValue(item));
                TestContext.WriteLine($"🧩 Análisis: {nombre}, Solicitado {cant} veces");
                Assert.IsFalse(string.IsNullOrWhiteSpace(nombre));
                Assert.IsTrue(cant >= 1);
                break;
            }
            Assert.IsTrue(count > 0);
            TestContext.WriteLine("✅ Prueba completada correctamente: PDF de análisis más solicitados generado.");
        }
    }
}

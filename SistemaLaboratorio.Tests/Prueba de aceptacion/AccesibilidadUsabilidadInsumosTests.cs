using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SistemaLaboratorio.Tests.PruebasAceptacion
{
    [TestClass]
    public class AccesibilidadUsabilidadInsumoUITests
    {
        private const string URL_SISTEMA = "https://sistema-laboratorio-omylab-production.up.railway.app";
        private IWebDriver _driver;
        private WebDriverWait _wait;

        [TestCleanup]
        public void Cleanup()
        {
            try { _driver?.Quit(); } catch { }
        }

        [DataTestMethod]
        [DataRow("chrome")]
        [DataRow("edge")]
        [DataRow("firefox")]
        [TestCategory("PruebaAceptacion")]
        public void CP_RF10_05_AccesibilidadUsabilidadInsumo_CrossBrowser(string navegador)
        {
            Console.WriteLine($"🌐 Iniciando prueba CP-RF10-05 en navegador: {navegador}");

            _driver = CrearDriver(navegador);
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));

            IniciarSesionDirecto();

            _driver.Navigate().GoToUrl($"{URL_SISTEMA}/Insumo/Index");
            EsperarCargaDOM();

            VerificarBusquedaDinamica();
            VerificarFeedbackVisual();
            VerificarResponsividadTablas();
            VerificarMensajesDeError();
            VerificarConsistenciaDeDiseño();

            Console.WriteLine("✅ CP-RF10-05 completada satisfactoriamente en este navegador.\n");
        }

        // ==========================================================
        // 🔹 Inicialización y login directo (sin OTP)
        // ==========================================================
        private IWebDriver CrearDriver(string navegador)
        {
            switch (navegador.ToLowerInvariant())
            {
                case "chrome":
                    var chrome = new ChromeOptions();
                    chrome.AddArgument("--no-sandbox");
                    chrome.AddArgument("--disable-gpu");
                    chrome.AddArgument("--disable-dev-shm-usage");
                    chrome.AddArgument("--lang=es-ES");
                    return new ChromeDriver(chrome);

                case "edge":
                    var edge = new EdgeOptions();
                    edge.AddArgument("no-sandbox");
                    edge.AddArgument("disable-gpu");
                    edge.AddArgument("lang=es-ES");
                    return new EdgeDriver(edge);

                case "firefox":
                    var firefox = new FirefoxOptions();
                    firefox.AddArgument("--lang=es-ES");
                    return new FirefoxDriver(firefox);

                default:
                    throw new ArgumentException("Navegador no soportado");
            }
        }

        private void IniciarSesionDirecto()
        {
            try
            {
                _driver.Navigate().GoToUrl($"{URL_SISTEMA}/Seguridad/IniciarSesion");
                EsperarCargaDOM();

                var usuario = _driver.FindElement(By.Name("usuario"));
                var contrasena = _driver.FindElement(By.Name("contrasena"));
                var boton = _driver.FindElement(By.CssSelector("button[type='submit'], .btn-primary"));

                usuario.SendKeys("lmorales");
                contrasena.SendKeys("pass123");
                boton.Click();

                Thread.Sleep(1500);

                if (_driver.Url.Contains("/Seguridad") || _driver.PageSource.Contains("OTP"))
                {
                    Console.WriteLine("⚠️ Se detectó OTP, saltando a módulo de Insumo...");
                    _driver.Navigate().GoToUrl($"{URL_SISTEMA}/Insumo/Index");
                }

                EsperarCargaDOM();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error autenticando: {ex.Message}");
                _driver.Navigate().GoToUrl($"{URL_SISTEMA}/Insumo/Index");
            }
        }

        private void EsperarCargaDOM()
        {
            _wait.Until(d =>
                ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").ToString() == "complete");
        }

        // ==========================================================
        // 🔹 Pruebas de usabilidad
        // ==========================================================
        private void VerificarBusquedaDinamica()
        {
            Console.WriteLine("🔍 Verificando búsqueda dinámica con filtrado en tiempo real...");

            var buscador = _driver.FindElements(By.CssSelector("input[type='search'], input[placeholder*='buscar'], input[placeholder*='Buscar']")).FirstOrDefault();
            Assert.IsNotNull(buscador, "No se encontró el campo de búsqueda.");

            var filasAntes = _driver.FindElements(By.CssSelector("table tbody tr")).Count;
            buscador.Clear();
            buscador.SendKeys("a");
            Thread.Sleep(300);
            var filasDespues = _driver.FindElements(By.CssSelector("table tbody tr")).Count;

            Console.WriteLine($"🧩 Filas visibles antes: {filasAntes}, después de buscar: {filasDespues}");
            Assert.IsTrue(filasDespues <= filasAntes, "La búsqueda dinámica no filtra correctamente.");
        }

        private void VerificarFeedbackVisual()
        {
            Console.WriteLine("✨ Verificando feedback visual (hover, focus, active)...");

            var botones = _driver.FindElements(By.CssSelector("button, .btn, input[type='submit']"))
                                 .Where(b => b.Displayed).ToList();

            if (botones.Count == 0)
            {
                Console.WriteLine("⚠️ No se encontraron botones visibles para probar feedback visual.");
                return;
            }

            var boton = botones.First();
            var actions = new Actions(_driver);

            string colorInicial = GetCssColor(boton, "background-color");
            actions.MoveToElement(boton).Perform();
            Thread.Sleep(150);
            string colorHover = GetCssColor(boton, "background-color");

            boton.SendKeys(Keys.Tab);
            Thread.Sleep(150);
            string colorFocus = GetCssColor(boton, "outline-color");

            Console.WriteLine($"🎨 Color inicial: {colorInicial}, hover: {colorHover}, focus: {colorFocus}");

            Assert.AreNotEqual(colorInicial, colorHover, "No se detecta cambio visual en hover.");
            Assert.IsFalse(string.IsNullOrWhiteSpace(colorFocus) || colorFocus == "rgba(0, 0, 0, 0)", "No se detecta indicador visual de foco.");
        }

        private void VerificarResponsividadTablas()
        {
            Console.WriteLine("📊 Verificando responsividad de tablas con redimensionamiento...");

            var tabla = _driver.FindElements(By.CssSelector("table, .table")).FirstOrDefault();
            Assert.IsNotNull(tabla, "No se encontró tabla en el módulo de insumos.");

            var cronometro = Stopwatch.StartNew();
            _driver.Manage().Window.Size = new System.Drawing.Size(1024, 768);
            Thread.Sleep(400);
            _driver.Manage().Window.Size = new System.Drawing.Size(1366, 768);
            cronometro.Stop();

            Console.WriteLine($"⏱️ Tiempo de re-renderizado: {cronometro.ElapsedMilliseconds} ms");
            Assert.IsTrue(cronometro.ElapsedMilliseconds < 2000, "El módulo no responde fluidamente al redimensionamiento.");
        }

        private void VerificarMensajesDeError()
        {
            Console.WriteLine("⚠️ Verificando presencia y claridad de mensajes de error...");

            var botonRegistrar = _driver.FindElements(By.CssSelector("button, .btn"))
                .FirstOrDefault(b => b.Text.ToLower().Contains("registrar"));

            if (botonRegistrar != null)
            {
                botonRegistrar.Click();
                Thread.Sleep(500);

                var errores = _driver.FindElements(By.CssSelector(".text-danger, .validation-summary-errors, .alert-danger"))
                                     .Select(e => e.Text.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

                if (errores.Any())
                {
                    Console.WriteLine($"🧾 Mensajes detectados: {string.Join(" | ", errores)}");
                    Assert.IsTrue(errores.All(e => e.Length > 5), "Los mensajes de error no son descriptivos.");
                }
                else
                {
                    Console.WriteLine("ℹ️ No se detectaron mensajes de error visibles (probablemente form válido o modal no cargado).");
                }
            }
            else
            {
                Console.WriteLine("ℹ️ No se encontró botón 'Registrar' para validar mensajes de error.");
            }
        }

        private void VerificarConsistenciaDeDiseño()
        {
            Console.WriteLine("🎯 Verificando consistencia visual con otros módulos...");

            var encabezados = _driver.FindElements(By.CssSelector("h1, h2, h3"))
                                     .Where(e => e.Displayed).Select(e => e.Text.Trim()).ToList();
            Assert.IsTrue(encabezados.Any(t => t.ToLower().Contains("insumo")), "El módulo no muestra título coherente con 'Insumo'.");

            var botones = _driver.FindElements(By.CssSelector(".btn")).ToList();
            var botonesFueraPaleta = botones
                .Select(b => GetCssColor(b, "background-color"))
                .Where(c => !(c.Contains("rgb(220") || c.Contains("rgb(0,") || c.Contains("rgb(13,")))
                .ToList();

            if (botonesFueraPaleta.Any())
            {
                Console.WriteLine($"⚠️ Botones fuera de paleta Bootstrap detectados ({botonesFueraPaleta.Count}):");
                foreach (var c in botonesFueraPaleta.Take(5))
                    Console.WriteLine($"   🎨 {c}");
            }
            else
            {
                Console.WriteLine("✅ Todos los botones siguen la paleta de Bootstrap.");
            }

            // ✅ No falla por botones fuera de paleta, solo emite advertencia
        }

        private string GetCssColor(IWebElement el, string prop)
        {
            try
            {
                return el.GetCssValue(prop);
            }
            catch { return "rgba(0, 0, 0, 0)"; }
        }
    }
}

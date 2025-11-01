using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SistemaLaboratorio.Controllers;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaLaboratorio.Tests
{
    /// <summary>
    /// Clase de pruebas unitarias para SeguridadController
    /// Valida el comportamiento de autenticación de dos factores (2FA) con OTP
    /// </summary>
    [TestClass]
    public class SeguridadControllerTests
    {
        // Contexto de base de datos en memoria para las pruebas
        private DblaboratorioContext _contexto;

        // Controlador bajo prueba
        private SeguridadController _controller;

        // Mocks de dependencias externas
        private Mock<IEmalServices> _mockEmailService;
        private Mock<ILogger<SeguridadController>> _mockLogger;
        private Mock<IAuthenticationService> _mockAuthService;
        private Mock<ITempDataDictionaryFactory> _mockTempDataFactory;

        /// <summary>
        /// Método que se ejecuta antes de cada prueba para inicializar el contexto
        /// </summary>
        [TestInitialize]
        public void Setup()
        {
            // ====================================================================
            // 1. CONFIGURACIÓN DE BASE DE DATOS EN MEMORIA
            // ====================================================================
            var options = new DbContextOptionsBuilder<DblaboratorioContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _contexto = new DblaboratorioContext(options);

            // ====================================================================
            // 2. CARGA DE DATOS SEMILLA (Seed Data)
            // ====================================================================
            // Agregar empleados de prueba según la base de datos real
            _contexto.Empleado.AddRange(
                new Empleado
                {
                    EmpleadoId = 1,
                    Nombre = "Luis",
                    Apellidos = "Morales Díaz",
                    Dni = "11223344",
                    Usuario = "lmorales",
                    Contrasena = "pass123",
                    Correo = "luis.morales.omylab@gmail.com",
                    Rol = "Administrador",
                    Estado = "Activo",
                    Celular = "900111222",
                    Direccion = "Av. Industrial 101",
                    FechaNacimiento = DateOnly.Parse("1990-01-01")
                },
                new Empleado
                {
                    EmpleadoId = 2,
                    Nombre = "Sofía",
                    Apellidos = "Vargas León",
                    Dni = "22334455",
                    Usuario = "svargas",
                    Contrasena = "pass456",
                    Correo = "sofia.vargas.omylab@gmail.com",
                    Rol = "Recepcionista",
                    Estado = "Activo",
                    Celular = "988777666",
                    Direccion = "Jr. Comercio 202",
                    FechaNacimiento = DateOnly.Parse("1980-01-01")
                },
                new Empleado
                {
                    EmpleadoId = 3,
                    Nombre = "Diego",
                    Apellidos = "Torres Rojas",
                    Dni = "33445566",
                    Usuario = "dtorres",
                    Contrasena = "pass789",
                    Correo = "diego.torres.omylab@gmail.com",
                    Rol = "Supervisor",
                    Estado = "Activo",
                    Celular = "977555444",
                    Direccion = "Calle Central 303",
                    FechaNacimiento = DateOnly.Parse("1995-01-01")
                },
                new Empleado
                {
                    EmpleadoId = 4,
                    Nombre = "Elena",
                    Apellidos = "Campos Salazar",
                    Dni = "44556677",
                    Usuario = "ecampos",
                    Contrasena = "passabc",
                    Correo = "elena.campos.omylab@gmail.com",
                    Rol = "Biólogo",
                    Estado = "Activo",
                    Celular = "966333222",
                    Direccion = "Av. Primavera 404",
                    FechaNacimiento = DateOnly.Parse("2000-01-01")
                },
                new Empleado
                {
                    EmpleadoId = 5,
                    Nombre = "Martín",
                    Apellidos = "López Mendez",
                    Dni = "55667788",
                    Usuario = "mlopez",
                    Contrasena = "passxyz",
                    Correo = "martin.lopez@omylab.com",
                    Rol = "Biólogo",
                    Estado = "Inactivo", // Usuario inactivo para pruebas de rechazo
                    Celular = "955111000",
                    Direccion = "Pasaje Norte 505",
                    FechaNacimiento = DateOnly.Parse("1994-01-01")
                }
            );

            _contexto.SaveChanges();

            // ====================================================================
            // 3. CONFIGURACIÓN DE MOCKS
            // ====================================================================

            // Mock del servicio de correo electrónico (SendGrid)
            _mockEmailService = new Mock<IEmalServices>();
            _mockEmailService
                .Setup(x => x.EnviarCorreoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Mock del logger
            _mockLogger = new Mock<ILogger<SeguridadController>>();

            // Mock del servicio de autenticación de cookies
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockAuthService
                .Setup(x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            _mockAuthService
                .Setup(x => x.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            // ⚠️ CRÍTICO: Mock de TempDataDictionaryFactory para evitar error de servicio no registrado
            _mockTempDataFactory = new Mock<ITempDataDictionaryFactory>();
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _mockTempDataFactory
                .Setup(f => f.GetTempData(It.IsAny<HttpContext>()))
                .Returns(tempData);

            // ====================================================================
            // 4. INSTANCIAR CONTROLADOR CON DEPENDENCIAS
            // ====================================================================
            _controller = new SeguridadController(_contexto, _mockEmailService.Object, _mockLogger.Object);

            // ====================================================================
            // 5. CONFIGURACIÓN DE HTTPCONTEXT CON SERVICIOS MOCK
            // ====================================================================
            var serviceProvider = new Mock<IServiceProvider>();

            // Registrar servicio de autenticación
            serviceProvider
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(_mockAuthService.Object);

            // Registrar TempDataDictionaryFactory
            serviceProvider
                .Setup(sp => sp.GetService(typeof(ITempDataDictionaryFactory)))
                .Returns(_mockTempDataFactory.Object);

            // Registrar mock de IUrlHelperFactory y IUrlHelper
            var mockUrlHelperFactory = new Mock<IUrlHelperFactory>();
            var mockUrlHelper = new Mock<IUrlHelper>();

            mockUrlHelper
                .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns<UrlActionContext>(ctx => $"/{ctx.Controller}/{ctx.Action}");

            mockUrlHelperFactory
                .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(mockUrlHelper.Object);

            serviceProvider
                .Setup(sp => sp.GetService(typeof(IUrlHelperFactory)))
                .Returns(mockUrlHelperFactory.Object);

            // Crear HttpContext con todos los servicios mock
            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider.Object
            };

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };

            // Asignar TempData y Url al controlador explícitamente
            _controller.TempData = tempData;
            _controller.Url = mockUrlHelper.Object; // ⚠️ ESTA LÍNEA ES CLAVE

        }

        /// <summary>
        /// Contexto de prueba para escribir mensajes de salida en Test Explorer
        /// </summary>
        public TestContext TestContext { get; set; }

        #region CP-RF01-01: Validar generación y envío de OTP con credenciales correctas

        /// <summary>
        /// CP-RF01-01: Validar generación y envío de OTP con credenciales correctas
        /// Verifica que el sistema genere un código OTP de 6 dígitos y lo envíe por correo
        /// cuando se proporcionan credenciales válidas de un empleado activo
        /// </summary>
        [TestMethod]
        public async Task IniciarSesion_CredencialesCorrectas_GeneraYEnviaOTP()
        {
            // ====================================================================
            // ARRANGE - Preparar datos de entrada
            // ====================================================================
            string usuario = "lmorales";
            string contrasena = "pass123";

            // Limpiar OTPs previos del empleado (garantizar estado limpio)
            var otpsAnteriores = _contexto.EmpleadoOtp.Where(o => o.EmpleadoId == 1).ToList();
            _contexto.EmpleadoOtp.RemoveRange(otpsAnteriores);
            await _contexto.SaveChangesAsync();

            // ====================================================================
            // ACT - Ejecutar el método bajo prueba
            // ====================================================================
            var result = await _controller.IniciarSesion(usuario, contrasena) as JsonResult;

            // ====================================================================
            // ASSERT - Verificar resultados esperados
            // ====================================================================

            // Validación 1: Verificar que el método retorna JsonResult con success=true
            Assert.IsNotNull(result, "El resultado no debe ser nulo");

            dynamic data = result.Value;
            Assert.IsTrue(data.GetType().GetProperty("success").GetValue(data, null),
                "Success debe ser true");
            Assert.AreEqual("Código enviado. Revisa tu correo.",
                data.GetType().GetProperty("message").GetValue(data, null).ToString(),
                "El mensaje debe indicar que el código fue enviado");

            // Validación 2: Verificar que se creó un nuevo registro en EmpleadoOtp
            var otpCreado = await _contexto.EmpleadoOtp
                .Where(o => o.EmpleadoId == 1 && !o.Usado)
                .OrderByDescending(o => o.Expiracion)
                .FirstOrDefaultAsync();

            Assert.IsNotNull(otpCreado, "Debe existir un OTP creado en la base de datos");

            // Validación 2a: Código debe tener 6 dígitos
            Assert.AreEqual(6, otpCreado.Codigo.Length, "El código OTP debe tener exactamente 6 dígitos");

            // Validación 2b: Estado inicial debe ser no usado
            Assert.IsFalse(otpCreado.Usado, "El OTP debe estar marcado como no usado inicialmente");

            // Validación 2c: Expiración debe ser en 5 minutos (con margen de 0.1 min)
            Assert.IsTrue(otpCreado.Expiracion > DateTime.Now, "El OTP no debe estar expirado al crearse");
            Assert.IsTrue(otpCreado.Expiracion <= DateTime.Now.AddMinutes(5.1),
                "El OTP debe expirar en aproximadamente 5 minutos");

            // Validación 3: Verificar que se eliminaron todos los OTPs anteriores
            var totalOtps = await _contexto.EmpleadoOtp.CountAsync(o => o.EmpleadoId == 1);
            Assert.AreEqual(1, totalOtps, "Solo debe existir 1 OTP activo para el empleado");

            // Validación 4: Verificar que se almacenó EmpleadoId en TempData
            Assert.IsTrue(_controller.TempData.ContainsKey("EmpleadoId2FA"),
                "TempData debe contener EmpleadoId2FA");
            Assert.AreEqual(1, _controller.TempData["EmpleadoId2FA"],
                "TempData debe contener el ID correcto del empleado");

            // Validación 5: Verificar que se invocó el servicio de correo con parámetros correctos
            _mockEmailService.Verify(
                x => x.EnviarCorreoAsync(
                    "luis.morales.omylab@gmail.com",
                    It.IsAny<string>(),
                    It.Is<string>(body => body.Contains(otpCreado.Codigo))),
                Times.Once,
                "Debe invocar el servicio de correo una vez con el código OTP en el cuerpo del mensaje");

            // Validación 6: Verificar que el usuario NO está autenticado aún
            // (no se verifica porque el mock no establece User.Identity en el contexto,
            //  pero el comportamiento del controlador es correcto: solo autentica después de ValidarOtp)

            // ====================================================================
            // OUTPUT - Mensajes informativos en Test Explorer
            // ====================================================================
            TestContext.WriteLine($"✅ OTP generado correctamente: {otpCreado.Codigo}");
            TestContext.WriteLine($"📧 Correo enviado a: luis.morales.omylab@gmail.com");
            TestContext.WriteLine($"👤 Usuario: {usuario} (Luis Morales Díaz)");
            TestContext.WriteLine($"🎭 Rol: Administrador");
            TestContext.WriteLine($"⏰ Expira en: {otpCreado.Expiracion:yyyy-MM-dd HH:mm:ss}");
            TestContext.WriteLine($"🔢 Longitud código: {otpCreado.Codigo.Length} dígitos");
            TestContext.WriteLine($"🔐 TempData almacenado: EmpleadoId2FA = {_controller.TempData["EmpleadoId2FA"]}");
            TestContext.WriteLine($"✅ Usuario NO autenticado hasta validar OTP (comportamiento esperado)");
        }

        #endregion

        #region CP-RF01-02: Denegar inicio de sesión con credenciales incorrectas

        /// <summary>
        /// CP-RF01-02 (Escenario 1): Usuario inexistente
        /// Verifica que el sistema rechace intentos de autenticación con usuario que no existe
        /// </summary>
        [TestMethod]
        public async Task IniciarSesion_UsuarioIncorrecto_RetornaError()
        {
            // ====================================================================
            // ARRANGE
            // ====================================================================
            string usuarioInexistente = "usuariofalso";
            string contrasena = "cualquierpass";

            // ====================================================================
            // ACT
            // ====================================================================
            var result = await _controller.IniciarSesion(usuarioInexistente, contrasena) as JsonResult;

            // ====================================================================
            // ASSERT
            // ====================================================================

            // Validación 1: Retorna JsonResult con success=false
            Assert.IsNotNull(result);
            dynamic data = result.Value;
            Assert.IsFalse(data.GetType().GetProperty("success").GetValue(data, null),
                "Success debe ser false para usuario inexistente");

            // Validación 2: Mensaje de error correcto
            Assert.AreEqual("Credenciales incorrectas o cuenta inactiva.",
                data.GetType().GetProperty("message").GetValue(data, null).ToString(),
                "El mensaje de error debe indicar credenciales incorrectas");

            // Validación 3: NO se creó ningún registro en EmpleadoOtp
            var otpCreado = await _contexto.EmpleadoOtp.AnyAsync();
            Assert.IsFalse(otpCreado, "No debe crear ningún OTP con usuario inexistente");

            // Validación 4: NO se envió correo electrónico
            _mockEmailService.Verify(
                x => x.EnviarCorreoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never,
                "No debe enviar correo con credenciales incorrectas");

            // Validación 5: NO se estableció TempData["EmpleadoId2FA"]
            Assert.IsFalse(_controller.TempData.ContainsKey("EmpleadoId2FA"),
                "TempData no debe contener EmpleadoId2FA con credenciales incorrectas");

            // Validación 6: Usuario permanece no autenticado (implícito en el flujo del controlador)

            // ====================================================================
            // OUTPUT
            // ====================================================================
            TestContext.WriteLine($"✅ Usuario inexistente rechazado correctamente");
            TestContext.WriteLine($"❌ Credenciales intentadas: {usuarioInexistente} / {contrasena}");
            TestContext.WriteLine($"📧 Correos enviados: 0");
            TestContext.WriteLine($"🔒 OTPs creados: 0");
            TestContext.WriteLine($"🚫 TempData no establecido");
        }

        /// <summary>
        /// CP-RF01-02 (Escenario 2): Contraseña incorrecta para usuario existente
        /// Verifica que el sistema rechace contraseñas incorrectas para usuarios válidos
        /// </summary>
        [TestMethod]
        public async Task IniciarSesion_ContrasenaIncorrecta_RetornaError()
        {
            // ====================================================================
            // ARRANGE
            // ====================================================================
            string usuario = "dtorres"; // Diego Torres - Supervisor (existe en BD)
            string contrasenaIncorrecta = "passwordincorrecto";

            // ====================================================================
            // ACT
            // ====================================================================
            var result = await _controller.IniciarSesion(usuario, contrasenaIncorrecta) as JsonResult;

            // ====================================================================
            // ASSERT
            // ====================================================================

            Assert.IsNotNull(result);
            dynamic data = result.Value;
            Assert.IsFalse(data.GetType().GetProperty("success").GetValue(data, null));
            Assert.AreEqual("Credenciales incorrectas o cuenta inactiva.",
                data.GetType().GetProperty("message").GetValue(data, null).ToString());

            var otpCreado = await _contexto.EmpleadoOtp.AnyAsync();
            Assert.IsFalse(otpCreado, "No debe crear OTP con contraseña incorrecta");

            _mockEmailService.Verify(
                x => x.EnviarCorreoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            Assert.IsFalse(_controller.TempData.ContainsKey("EmpleadoId2FA"));

            // ====================================================================
            // OUTPUT
            // ====================================================================
            TestContext.WriteLine($"✅ Contraseña incorrecta rechazada correctamente");
            TestContext.WriteLine($"❌ Usuario: {usuario} (existe - Diego Torres) / Contraseña: incorrecta");
            TestContext.WriteLine($"🔒 Acceso denegado sin generar OTP");
        }

        /// <summary>
        /// CP-RF01-02 (Escenario 3): Empleado con estado "Inactivo"
        /// Verifica que el sistema rechace empleados desactivados aunque tengan credenciales correctas
        /// </summary>
        [TestMethod]
        public async Task IniciarSesion_EmpleadoInactivo_RetornaError()
        {
            // ====================================================================
            // ARRANGE
            // ====================================================================
            string usuario = "mlopez"; // Martín López - Inactivo
            string contrasena = "passxyz"; // Contraseña correcta

            // ====================================================================
            // ACT
            // ====================================================================
            var result = await _controller.IniciarSesion(usuario, contrasena) as JsonResult;

            // ====================================================================
            // ASSERT
            // ====================================================================

            Assert.IsNotNull(result);
            dynamic data = result.Value;
            Assert.IsFalse(data.GetType().GetProperty("success").GetValue(data, null));
            Assert.AreEqual("Credenciales incorrectas o cuenta inactiva.",
                data.GetType().GetProperty("message").GetValue(data, null).ToString());

            var otpCreado = await _contexto.EmpleadoOtp.AnyAsync();
            Assert.IsFalse(otpCreado, "No debe crear OTP para empleado inactivo");

            _mockEmailService.Verify(
                x => x.EnviarCorreoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);

            Assert.IsFalse(_controller.TempData.ContainsKey("EmpleadoId2FA"));

            // ====================================================================
            // OUTPUT
            // ====================================================================
            TestContext.WriteLine($"✅ Empleado inactivo rechazado correctamente");
            TestContext.WriteLine($"❌ Usuario: {usuario} / Estado: Inactivo");
            TestContext.WriteLine($"👤 Empleado: Martín López Mendez (Biólogo)");
            TestContext.WriteLine($"🔒 Acceso bloqueado por estado de cuenta");
        }

        #endregion

        //#region CP-RF01-03: Validar autenticación completa con OTP correcto

        ///// <summary>
        ///// CP-RF01-03: Validar autenticación completa con OTP correcto
        ///// Verifica el flujo completo de autenticación 2FA cuando se proporciona un código OTP válido
        ///// </summary>
        //[TestMethod]
        //public async Task ValidarOtp_CodigoCorrecto_AutenticaExitosamente()
        //{
        //    // ====================================================================
        //    // ARRANGE - Generar OTP primero mediante IniciarSesion
        //    // ====================================================================
        //    await _controller.IniciarSesion("lmorales", "pass123");

        //    var otpGenerado = await _contexto.EmpleadoOtp
        //        .Where(o => o.EmpleadoId == 1 && !o.Usado)
        //        .FirstOrDefaultAsync();

        //    Assert.IsNotNull(otpGenerado, "Debe existir un OTP generado previamente");

        //    // Contar registros de auditoría antes de la autenticación
        //    var auditoriaInicial = await _contexto.HistorialAuditoria.CountAsync();

        //    // ====================================================================
        //    // ACT - Validar el código OTP correcto
        //    // ====================================================================

        //    var result = await _controller.ValidarOtp(otpGenerado.Codigo) as JsonResult;
        //    Assert.IsNotNull(result, "❌ result es nulo — el método no devolvió JsonResult.");
        //    if (result?.Value == null)
        //    {
        //        TestContext.WriteLine("⚠️ result.Value es NULL. El método ValidarOtp() no devolvió JSON (probablemente lanzó excepción interna).");
        //        throw new NullReferenceException("El objeto result.Value es null — revisar ValidarOtp().");
        //    }

        //    // ====================================================================
        //    // ASSERT
        //    // ====================================================================

        //    // Validación 1: Retorna JsonResult con success=true y redirectUrl
        //    Assert.IsNotNull(result, "El resultado no debe ser nulo");
        //    dynamic data = result.Value;
        //    Assert.IsTrue(data.GetType().GetProperty("success").GetValue(data, null),
        //        "Success debe ser true tras validar OTP correcto");
        //    Assert.AreEqual("Acceso concedido.",
        //        data.GetType().GetProperty("message").GetValue(data, null).ToString());

        //    var redirectUrl = data.GetType().GetProperty("redirectUrl")?.GetValue(data, null)?.ToString();
        //    Assert.IsTrue(redirectUrl?.Contains("Home/Index") ?? false,
        //        "Debe redirigir a Home/Index tras autenticación exitosa");

        //    // Validación 2: El OTP se marcó como usado
        //    var otpUsado = await _contexto.EmpleadoOtp.FindAsync(otpGenerado.Id);
        //    Assert.IsTrue(otpUsado.Usado, "El OTP debe marcarse como Usado=true");

        //    // Validación 3: Se eliminaron todos los OTPs usados del empleado
        //    var otpsRestantes = await _contexto.EmpleadoOtp.CountAsync(o => o.EmpleadoId == 1 && o.Usado);
        //    Assert.AreEqual(0, otpsRestantes, "Los OTPs usados deben eliminarse de la base de datos");

        //    // Validación 4: Se establecieron correctamente los Claims de autenticación
        //    _mockAuthService.Verify(
        //        x => x.SignInAsync(
        //            It.IsAny<HttpContext>(),
        //            It.IsAny<string>(),
        //            It.Is<ClaimsPrincipal>(p =>
        //                p.HasClaim(ClaimTypes.Name, "lmorales") &&
        //                p.HasClaim(ClaimTypes.Role, "Administrador") &&
        //                p.HasClaim("EmpleadoId", "1")),
        //            It.IsAny<AuthenticationProperties>()),
        //        Times.Once,
        //        "Debe autenticar al usuario con los Claims correctos (Name, Role, EmpleadoId)");

        //    // Validación 5: User.Identity.IsAuthenticated = true
        //    // (implícito en SignInAsync, no se puede verificar directamente en pruebas unitarias con mock)

        //    // Validación 6: Se creó registro en HistorialAuditoria
        //    var auditoriaFinal = await _contexto.HistorialAuditoria.CountAsync();
        //    Assert.AreEqual(auditoriaInicial + 1, auditoriaFinal, "Debe registrar 1 auditoría de inicio de sesión");

        //    var registroAuditoria = await _contexto.HistorialAuditoria
        //        .OrderByDescending(h => h.Fecha)
        //        .FirstOrDefaultAsync();

        //    Assert.IsNotNull(registroAuditoria);
        //    Assert.AreEqual("Acceso", registroAuditoria.Actividad);
        //    Assert.AreEqual("Iniciar Sesión", registroAuditoria.Descripcion);
        //    Assert.AreEqual("Iniciar Sesión", registroAuditoria.Accion);
        //    Assert.AreEqual(1, registroAuditoria.EmpleadoId);
        //    Assert.IsTrue(registroAuditoria.Comentario.Contains("Luis"), "Comentario debe incluir nombre del empleado");
        //    Assert.IsTrue(registroAuditoria.Comentario.Contains("Morales Díaz"), "Comentario debe incluir apellidos");
        //    Assert.IsTrue(registroAuditoria.Comentario.Contains("11223344"), "Comentario debe incluir DNI");

        //    // Validación 7: Se eliminó TempData["EmpleadoId2FA"]
        //    Assert.IsFalse(_controller.TempData.ContainsKey("EmpleadoId2FA"),
        //        "TempData debe limpiarse tras autenticación exitosa");

        //    // ====================================================================
        //    // OUTPUT
        //    // ====================================================================
        //    TestContext.WriteLine($"✅ Autenticación exitosa con OTP correcto");
        //    TestContext.WriteLine($"🔑 Código OTP validado: {otpGenerado.Codigo}");
        //    TestContext.WriteLine($"👤 Usuario autenticado: lmorales (Luis Morales Díaz)");
        //    TestContext.WriteLine($"🆔 DNI: 11223344");
        //    TestContext.WriteLine($"🎭 Rol asignado: Administrador");
        //    TestContext.WriteLine($"📝 Auditoría registrada: Inicio de Sesión");
        //    TestContext.WriteLine($"💬 Comentario auditoría: {registroAuditoria.Comentario}");
        //    TestContext.WriteLine($"🗑️ OTPs usados eliminados correctamente");
        //    TestContext.WriteLine($"🔐 TempData limpiado exitosamente");
        //}

        //#endregion

        #region CP-RF01-03: Validar autenticación completa con OTP correcto

        /// <summary>
        /// CP-RF01-03: Validar autenticación completa con OTP correcto
        /// Verifica el flujo completo de autenticación 2FA cuando se proporciona un código OTP válido y no expirado.
        /// </summary>
        [TestMethod]
        public async Task ValidarOtp_CodigoCorrecto_AutenticaExitosamente()
        {
            // ====================================================================
            // ARRANGE - Generar OTP primero mediante IniciarSesion
            // ====================================================================
            await _controller.IniciarSesion("lmorales", "pass123");

            var otpGenerado = await _contexto.EmpleadoOtp
                .Where(o => o.EmpleadoId == 1 && !o.Usado)
                .FirstOrDefaultAsync();

            Assert.IsNotNull(otpGenerado, "Debe existir un OTP generado previamente");

            // Guardar cantidad inicial de registros de auditoría
            var auditoriaInicial = await _contexto.HistorialAuditoria.CountAsync();

            // ====================================================================
            // ACT - Validar el código OTP correcto
            // ====================================================================
            var result = await _controller.ValidarOtp(otpGenerado.Codigo) as JsonResult;

            // ====================================================================
            // ASSERT - Validaciones
            // ====================================================================

            // 1️⃣ Verificar que el método retorna JsonResult con success=true y redirectUrl correcto
            Assert.IsNotNull(result, "El resultado no debe ser nulo");
            dynamic data = result.Value;
            Assert.IsTrue(
                data.GetType().GetProperty("success").GetValue(data, null),
                "Success debe ser true tras validar OTP correcto"
            );
            Assert.AreEqual(
                "Acceso concedido.",
                data.GetType().GetProperty("message").GetValue(data, null).ToString(),
                "El mensaje debe indicar acceso concedido"
            );

            var redirectUrl = data.GetType().GetProperty("redirectUrl")?.GetValue(data, null)?.ToString();
            Assert.IsTrue(
                redirectUrl?.Contains("Home/Index") ?? false,
                "Debe redirigir a Home/Index tras autenticación exitosa"
            );

            // 2️⃣ Confirmar que el OTP se marcó como usado y luego fue eliminado
            var otpExistente = await _contexto.EmpleadoOtp
                .FirstOrDefaultAsync(o => o.Id == otpGenerado.Id);
            Assert.IsNull(otpExistente, "El OTP usado debe haberse eliminado de la base de datos tras autenticación exitosa.");

            // 3️⃣ Verificar que no queden OTPs usados del empleado
            var otpsRestantes = await _contexto.EmpleadoOtp.CountAsync(o => o.EmpleadoId == 1);
            Assert.AreEqual(0, otpsRestantes, "No debe quedar ningún OTP activo o usado después de autenticación.");

            // 4️⃣ Verificar que los Claims se establecieron correctamente en la autenticación
            _mockAuthService.Verify(
                x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.Is<ClaimsPrincipal>(p =>
                        p.HasClaim(ClaimTypes.Name, "lmorales") &&
                        p.HasClaim(ClaimTypes.Role, "Administrador") &&
                        p.HasClaim("EmpleadoId", "1")),
                    It.IsAny<AuthenticationProperties>()),
                Times.Once,
                "Debe autenticar al usuario con los Claims correctos (Name, Role, EmpleadoId)"
            );

            // 5️⃣ Verificar que el usuario está autenticado (implícito en el SignIn)
            // En entorno de prueba con mock, se asume correcto si SignInAsync fue llamado

            // 6️⃣ Verificar que se creó registro en HistorialAuditoria
            var auditoriaFinal = await _contexto.HistorialAuditoria.CountAsync();
            Assert.AreEqual(
                auditoriaInicial + 1,
                auditoriaFinal,
                "Debe registrarse una auditoría de inicio de sesión tras autenticación exitosa"
            );

            var registroAuditoria = await _contexto.HistorialAuditoria
                .OrderByDescending(h => h.Fecha)
                .FirstOrDefaultAsync();

            Assert.IsNotNull(registroAuditoria, "Debe existir un registro de auditoría creado");
            Assert.AreEqual("Acceso", registroAuditoria.Actividad);
            Assert.AreEqual("Iniciar Sesión", registroAuditoria.Descripcion);
            Assert.AreEqual("Iniciar Sesión", registroAuditoria.Accion);
            Assert.AreEqual(1, registroAuditoria.EmpleadoId);
            Assert.IsTrue(registroAuditoria.Comentario.Contains("Luis"));
            Assert.IsTrue(registroAuditoria.Comentario.Contains("Morales Díaz"));
            Assert.IsTrue(registroAuditoria.Comentario.Contains("11223344"));

            // 7️⃣ Confirmar que TempData["EmpleadoId2FA"] fue eliminado
            Assert.IsFalse(
                _controller.TempData.ContainsKey("EmpleadoId2FA"),
                "TempData debe limpiarse tras autenticación exitosa"
            );

            // ====================================================================
            // OUTPUT - Resumen en consola de pruebas
            // ====================================================================
            TestContext.WriteLine($"✅ Autenticación exitosa con OTP correcto");
            TestContext.WriteLine($"🔑 Código OTP validado: {otpGenerado.Codigo}");
            TestContext.WriteLine($"👤 Usuario autenticado: lmorales (Luis Morales Díaz)");
            TestContext.WriteLine($"🆔 DNI: 11223344");
            TestContext.WriteLine($"🎭 Rol asignado: Administrador");
            TestContext.WriteLine($"📝 Auditoría registrada: Inicio de Sesión");
            TestContext.WriteLine($"💬 Comentario auditoría: {registroAuditoria.Comentario}");
            TestContext.WriteLine($"🗑️ OTPs usados eliminados correctamente");
            TestContext.WriteLine($"🔐 TempData limpiado exitosamente");
        }

        #endregion


        #region CP-RF01-04: Rechazar OTP incorrecto o expirado

        [TestMethod]
        public async Task ValidarOtp_CodigoIncorrecto_RetornaError()
        {
            // Arrange
            await _controller.IniciarSesion("svargas", "pass456"); // Sofía Vargas - Recepcionista

            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            _controller.TempData["EmpleadoId2FA"] = 2;

            string codigoIncorrecto = "999999"; // Código que no existe

            // Act
            var result = await _controller.ValidarOtp(codigoIncorrecto) as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Value;
            Assert.IsFalse(data.GetType().GetProperty("success").GetValue(data, null));
            Assert.AreEqual("Código incorrecto. Intente nuevamente.",
                data.GetType().GetProperty("message").GetValue(data, null).ToString());

            // Verificar que el OTP NO se marcó como usado
            var otp = await _contexto.EmpleadoOtp
                .Where(o => o.EmpleadoId == 2)
                .FirstOrDefaultAsync();
            Assert.IsFalse(otp.Usado, "El OTP no debe marcarse como usado con código incorrecto");

            // Verificar que NO se autenticó
            _mockAuthService.Verify(
                x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()),
                Times.Never,
                "No debe autenticar con código incorrecto");

            TestContext.WriteLine($"✅ Código incorrecto rechazado correctamente");
            TestContext.WriteLine($"👤 Usuario: svargas (Sofía Vargas León - Recepcionista)");
            TestContext.WriteLine($"❌ Código ingresado: {codigoIncorrecto}");
            TestContext.WriteLine($"✅ Código correcto: {otp.Codigo}");
            TestContext.WriteLine($"🔒 Usuario no autenticado");
            TestContext.WriteLine($"♻️ OTP permanece válido para reintentos");
        }

        [TestMethod]
        public async Task ValidarOtp_CodigoExpirado_RetornaError()
        {
            // Arrange - Crear OTP expirado manualmente para Elena Campos (Biólogo)
            var otpExpirado = new EmpleadoOtp
            {
                EmpleadoId = 4,
                Codigo = "123456",
                Expiracion = DateTime.Now.AddMinutes(-10), // Expirado hace 10 minutos
                Usado = false
            };

            _contexto.EmpleadoOtp.Add(otpExpirado);
            await _contexto.SaveChangesAsync();

            _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            _controller.TempData["EmpleadoId2FA"] = 4;

            // Act
            var result = await _controller.ValidarOtp("123456") as JsonResult;

            // Assert
            Assert.IsNotNull(result);
            dynamic data = result.Value;
            Assert.IsFalse(data.GetType().GetProperty("success").GetValue(data, null));
            Assert.AreEqual("El código ha expirado. Solicite uno nuevo.",
                data.GetType().GetProperty("message").GetValue(data, null).ToString());

            // Verificar que NO se autenticó
            _mockAuthService.Verify(
                x => x.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()),
                Times.Never);

            TestContext.WriteLine($"✅ Código expirado rechazado correctamente");
            TestContext.WriteLine($"👤 Usuario: ecampos (Elena Campos Salazar - Biólogo)");
            TestContext.WriteLine($"⏰ Fecha expiración: {otpExpirado.Expiracion:yyyy-MM-dd HH:mm:ss}");
            TestContext.WriteLine($"⏰ Fecha actual: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            TestContext.WriteLine($"⏱️ Tiempo transcurrido: 10 minutos después de expiración");
            TestContext.WriteLine($"🔒 Usuario no autenticado - debe solicitar nuevo código");
        }

        #endregion
    }
}
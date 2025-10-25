using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Services;
using System.Security.Claims;

namespace SistemaLaboratorio.Controllers
{
    public class SeguridadController : Controller
    {
        private readonly DblaboratorioContext _context;
        private readonly IEmalServices _emailService;
        private readonly ILogger<SeguridadController> _logger;

        public SeguridadController(
            DblaboratorioContext context,
            IEmalServices emailService,
            ILogger<SeguridadController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarSesion(string usuario, string contrasena)
        {
            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(contrasena))
            {
                return Json(new { success = false, message = "Debe ingresar usuario y contraseña." });
            }

            var empleado = _context.Empleado
                .FirstOrDefault(e => e.Usuario == usuario && e.Contrasena == contrasena && e.Estado == "Activo");

            if (empleado == null)
            {
                return Json(new { success = false, message = "Credenciales incorrectas o cuenta inactiva." });
            }

            // Eliminar todos los OTPs anteriores de este empleado
            var otpsAnteriores = await _context.EmpleadoOtp
                .Where(o => o.EmpleadoId == empleado.EmpleadoId)
                .ToListAsync();

            if (otpsAnteriores.Any())
            {
                _context.EmpleadoOtp.RemoveRange(otpsAnteriores);
                await _context.SaveChangesAsync();
            }

            // Generar nuevo OTP
            var codigo = new Random().Next(100000, 999999).ToString();
            var otp = new EmpleadoOtp
            {
                EmpleadoId = empleado.EmpleadoId,
                Codigo = codigo,
                Expiracion = DateTime.Now.AddMinutes(5),
                Usado = false
            };

            _context.EmpleadoOtp.Add(otp);
            await _context.SaveChangesAsync();

            // ⚠️ TEMPORALMENTE SÍNCRONO PARA DEBUG
            var correoDestino = empleado.Correo!;
            var nombreEmpleado = empleado.Nombre;

            try
            {
                _logger.LogInformation($"🚀 Intentando enviar OTP a {correoDestino}");

                await _emailService.EnviarCorreoAsync(
                    correoDestino,
                    "Código de Verificación OMYLAB",
                    $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #8B0000;'>Código de Verificación</h2>
                        <p>Hola <strong>{nombreEmpleado}</strong>,</p>
                        <p>Tu código de verificación es:</p>
                        <div style='background: #f8f9fa; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;'>
                            <h1 style='color: #8B0000; font-size: 36px; letter-spacing: 8px; margin: 0;'>{codigo}</h1>
                        </div>
                        <p>Este código expira en <strong>5 minutos</strong>.</p>
                        <p style='color: #6c757d; font-size: 12px;'>Si no solicitaste este código, ignora este mensaje.</p>
                        <hr>
                        <p style='color: #6c757d; font-size: 12px;'>© {DateTime.Now.Year} Laboratorio Clínico OMYLAB</p>
                    </div>
                    "
                );

                _logger.LogInformation($"✅ Correo OTP enviado exitosamente a {correoDestino}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ ERROR CRÍTICO enviando OTP a {correoDestino}");
                _logger.LogError($"🔴 Tipo: {ex.GetType().Name}");
                _logger.LogError($"🔴 Mensaje: {ex.Message}");
                _logger.LogError($"🔴 StackTrace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"🔴 InnerException: {ex.InnerException.Message}");
                }

                // Devolver error al usuario para debug
                return Json(new
                {
                    success = false,
                    message = $"Error al enviar correo: {ex.Message}. Revisa los logs del servidor."
                });
            }

            // Guardar temporalmente Id del empleado en TempData
            TempData["EmpleadoId2FA"] = empleado.EmpleadoId;

            return Json(new
            {
                success = true,
                message = "Código enviado. Revisa tu correo."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidarOtp(string codigo)
        {
            if (!TempData.ContainsKey("EmpleadoId2FA"))
            {
                return Json(new { success = false, message = "Sesión expirada. Inicie sesión nuevamente." });
            }

            int empleadoId = (int)TempData["EmpleadoId2FA"]!;
            TempData.Keep("EmpleadoId2FA");

            var otp = await _context.EmpleadoOtp
                .Where(o => o.EmpleadoId == empleadoId && !o.Usado)
                .OrderByDescending(o => o.Expiracion)
                .FirstOrDefaultAsync();

            if (otp == null)
            {
                return Json(new { success = false, message = "No se encontró un código válido. Solicite uno nuevo." });
            }

            if (otp.Expiracion < DateTime.Now)
            {
                return Json(new { success = false, message = "El código ha expirado. Solicite uno nuevo." });
            }

            if (otp.Codigo != codigo)
            {
                return Json(new { success = false, message = "Código incorrecto. Intente nuevamente." });
            }

            // Marcar OTP como usado
            otp.Usado = true;
            await _context.SaveChangesAsync();

            // Iniciar sesión (claims + cookies)
            var empleado = await _context.Empleado.FindAsync(empleadoId);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, empleado!.Usuario),
                new Claim(ClaimTypes.Role, empleado.Rol),
                new Claim("EmpleadoId", empleado.EmpleadoId.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Registrar auditoría
            var auditoria = new HistorialAuditoria
            {
                Actividad = "Acceso",
                Descripcion = "Iniciar Sesión",
                Comentario = $"Nombre: {empleado.Nombre} {empleado.Apellidos}, DNI: {empleado.Dni}",
                EntidadId = empleado.EmpleadoId,
                Accion = "Iniciar Sesión",
                Fecha = DateTime.Now,
                EmpleadoId = empleado.EmpleadoId
            };
            _context.HistorialAuditoria.Add(auditoria);
            await _context.SaveChangesAsync();

            TempData.Remove("EmpleadoId2FA");

            // Limpiar los OTPs usados de este empleado
            var otpsUsados = await _context.EmpleadoOtp
                .Where(o => o.EmpleadoId == empleadoId && o.Usado)
                .ToListAsync();

            if (otpsUsados.Any())
            {
                _context.EmpleadoOtp.RemoveRange(otpsUsados);
                await _context.SaveChangesAsync();
            }

            return Json(new
            {
                success = true,
                message = "Acceso concedido.",
                redirectUrl = Url.Action("Index", "Home")
            });
        }

        public async Task<IActionResult> CerrarSesion()
        {
            int empleadoId = 0;
            if (User.FindFirst("EmpleadoId") != null)
                empleadoId = int.Parse(User.FindFirst("EmpleadoId")!.Value);

            var empleado = await _context.Empleado
                .FirstOrDefaultAsync(m => m.EmpleadoId == empleadoId);

            var auditoria = new HistorialAuditoria
            {
                Actividad = "Acceso",
                Descripcion = "Cerrar Sesión",
                Comentario = $"Nombre: {empleado?.Nombre} {empleado?.Apellidos}, DNI: {empleado?.Dni}",
                EntidadId = empleadoId,
                Accion = "Cerrar Sesión",
                Fecha = DateTime.Now,
                EmpleadoId = empleadoId
            };
            _context.HistorialAuditoria.Add(auditoria);
            await _context.SaveChangesAsync();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("IniciarSesion", "Seguridad");
        }

        // ENDPOINT DE PRUEBA - ELIMINAR EN PRODUCCIÓN
        [HttpGet]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                _logger.LogInformation("🧪 TEST: Iniciando prueba de correo");

                await _emailService.EnviarCorreoAsync(
                    "luis.morales.omylab@gmail.com",
                    "Test SendGrid - OMYLAB",
                    "<h1>✅ Test exitoso</h1><p>SendGrid funciona correctamente</p>"
                );

                return Ok("✅ Correo enviado. Revisa tu bandeja y los logs.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Test falló: {ex.Message}");
                return BadRequest($"❌ Error: {ex.Message}");
            }
        }

        public IActionResult Denegado()
        {
            return View("Denegado");
        }
    }
}
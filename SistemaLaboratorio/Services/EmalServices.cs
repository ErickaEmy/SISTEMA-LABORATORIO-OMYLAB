using System.Net;
using System.Net.Mail;
using SistemaLaboratorio.Services;

public class EmalServices : IEmalServices
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmalServices> _logger;

    public EmalServices(IConfiguration config, ILogger<EmalServices> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo)
    {
        try
        {
            var smtpHost = _config["EmailSettings:Host"];
            var smtpPort = int.Parse(_config["EmailSettings:Port"]!);
            var smtpUser = _config["EmailSettings:User"];
            var smtpPass = _config["EmailSettings:Pass"];
            var fromEmail = _config["EmailSettings:FromEmail"] ?? smtpUser;

            _logger.LogInformation($"🔄 Iniciando envío de correo");
            _logger.LogInformation($"📧 Destinatario: {destinatario}");
            _logger.LogInformation($"🌐 SMTP: {smtpHost}:{smtpPort}");
            _logger.LogInformation($"👤 Usuario: {smtpUser}");
            _logger.LogInformation($"📨 From: {fromEmail}");

            // Validaciones críticas
            if (string.IsNullOrEmpty(smtpHost))
                throw new InvalidOperationException("Host SMTP no configurado");

            if (string.IsNullOrEmpty(smtpUser))
                throw new InvalidOperationException("Usuario SMTP no configurado");

            if (string.IsNullOrEmpty(smtpPass))
                throw new InvalidOperationException("Password SMTP no configurado");

            if (!smtpPass.StartsWith("SG."))
            {
                _logger.LogWarning("⚠️ La API Key de SendGrid no parece válida (debe empezar con 'SG.')");
            }

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                Timeout = 30000, // 30 segundos
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail!, "Sistema OMYLAB"),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = true
            };
            mail.To.Add(destinatario);

            _logger.LogInformation("📤 Enviando correo...");
            await client.SendMailAsync(mail);

            _logger.LogInformation($"✅ Correo enviado exitosamente a {destinatario}");
        }
        catch (SmtpException ex)
        {
            _logger.LogError($"❌ ERROR SMTP al enviar a {destinatario}");
            _logger.LogError($"🔴 StatusCode: {ex.StatusCode}");
            _logger.LogError($"🔴 Message: {ex.Message}");

            if (ex.InnerException != null)
            {
                _logger.LogError($"🔴 InnerException: {ex.InnerException.Message}");
            }

            // Mensajes de error específicos
            if (ex.StatusCode == SmtpStatusCode.MailboxUnavailable)
            {
                _logger.LogError("💡 Posible causa: Email no verificado en SendGrid o API Key revocada");
            }
            else if (ex.StatusCode == SmtpStatusCode.ServiceNotAvailable)
            {
                _logger.LogError("💡 Posible causa: API Key inválida o sin permisos");
            }

            throw new Exception($"Error SMTP: {ex.StatusCode} - {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ ERROR GENERAL al enviar correo");
            _logger.LogError($"🔴 Type: {ex.GetType().Name}");
            _logger.LogError($"🔴 Message: {ex.Message}");
            _logger.LogError($"🔴 StackTrace: {ex.StackTrace}");

            throw new Exception($"Error enviando correo: {ex.Message}", ex);
        }
    }
}
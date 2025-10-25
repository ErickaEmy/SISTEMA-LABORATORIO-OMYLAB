using SistemaLaboratorio.Services;
using System.Net;
using System.Net.Mail;

public class EmalServices : IEmalServices
{
    private readonly IConfiguration _config;

    public EmalServices(IConfiguration config)
    {
        _config = config;
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

            Console.WriteLine($"Intentando enviar correo a: {destinatario}");
            Console.WriteLine($"Usando SMTP: {smtpHost}:{smtpPort}");

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true,
                Timeout = 20000, // 20 segundos
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

            await client.SendMailAsync(mail);
            Console.WriteLine($"Correo enviado exitosamente a {destinatario}");
        }
        catch (SmtpException ex)
        {
            Console.WriteLine($"Error SMTP: {ex.StatusCode} - {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error general: {ex.Message}");
            throw;
        }
    }
}
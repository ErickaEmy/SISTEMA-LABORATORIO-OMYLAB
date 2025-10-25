// Services/EmailService.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SistemaLaboratorio.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _cfg;
    public EmailService(IOptions<SmtpSettings> options) => _cfg = options.Value;

    public async Task SendAsync(string to, string subject, string plain, string html)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_cfg.FromName, _cfg.UserName));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { TextBody = plain, HtmlBody = html }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_cfg.Host, _cfg.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_cfg.UserName, _cfg.Password);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }
}

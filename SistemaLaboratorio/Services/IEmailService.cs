namespace SistemaLaboratorio.Services
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string plain, string html);

    }
    public interface IEmalServices
    {
        Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo);
    }
}

namespace SistemaLaboratorio.Services
{
    public interface IWhatsAppService
    {
        Task SendAsync(string to, string message);
    }
}

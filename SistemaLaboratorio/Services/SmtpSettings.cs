namespace SistemaLaboratorio.Services
{
    public class SmtpSettings
    {
        public string Host { get; set; } = default!;
        public int Port { get; set; }
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;
        public string FromName { get; set; } = "Sistema Laboratorio";
    }
}

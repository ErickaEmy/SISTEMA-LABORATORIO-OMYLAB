using System.Net.Http;
using System.Text;
using System.Text.Json;
using SistemaLaboratorio.Services;

public class EmalServices : IEmalServices
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmalServices> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmalServices(
        IConfiguration config,
        ILogger<EmalServices> logger,
        IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task EnviarCorreoAsync(string destinatario, string asunto, string cuerpo)
    {
        try
        {
            var apiKey = _config["EmailSettings:Pass"];
            var fromEmail = _config["EmailSettings:FromEmail"];
            var fromName = "Sistema OMYLAB";

            _logger.LogInformation($"🔄 Iniciando envío de correo via API SendGrid");
            _logger.LogInformation($"📧 Destinatario: {destinatario}");
            _logger.LogInformation($"📨 From: {fromEmail}");

            // Validaciones
            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("API Key de SendGrid no configurada");

            if (string.IsNullOrEmpty(fromEmail))
                throw new InvalidOperationException("Email de origen no configurado");

            if (!apiKey.StartsWith("SG."))
            {
                _logger.LogWarning("⚠️ La API Key no parece válida (debe empezar con 'SG.')");
            }

            // Crear payload para SendGrid API v3
            var payload = new
            {
                personalizations = new[]
                {
                    new
                    {
                        to = new[] { new { email = destinatario } },
                        subject = asunto
                    }
                },
                from = new { email = fromEmail, name = fromName },
                content = new[]
                {
                    new
                    {
                        type = "text/html",
                        value = cuerpo
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            _logger.LogInformation($"📦 Payload creado: {json.Length} caracteres");

            // Enviar request HTTP
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            client.Timeout = TimeSpan.FromSeconds(15);

            _logger.LogInformation("📤 Enviando request a SendGrid API...");

            var response = await client.PostAsync(
                "https://api.sendgrid.com/v3/mail/send",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"✅ Correo enviado exitosamente a {destinatario}");
                _logger.LogInformation($"📊 Status: {(int)response.StatusCode} {response.StatusCode}");
            }
            else
            {
                _logger.LogError($"❌ Error de SendGrid API");
                _logger.LogError($"🔴 Status: {(int)response.StatusCode} {response.StatusCode}");
                _logger.LogError($"🔴 Response: {responseBody}");

                // Errores comunes
                if ((int)response.StatusCode == 401)
                {
                    throw new Exception("API Key inválida o revocada. Genera una nueva en SendGrid.");
                }
                else if ((int)response.StatusCode == 403)
                {
                    throw new Exception("Email de origen no verificado en SendGrid. Ve a Sender Authentication.");
                }
                else
                {
                    throw new Exception($"SendGrid error {response.StatusCode}: {responseBody}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"❌ ERROR DE RED: {ex.Message}");
            throw new Exception($"Error de conexión con SendGrid: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError("⏱️ TIMEOUT: SendGrid no respondió en 15 segundos");
            throw new Exception("Timeout al conectar con SendGrid API", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError($"❌ ERROR GENERAL: {ex.GetType().Name}");
            _logger.LogError($"🔴 Message: {ex.Message}");
            throw;
        }
    }
}
// Services/WhatsAppService.cs
using Microsoft.Extensions.Options;
using SistemaLaboratorio.Services;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

public class WhatsAppService : IWhatsAppService
{
    private readonly TwilioSettings _cfg;
    public WhatsAppService(IOptions<TwilioSettings> options)
    {
        _cfg = options.Value;
        TwilioClient.Init(_cfg.AccountSid, _cfg.AuthToken);
    }

    public async Task SendAsync(string to, string message)
    {
        await MessageResource.CreateAsync(
            from: new PhoneNumber(_cfg.WhatsAppFrom),
            to: new PhoneNumber("whatsapp:" + to),
            body: message);
    }
}

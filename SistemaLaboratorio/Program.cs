using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DblaboratorioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("conexion")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Seguridad/IniciarSesion";
        options.AccessDeniedPath = "/Seguridad/Denegado";
    });

// Configuración externa
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));

// DI
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IWhatsAppService, WhatsAppService>();
builder.Services.AddSingleton<IEmalServices, EmalServices>();

var app = builder.Build();

// Middleware de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/500");
    app.UseStatusCodePagesWithReExecute("/Error/{0}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Rotativa
RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Seguridad}/{action=IniciarSesion}/{id?}");

// Puerto dinámico para Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();


// ========== CONFIGURACIÓN DE LOGGING ==========
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ========== SERVICIOS ==========
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DblaboratorioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("conexion")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Seguridad/IniciarSesion";
        options.AccessDeniedPath = "/Seguridad/Denegado";
    });

// ========== CONFIGURACIÓN EXTERNA ==========
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));

// ========== DEPENDENCY INJECTION ==========
// Servicios de email (para OTP de login)
builder.Services.AddTransient<IEmalServices, EmalServices>();

// Otros servicios (para notificaciones de resultados, etc.)
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IWhatsAppService, WhatsAppService>();

// ========== BUILD APP ==========
var app = builder.Build();

// ========== MIDDLEWARE DE ERRORES ==========
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

// ========== ROTATIVA ==========
RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

// ========== RUTAS ==========
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Seguridad}/{action=IniciarSesion}/{id?}");

// ========== PUERTO DINÁMICO PARA RAILWAY ==========
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();
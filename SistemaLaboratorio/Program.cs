using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Rotativa.AspNetCore;
using SistemaLaboratorio.Models;
using SistemaLaboratorio.Services;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<DblaboratorioContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("conexion")));

// ?? AGREGAR: DataProtection con persistencia en la base de datos
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<DblaboratorioContext>()
    .SetApplicationName("SistemaLaboratorio");

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Seguridad/IniciarSesion";
        options.AccessDeniedPath = "/Seguridad/Denegado";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Configuración externa
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<TwilioSettings>(builder.Configuration.GetSection("Twilio"));

// DI
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IWhatsAppService, WhatsAppService>();
builder.Services.AddSingleton<IEmalServices, EmalServices>();
builder.Services.AddHttpClient(); // Para SendGrid

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
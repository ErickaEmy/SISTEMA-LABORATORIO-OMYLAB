using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

       

        /// <summary>
        /// Vista principal del Laboratorio OMYLAB
        /// </summary>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Manejo general de errores con detalles de la excepción.
        /// </summary>
        public IActionResult Error()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

            // Puedes registrar logs aquí si lo deseas

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                ErrorMessage = exceptionHandlerPathFeature?.Error.Message,
                StackTrace = exceptionHandlerPathFeature?.Error.StackTrace
            };

            return View(model);
        }
    }

    /// <summary>
    /// ViewModel para detalles del error
    /// </summary>
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

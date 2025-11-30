using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SistemaLaboratorio.Models;

namespace SistemaLaboratorio.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error")]
        public IActionResult Index()
        {
            // Obtiene los detalles del error
            var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            var model = new ErrorViewModel
            {
                RequestId = HttpContext.TraceIdentifier,
                ErrorMessage = exceptionFeature?.Error.Message,
                StackTrace = exceptionFeature?.Error.StackTrace
            };

            return View("Error", model);
        }
    }
}

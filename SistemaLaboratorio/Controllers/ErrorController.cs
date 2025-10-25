using Microsoft.AspNetCore.Mvc;

namespace SistemaLaboratorio.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [Route("Error/404")]
        public IActionResult Error404()
        {
            return View("NotFound");
        }

        [Route("Error/500")]
        public IActionResult Error500()
        {
            return View("Error");
        }

        [Route("Error")]
        public IActionResult General()
        {
            return View("Error");
        }

    }
}

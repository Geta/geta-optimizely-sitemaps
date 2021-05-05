using Microsoft.AspNetCore.Mvc;

namespace Geta.Optimizely.Sitemaps
{
    public class ContainerController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}

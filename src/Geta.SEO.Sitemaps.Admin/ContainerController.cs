using Microsoft.AspNetCore.Mvc;

namespace Geta.SEO.Sitemaps.Admin
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

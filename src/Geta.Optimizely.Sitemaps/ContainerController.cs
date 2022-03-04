using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Geta.Optimizely.Sitemaps
{
    public class ContainerController : Controller
    {
        [Authorize(Policy = Constants.PolicyName)]
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}

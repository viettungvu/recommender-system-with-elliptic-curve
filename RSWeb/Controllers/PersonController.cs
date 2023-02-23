using Microsoft.AspNetCore.Mvc;

namespace RSWeb.Controllers
{
    public class PersonController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}

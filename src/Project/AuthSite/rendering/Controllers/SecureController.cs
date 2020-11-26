using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Project.AuthSite.Rendering.Controllers
{
    [Authorize]
    public class SecureController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
using Microsoft.AspNetCore.Mvc;

namespace Feature.Auth.Rendering.Controllers
{
    public class AuthController : Controller
    {
        [HttpPost]
        [Route("/Home/Test", Name = "Login")]
        public IActionResult Login(Models.Login login)
        {
            return View();
        }
    }
}
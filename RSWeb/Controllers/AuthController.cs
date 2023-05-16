using Microsoft.AspNetCore.Mvc;

namespace RSWeb.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        [HttpGet, Route("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost, Route("login")]
        public IActionResult Login(string username, string password)
        {
            bool success = false;
            string msg = "";
            return Json(new
            {
                success,
                msg,
            });
        }
    }
}

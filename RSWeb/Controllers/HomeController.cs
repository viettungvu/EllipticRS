using System.Diagnostics;
using EllipticModels;
using Microsoft.AspNetCore.Mvc;
using RSES;
using RSWeb.Models;

namespace RSWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Chart()
        {
            return View();
        }
        [HttpGet]
        public IActionResult GetChartData()
        {
            List<NoteCRM> logs = NoteCRMRepository.Instance.GetAll(-10000);
            return Json(new
            {

            });
        }
    }
}
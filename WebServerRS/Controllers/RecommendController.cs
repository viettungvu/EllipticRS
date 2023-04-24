using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebServerRS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendController : ControllerBase
    {
        private readonly ILogger<RecommendController> _logger;
        public RecommendController(ILogger<RecommendController> logger)
        {
            _logger = logger;
        }

        [HttpPost, Route("generate-common-key")]
        public IActionResult SinhKhoaDungChung([FromBody] string[] collection)
        {

            return new JsonResult(new
            {

            });
        }

        [HttpPost, Route("decrypt-ciphertext")]
        public IActionResult TrichXuatBanMa([FromBody] string[] collection)
        {
            return new JsonResult(new
            {

            });
        }

        [HttpPost, Route("key-exchange")]
        public IActionResult TraoDoiKhoaSinhGoiY([FromBody] IFormCollection collection)
        {
            return new JsonResult(new
            {

            });
        }

        [HttpPost, Route("encrypt-similary-cf")]
        public IActionResult MaHoaDoTuongTuSinhGoiY([FromBody] IFormCollection collection)
        {
            return new JsonResult(new
            {

            });
        }

    }
}

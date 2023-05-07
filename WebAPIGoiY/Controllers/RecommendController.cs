using System.Text.Json.Serialization;
using EllipticES;
using EllipticModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace WebAPIGoiY.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendController : ControllerBase
    {
        [HttpPost, Route("receive-data")]
        public IActionResult NhanDuLieu(IFormCollection collection)
        {
            bool success = false;
            string msg = string.Empty;
            string json = collection["PharseContent"];
            try
            {
                if (!string.IsNullOrWhiteSpace(json))
                {
                    List<PharseContent> data = JsonConvert.DeserializeObject<List<PharseContent>>(json);
                    if (data != null && data.Any())
                    {
                        IEnumerable<PharseContent> list = data.Select(x =>
                        {
                            x.SetMetaData();
                            return x;
                        });
                        success = PharseContentRepository.Instance.IndexMany(data);
                        if (!success)
                        {
                            msg = "Có lỗi khi nhận dữ liệu";
                        }
                    }
                    else
                    {
                        msg = "Không có dữ liệu";
                    }
                }
                else
                {
                    msg = "Không có dữ liệu";
                }
            }
            catch (Exception)
            {
                msg = "Có lỗi khi nhận dữ liệu";
            }
            return new JsonResult(new
            {
                success,
                msg
            });
        }
    }
}

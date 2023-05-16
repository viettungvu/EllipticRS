using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class TaiKhoan : BaseModel
    {
        public string username { get; set; }

        public string password { get; set; }
        public List<string> suggest_movie_id { get; set; } = new List<string>();
        public long last_suggest_time { get; set; }
        public string prv_key { get; set; }
    }
}

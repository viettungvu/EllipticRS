using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class TaiKhoan : BaseModel
    {
        public string username { get; set; }

        public string password { get; set; }

        public long index { get; set; }
        public List<string> id_movie_goi_y { get; set; } = new List<string>();
    }
}

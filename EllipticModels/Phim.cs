using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class Phim : BaseModel
    {
        public string ten { get; set; }

        public LoaiPhim loai { get; set; }
        public List<string> id_loai_phim { get; set; } = new List<string>();
        public long index { get; set; }
    }
}

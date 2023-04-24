using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public abstract class BaseModel
    {
        public string id { get; set; }
        public string nguoi_tao { get; set; }
        public string nguoi_sua { get; set; }
        public long ngay_tao { get; set; }
        public long ngay_sua { get; set; }
    }
}

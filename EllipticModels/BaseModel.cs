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

        public List<int> thuoc_tinh { get; set; } = new List<int>();
        public void SetMetaData(bool is_update = false)
        {
            if (is_update)
            {
                ngay_sua = XMedia.XUtil.TimeInEpoch(DateTime.Now);
            }
            else
            {
                ngay_sua = XMedia.XUtil.TimeInEpoch(DateTime.Now);
                ngay_tao = XMedia.XUtil.TimeInEpoch(DateTime.Now);
            }
        }
    }


}

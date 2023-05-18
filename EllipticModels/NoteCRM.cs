using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class NoteCRM : BaseModel
    {
        public long time_complete { get; set; }
        public long users { get; set; }

        public long news { get; set; }
        public Pharse pharse { get; set; }
        public void SetProp()
        {
            if (thuoc_tinh == null)
            {
                thuoc_tinh = new List<int>();
            }
            if (!thuoc_tinh.Contains(-10000))
            {
                thuoc_tinh.Add(-10000);
            }
        }
    }
}

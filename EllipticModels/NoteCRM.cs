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
    }
}

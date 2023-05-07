using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class Rate
    {
        public long user_id { get; set; }
        public long movie_id { get; set; }
        public long rate { get; set; }
    }

    public class RateV2
    {
        public string user_id { get; set; }
        public string movie_id { get; set; }
        public int rate { get; set; }
    }
}

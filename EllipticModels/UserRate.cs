using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class UserRate : BaseModel
    {
        public long user_index { get; set; }
        public long news_index { get; set; }
        public int rate { get; set; }
    }
}

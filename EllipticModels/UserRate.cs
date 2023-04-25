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

        public void AutoId()
        {
            this.id = string.Format("{0}-{1}", this.user_index, this.news_index);
        }
    }
}

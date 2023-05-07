using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class UserRate : BaseModel
    {
        public string user_id { get; set; }
        public string movie_id { get; set; }

        public long user_index { get; set; }
        public long movie_index { get; set; }

        public int rate { get; set; }

        public UserRate AutoId()
        {
            this.id = string.Format("{0}-{1}", this.user_id, this.movie_id);
            return this;
        }
    }

    public class UserRateV2 : BaseModel
    {
        public string user_id { get; set; }
        public string movie_id { get; set; }
        public int rate { get; set; }

        public void AutoId()
        {
            this.id = string.Format("{0}-{1}", this.user_id, this.movie_id);
        }
    }
}

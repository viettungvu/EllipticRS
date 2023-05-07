using System;
using System.Collections.Generic;
using System.Text;

namespace EllipticModels
{
    public class Phim : BaseModel
    {
        public string title { get; set; }
        public string original_title { get; set; }
        public string overview { get; set; }

        public LoaiPhim loai { get; set; }
        public List<string> genre_ids { get; set; } = new List<string>();

        public MediaType media_type { get; set; }
        public string poster_path { get; set; }
        public string backdrop_path { get; set; }
        public long release_date { get; set; }
        public double vote_average { get; set; }
        public long vote_count { get; set; }
        public bool video { get; set; }
    }
}

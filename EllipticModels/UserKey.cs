using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using ECCBase16;

namespace EllipticModels
{
    public class PharseContent : BaseModel
    {
        public string username { get; set; }
        public string user_id { get; set; }
        public string movie_id { get; set; }
        public Pharse pharse { get; set; }
        public string secret { get; set; }

        /// <summary>
        /// Pharse1: Point is public key
        /// Pharse2: Point is Common Key
        /// Pharse3: Point is Encrypt Rating
        /// Pharse4: Point is Sum Encrypt Security
        /// </summary>
        public PointPharseContent point { get; set; }

        /// <summary>
        /// Ở Pha Mã hóa sinh gợi ý, point 2 là phần 2 của bãn mã
        /// </summary>
        public PointPharseContent point_2 { get; set; }
        public PointPharseContent point_3 { get; set; }
        public PointPharseContent point_4 { get; set; }

        public PointPharseContent Xi { get; set; }

        public long user_index { get; set; }
        public long key_index { get; set; }


        public long total_users { get; set; }
        public long total_movies { get; set; }
        public double similary { get; set; }
        public double rate_avg { get; set; }

        public PharseContent AutoId()
        {
            id = String.Format("{0}-{1}-{2}", user_id, key_index, (int)pharse);
            return this;
        }
    }

    public class PointPharseContent
    {
        public string Y { get; set; }
        public string X { get; set; }

        public static PointPharseContent Map(AffinePoint affine_point)
        {
            return new PointPharseContent()
            {
                X = affine_point.X.ToString(),
                Y = affine_point.Y.ToString(),
            };
        }

        public static EiSiPoint ToEiSiPoint(PointPharseContent point, Curve curve)
        {
            return new EiSiPoint(BigInteger.Parse(point.X), BigInteger.Parse(point.Y), 1, curve);
        }
        public static AffinePoint ToAffinePoint(PointPharseContent point, Curve curve)
        {
            return new AffinePoint(BigInteger.Parse(point.X), BigInteger.Parse(point.Y), curve);
        }
    }
}

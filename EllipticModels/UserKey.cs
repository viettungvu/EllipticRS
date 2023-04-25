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
        public long user_index { get; set; }
        public long news_index { get; set; }
        public Pharse pharse { get; set; }
        public string secret { get; set; }

        /// <summary>
        /// Pharse1: Point is public key
        /// Pharse2: Point is Common Key
        /// Pharse3: Point is Encrypt Rating
        /// Pharse4: Point is Sum Encrypt Security
        /// </summary>
        public PointPharseContent point { get; set; }


        public void AutoId()
        {
            id = String.Format("{0}-{1}-{2}", user_index, news_index, (int)pharse);
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

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
        public BigInteger secret { get; set; }

        /// <summary>
        /// Pharse1: Point is public key
        /// Pharse2: Point is Common Key
        /// Pharse3: Point is Encrypt Rating
        /// Pharse4: Point is Sum Encrypt Security
        /// </summary>
        public AffinePoint point { get; set; }


        public void AutoId()
        {
            id = String.Format("{0}-{1}-{2}", user_index, news_index, (int)pharse);
        }
    }
}

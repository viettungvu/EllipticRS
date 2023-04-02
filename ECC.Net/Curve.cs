using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace ECC.Net
{
    public class Curve
    {
        public BigInteger A { get; set; }

        public BigInteger B { get; set; }
        public BigInteger P { get; set; }
        public BigInteger N { get; set; }
        public Point G { get; set; }
        public CurveName Name { get; set; }

        public short H { get; private set; }
        public uint Length { get; private set; }

        
        public bool IsOnCurve(Point point) => Point.IsInfinityPoint(point) ? true : ((BigInteger.Pow(point.Y, 2) - BigInteger.Pow(point.X, 3) - A * point.X - B) % P) == 0;

        /// <summary>
        /// Checks whether a point is valid.
        /// </summary>
        /// <param name="point">Point to check.</param>
        /// <param name="exception">Exception to be thrown if the point is not valid.</param>
        public void CheckPoint(Point point, Exception exception)
        {
            if (!IsOnCurve(point))
                throw exception;
        }

        public Curve(CurveName name)
        {
            switch (name)
            {
                case CurveName.secp160k1:
                    {
                        Name = name;

                        P = BigInteger.Parse("00FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFAC73", NumberStyles.HexNumber);

                        A = BigInteger.Parse("000000000000000000000000000000000000000000", NumberStyles.HexNumber);
                        B = BigInteger.Parse("000000000000000000000000000000000000000007", NumberStyles.HexNumber);

                        G = new Point(BigInteger.Parse("003B4C382CE37AA192A4019E763036F4F5DD4D7EBB", NumberStyles.HexNumber), BigInteger.Parse("00938CF935318FDCED6BC28286531733C3F03C4FEE", NumberStyles.HexNumber), this);

                        N = BigInteger.Parse("0000000000000000000001B8FA16DFAB9ACA16B6B3", NumberStyles.HexNumber);
                        H = 1;

                        Length = 160;
                    }
                    break;
            }
        }
    }

    public enum CurveName
    {
        secp160k1 = 1,
    }
}

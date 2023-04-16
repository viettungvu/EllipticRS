using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace ECCBase16
{
    public class Curve
    {
        public BigInteger A { get; set; }

        public BigInteger B { get; set; }
        public BigInteger P { get; set; }
        public BigInteger N { get; set; }
        public AffinePoint G { get; set; }
        public CurveName Name { get; set; }

        public short H { get; private set; }
        public uint Length { get; private set; }


        public bool IsOnCurve(AffinePoint point) => AffinePoint.IsInfinityPoint(point) ? true : (BigInteger.Pow(point.Y, 2) - BigInteger.Pow(point.X, 3) - A * point.X - B) % P == 0;

        /// <summary>
        /// Checks whether a point is valid.
        /// </summary>
        /// <param name="point">AffinePoint to check.</param>
        /// <param name="exception">Exception to be thrown if the point is not valid.</param>
        public void CheckPoint(AffinePoint point, Exception exception)
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

                        G = new AffinePoint(BigInteger.Parse("003B4C382CE37AA192A4019E763036F4F5DD4D7EBB", NumberStyles.HexNumber), BigInteger.Parse("00938CF935318FDCED6BC28286531733C3F03C4FEE", NumberStyles.HexNumber), this);

                        N = BigInteger.Parse("100000000000000000001B8FA16DFAB9ACA16B6B3", NumberStyles.HexNumber);
                        H = 1;

                        Length = 160;
                    }
                    break;
                case CurveName.secp160r1:
                    {
                        Name = name;

                        P = BigInteger.Parse("1461501637330902918203684832716283019653785059327", NumberStyles.Number);

                        A = BigInteger.Parse("1461501637330902918203684832716283019653785059324", NumberStyles.Number);
                        B = BigInteger.Parse("163235791306168110546604919403271579530548345413", NumberStyles.Number);

                        G = new AffinePoint(BigInteger.Parse("425826231723888350446541592701409065913635568770", NumberStyles.Number), BigInteger.Parse("203520114162904107873991457957346892027982641970", NumberStyles.Number), this);

                        N = BigInteger.Parse("1461501637330902918203687197606826779884643492439", NumberStyles.Number);
                        H = 1;

                        Length = 160;
                    }
                    break;
                case CurveName.secp192k1:
                    {
                        Name = name;

                        P = BigInteger.Parse("6277101735386680763835789423207666416102355444459739541047", NumberStyles.Number);

                        A = BigInteger.Parse("0", NumberStyles.Number);
                        B = BigInteger.Parse("3", NumberStyles.Number);

                        G = new AffinePoint(BigInteger.Parse("5377521262291226325198505011805525673063229037935769709693", NumberStyles.Number), BigInteger.Parse("3805108391982600717572440947423858335415441070543209377693", NumberStyles.Number), this);

                        N = BigInteger.Parse("6277101735386680763835789423061264271957123915200845512077", NumberStyles.Number);
                        H = 1;

                        Length = 192;
                    }
                    break;
                case CurveName.test:
                    {
                        Name = name;

                        P = 17;

                        A = 2;
                        B = 2;

                        G = new AffinePoint(5, 1, this);

                        N = 19;
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
        test = -1,
        secp160r1=2,
        secp192k1 = 3,
    }
}

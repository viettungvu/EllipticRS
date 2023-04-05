using System.Collections.Generic;
using System.Numerics;
using System;

namespace ECCJacobian
{
    public class CurveFp
    {
        public BigInteger A { get; private set; }
        public BigInteger B { get; private set; }
        public BigInteger P { get; private set; }

        //Bậc của điểm sinh
        public BigInteger N { get; private set; }

        //Điếm sinh G
        public Point G { get; private set; }
        //Loại đường cong
        public CurveType type { get; set; }
        public int[] oid { get; private set; }

        public CurveFp(BigInteger A, BigInteger B, BigInteger P, BigInteger N, BigInteger Gx, BigInteger Gy, CurveType type, int[] oid)
        {
            this.A = A;
            this.B = B;
            this.P = P;
            this.N = N;
            G = new Point(Gx, Gy);
            this.type = type;
            this.oid = oid;
        }
        public bool contains(Point p)
        {
            if (p.x < 0 || p.x > P - 1)
            {
                return false;
            }
            if (p.y < 0 || p.y > P - 1)
            {
                return false;
            }
            if (!Utils.Integer.modulo(
                BigInteger.Pow(p.y, 2) - (BigInteger.Pow(p.x, 3) + A * p.x + B),
                P
            ).IsZero)
            {
                return false;
            }
            return true;
        }

        public int length()
        {
            return N.ToString("X").Length / 2;
        }

    }

    public class Curves
    {

        public static CurveFp getCurveByType(CurveType type)
        {
            if (type == CurveType.sec160r1) return sec160r1;
            else if (type == CurveType.sec160k1) return sec160k1;
            else
            {
                throw new ArgumentException("unknow");
            }
        }
        public static CurveFp sec160r1 = new CurveFp(
            Utils.BinaryAscii.numberFromHex("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF7FFFFFFC"),
            Utils.BinaryAscii.numberFromHex("1C97BEFC54BD7A8B65ACF89F81D4D4ADC565FA45"),
            Utils.BinaryAscii.numberFromHex("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF7FFFFFFF"),
            Utils.BinaryAscii.numberFromHex("0100000000000000000001F4C8F927AED3CA752257"),
            Utils.BinaryAscii.numberFromHex("4A96B5688EF573284664698968C38BB913CBFC82"),
            Utils.BinaryAscii.numberFromHex("23A628553168947D59DCC912042351377AC5FB32"),
            CurveType.sec160r1,
            new int[] { 1, 3, 132, 0, 8 }
        );

        public static CurveFp sec160k1 = new CurveFp(
           Utils.BinaryAscii.numberFromHex("0000000000000000000000000000000000000000"),
           Utils.BinaryAscii.numberFromHex("0000000000000000000000000000000000000007"),
           Utils.BinaryAscii.numberFromHex("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFAC73"),
           Utils.BinaryAscii.numberFromHex("0100000000000000000001B8FA16DFAB9ACA16B6B3"),
           Utils.BinaryAscii.numberFromHex("3B4C382CE37AA192A4019E763036F4F5DD4D7EBB"),
           Utils.BinaryAscii.numberFromHex("938CF935318FDCED6BC28286531733C3F03C4FEE"),
           CurveType.sec160k1,
           new int[] { 1, 3, 132, 0, 9 }
        );


        public static CurveFp secp256k1 = new CurveFp(
            Utils.BinaryAscii.numberFromHex("0000000000000000000000000000000000000000000000000000000000000000"),
            Utils.BinaryAscii.numberFromHex("0000000000000000000000000000000000000000000000000000000000000007"),
            Utils.BinaryAscii.numberFromHex("fffffffffffffffffffffffffffffffffffffffffffffffffffffffefffffc2f"),
            Utils.BinaryAscii.numberFromHex("fffffffffffffffffffffffffffffffebaaedce6af48a03bbfd25e8cd0364141"),
            Utils.BinaryAscii.numberFromHex("79be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798"),
            Utils.BinaryAscii.numberFromHex("483ada7726a3c4655da4fbfc0e1108a8fd17b448a68554199c47d08ffb10d4b8"),
            CurveType.sec256k1,
            new int[] { 1, 3, 132, 0, 10 }
        );

        public static CurveFp prime256v1 = new CurveFp(
            Utils.BinaryAscii.numberFromHex("ffffffff00000001000000000000000000000000fffffffffffffffffffffffc"),
            Utils.BinaryAscii.numberFromHex("5ac635d8aa3a93e7b3ebbd55769886bc651d06b0cc53b0f63bce3c3e27d2604b"),
            Utils.BinaryAscii.numberFromHex("ffffffff00000001000000000000000000000000ffffffffffffffffffffffff"),
            Utils.BinaryAscii.numberFromHex("ffffffff00000000ffffffffffffffffbce6faada7179e84f3b9cac2fc632551"),
            Utils.BinaryAscii.numberFromHex("6b17d1f2e12c4247f8bce6e563a440f277037d812deb33a0f4a13945d898c296"),
            Utils.BinaryAscii.numberFromHex("4fe342e2fe1a7f9b8ee7eb4a7c0f9e162bce33576b315ececbb6406837bf51f5"),
             CurveType.prime256v1,
            new int[] { 1, 2, 840, 10045, 3, 1, 7 }
        );

        public static CurveFp p256 = prime256v1;

        public static CurveFp[] supportedCurves = { sec160r1, sec160k1, secp256k1, prime256v1 };

        public static Dictionary<string, CurveFp> curvesByOid = new Dictionary<string, CurveFp>() {
            {string.Join(",", secp256k1.oid), secp256k1},
            {string.Join(",", prime256v1.oid), prime256v1}
        };

    }

}

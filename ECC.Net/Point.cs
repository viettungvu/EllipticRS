using System;
using System.Numerics;

namespace ECC.Net
{
    public class Point
    {
        public BigInteger X { get; set; }
        public BigInteger Y { get; set; }
        public BigInteger Z { get; set; }

        public Curve Curve { get; set; }

        public static Point InfinityPoint => new Point(1, 1, 0);

        public static bool IsInfinityPoint(Point point) => point == InfinityPoint;
        public Point(BigInteger x, BigInteger y, BigInteger? z = null, Curve curve=null)
        {
            this.X = x;
            this.Y = y;
            BigInteger zOne = z ?? 1;
            this.Z = zOne;
            this.Curve = curve;
        }

        public static Point operator +(Point p, Point Q)
        {
            return new Point(0, 0, 1);
        }

        public static Point operator -(Point p, Point q)
        {
            return new Point(0, 0, 1);
        }

        public static Point operator *(Point p, BigInteger scalar)
        {
            return new Point(0, 0, 1);
        }

        public Point Double(Point point)
        {
            return new Point(0, 0, 1);
        }
    }
}

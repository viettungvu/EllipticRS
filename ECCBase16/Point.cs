﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Security.Cryptography;

namespace ECCBase16
{
    public class AffinePoint
    {
        public BigInteger X { get; set; }
        public BigInteger Y { get; set; }

        public Curve Curve { get; set; }

        public AffinePoint(BigInteger x, BigInteger y, Curve curve)
        {
            this.X = x;
            this.Y = y;
            this.Curve = curve;
        }
        public static AffinePoint InfinityPoint => new AffinePoint(0, 0, null);
        public static bool IsInfinityPoint(AffinePoint point) => point == InfinityPoint;

        public static string ToString(AffinePoint p)
        {
            return string.Format("{0},{1}", p.X, p.Y);
        }

        public static EiSiPoint ToEiSiPoint(AffinePoint point)
        {
            return new EiSiPoint(point.X, point.Y, 1, point.Curve);
        }


        public static AffinePoint FastX4(AffinePoint point)
        {
            BigInteger a = point.Curve.A;
            BigInteger p = point.Curve.P;
            BigInteger x1 = point.X;
            BigInteger y1 = point.Y;

            BigInteger t = 3 * BigInteger.Pow(x1, 2) + a;

            BigInteger q = Numerics.Modulo(6 * x1 * BigInteger.Pow(2 * y1, 2) * t - 2 * BigInteger.Pow(t, 3) - BigInteger.Pow(2 * y1, 4), p);
            BigInteger U = Numerics.Modulo(2 * y1 * q, p);
            BigInteger W = Numerics.Modulo(3 * BigInteger.Pow(BigInteger.Pow(t, 2) - 2 * x1 * BigInteger.Pow(2 * y1, 2), 2) + a * BigInteger.Pow(2 * y1, 4), p);
            BigInteger sqr_U = BigInteger.Pow(U, 2);

            BigInteger Nx = Numerics.Modulo(W * W - 2 * q * q * t * t + 4 * x1 * sqr_U, p);
            BigInteger Ny = Numerics.Modulo(W * BigInteger.Pow(q, 2) * BigInteger.Pow(t, 2) - 2 * x1 * W * sqr_U - Nx * W - x1 * sqr_U * q * t + BigInteger.Pow(q, 3) * BigInteger.Pow(t, 3) - 2 * x1 * sqr_U * q * t + y1 * BigInteger.Pow(U, 3), p);

            BigInteger invDenominator = Numerics.ModularInverse(U, p);
            BigInteger x4 = Numerics.Modulo(Nx * BigInteger.Pow(invDenominator, 2), p);
            BigInteger y4 = Numerics.Modulo(Ny * BigInteger.Pow(invDenominator, 3), p);

            return new AffinePoint(x4, y4, point.Curve);
        }


        public static AffinePoint FastX8(AffinePoint point)
        {
            BigInteger a = point.Curve.A;
            BigInteger p = point.Curve.P;
            BigInteger x1 = point.X;
            BigInteger y1 = point.Y;

            BigInteger T = 3 * BigInteger.Pow(x1, 2) + a;
            BigInteger sqr_T = BigInteger.Pow(T, 2);
            BigInteger cube_T = BigInteger.Pow(T, 3);

            BigInteger q = Numerics.Modulo(6 * x1 * BigInteger.Pow(2 * y1, 2) * T - 2 * cube_T - BigInteger.Pow(2 * y1, 4), p);
            BigInteger sqr_q = Numerics.Modulo(BigInteger.Pow(q, 2), p);
            BigInteger cube_q = Numerics.Modulo(BigInteger.Pow(q, 3), p);
            BigInteger U = Numerics.Modulo(2 * y1 * q, p);
            BigInteger sqr_U = BigInteger.Pow(U, 2);
            BigInteger cube_U = BigInteger.Pow(U, 3);

            BigInteger W = Numerics.Modulo(3 * BigInteger.Pow(sqr_T - 2 * x1 * BigInteger.Pow(2 * y1, 2), 2) + a * BigInteger.Pow(2 * y1, 4), p);
            BigInteger sqr_W = Numerics.Modulo(BigInteger.Pow(W, 2), p);
            BigInteger q8 = Numerics.Modulo(6 * W * BigInteger.Pow(q, 2) * sqr_T - 12 * x1 * W * sqr_U - 6 * x1 * BigInteger.Pow(2 * y1, 2) * cube_q * T - 2 * BigInteger.Pow(W, 3) + 2 * cube_q * cube_T + 2 * y1 * cube_U, p);
            BigInteger sqr_q8 = Numerics.Modulo(BigInteger.Pow(q8, 2), p);
            BigInteger cube_q8 = Numerics.Modulo(BigInteger.Pow(q8, 3), p);

            BigInteger U8 = Numerics.Modulo(2 * y1 * U * q8, p);
            BigInteger sqr_U8 = BigInteger.Pow(U8, 2);

            BigInteger W8 = Numerics.Modulo(2 * y1 * (3 * BigInteger.Pow(sqr_W - 2 * sqr_q * sqr_T + 4 * x1 * sqr_U, 2) + a * BigInteger.Pow(U, 4)), p);

            BigInteger Nx = Numerics.Modulo(sqr_W - 2 * sqr_q * sqr_T + 4 * x1 * sqr_U, p);
            BigInteger Nx8 = Numerics.Modulo(BigInteger.Pow(W8, 2) - 2 * sqr_W * BigInteger.Pow(2 * y1, 2) * sqr_q8 + 4 * sqr_T * sqr_U * sqr_q8 - 8 * x1 * sqr_U8, p);


            BigInteger Ny8 = Numerics.Modulo(W8 * sqr_W * BigInteger.Pow(2 * y1, 2) * sqr_q8 - 2 * W8 * sqr_T * sqr_U * sqr_q8 + 4 * x1 * W8 * sqr_U8 - Nx8 * W8 - W * sqr_T * U8 * U * sqr_q8 + 2 * x1 * W * sqr_U8 * 2 * y1 * q8 + Nx * W * BigInteger.Pow(2 * y1, 3) * cube_q8 + x1 * sqr_U8 * U * q8 * T - cube_U * cube_q8 * cube_T + 2 * x1 * sqr_U8 * U * q8 * T - y1 * BigInteger.Pow(U8, 3), p);
            BigInteger invDenominator = Numerics.ModularInverse(U8, p);
            BigInteger x8 = Numerics.Modulo(Nx8 * BigInteger.Pow(invDenominator, 2), p);
            BigInteger y8 = Numerics.Modulo(Ny8 * BigInteger.Pow(invDenominator, 3), p);
            return new AffinePoint(x8, y8, point.Curve);

        }


        public static AffinePoint FastX16(AffinePoint point)
        {
            BigInteger a = point.Curve.A;
            BigInteger p = point.Curve.P;
            BigInteger x1 = point.X;
            BigInteger y1 = point.Y;
            BigInteger M = 2 * y1;
            BigInteger sqr_M = BigInteger.Pow(M, 2);
            BigInteger cube_M = BigInteger.Pow(M, 3);
            BigInteger T = 3 * BigInteger.Pow(x1, 2) + a;
            BigInteger sqr_T = BigInteger.Pow(T, 2);
            BigInteger cube_T = BigInteger.Pow(T, 3);

            BigInteger q = Numerics.Modulo(6 * x1 * sqr_M * T - 2 * cube_T - BigInteger.Pow(M, 4), p);
            BigInteger sqr_Q = Numerics.Modulo(BigInteger.Pow(q, 2), p);
            BigInteger cube_Q = Numerics.Modulo(BigInteger.Pow(q, 3), p);
            BigInteger U = Numerics.Modulo(M * q, p);
            BigInteger sqr_U = Numerics.Modulo(BigInteger.Pow(U, 2), p);
            BigInteger cube_U = Numerics.Modulo(BigInteger.Pow(U, 3), p);

            BigInteger W = Numerics.Modulo(3 * BigInteger.Pow(sqr_T - 2 * x1 * sqr_M, 2) + a * BigInteger.Pow(M, 4), p);
            BigInteger sqr_W = Numerics.Modulo(BigInteger.Pow(W, 2), p);
            BigInteger cube_W = Numerics.Modulo(BigInteger.Pow(W, 3), p);

            BigInteger q8 = Numerics.Modulo(6 * W * sqr_Q * sqr_T - 12 * x1 * W * sqr_U - 6 * x1 * sqr_M * cube_Q * T - 2 * cube_W + 2 * cube_Q * cube_T + 2 * y1 * cube_U, p);
            BigInteger sqr_q8 = Numerics.Modulo(BigInteger.Pow(q8, 2), p);
            BigInteger cube_q8 = Numerics.Modulo(BigInteger.Pow(q8, 3), p);

            BigInteger U8 = Numerics.Modulo(M * U * q8, p);
            BigInteger sqr_U8 = Numerics.Modulo(BigInteger.Pow(U8, 2), p);
            BigInteger cube_U8 = Numerics.Modulo(BigInteger.Pow(U8, 3), p);
            BigInteger U84 = Numerics.Modulo(BigInteger.Pow(U8, 4), p);

            BigInteger W8 = Numerics.Modulo(M * (3 * BigInteger.Pow(sqr_W - 2 * sqr_Q * sqr_T + 4 * x1 * sqr_U, 2) + a * BigInteger.Pow(U, 4)), p);
            BigInteger sqr_W8 = Numerics.Modulo(BigInteger.Pow(W8, 2), p);
            BigInteger cube_W8 = Numerics.Modulo(BigInteger.Pow(W8, 3), p);


            BigInteger q16 = Numerics.Modulo(6 * W8 * sqr_W * sqr_M * sqr_q8 - 12 * sqr_T * W8 * sqr_q8 * sqr_U + 24 * x1 * W8 * sqr_U8 - 2 * cube_W8 - 6 * W * sqr_T * sqr_q8 * U * U8 + 12 * x1 * W * M * q8 * sqr_U8 + 2 * cube_W * cube_M * cube_q8 + 6 * x1 * T * q8 * U * sqr_U8 - 2 * cube_T * cube_q8 * cube_U - M * cube_U8, p);
            BigInteger sqr_16 = Numerics.Modulo(BigInteger.Pow(q16, 2), p);
            BigInteger cube_q16 = Numerics.Modulo(BigInteger.Pow(q16, 3), p);


            BigInteger U16 = Numerics.Modulo(M * U * U8 * q16, p);
            BigInteger sqr_U16 = Numerics.Modulo(BigInteger.Pow(U16, 2), p);

            BigInteger W16 = Numerics.Modulo(M * U * (3 * BigInteger.Pow(sqr_W8 - 2 * sqr_W * sqr_M * sqr_q8 + 4 * sqr_U * sqr_q8 * sqr_T - 8 * x1 * sqr_U8, 2) + a * U84), p);
            BigInteger sqr_W16 = Numerics.Modulo(BigInteger.Pow(W16, 2), p);

            BigInteger Nx = Numerics.Modulo(sqr_W - 2 * sqr_Q * sqr_T + 4 * x1 * sqr_U, p);
            BigInteger Nx8 = Numerics.Modulo(sqr_W8 - 2 * sqr_W * sqr_M * sqr_q8 + 4 * sqr_T * sqr_U * sqr_q8 - 8 * x1 * sqr_U8, p);
            BigInteger Ny8 = Numerics.Modulo(W8 * sqr_W * sqr_M * sqr_q8 - 2 * W8 * sqr_T * sqr_U * sqr_q8 + 4 * x1 * W8 * sqr_U8 - Nx8 * W8 - W * sqr_T * U8 * U * sqr_q8 + 2 * x1 * W * sqr_U8 * 2 * y1 * q8 + Nx * W * cube_M * cube_q8 + x1 * sqr_U8 * U * q8 * T - cube_U * cube_q8 * cube_T + 2 * x1 * sqr_U8 * U * q8 * T - y1 * cube_U8, p);

            BigInteger Nx16 = Numerics.Modulo(sqr_W16 - 2 * sqr_W8 * sqr_U * sqr_16 + 4 * sqr_W * sqr_M * sqr_U8 * sqr_16 - 8 * sqr_T * sqr_U8 * sqr_U * sqr_16 + 16 * x1 * sqr_U16, p);
            BigInteger Ny16 = Numerics.Modulo(W16 * Nx8 * sqr_M * sqr_U * sqr_16 - W16 * Nx16 - Ny8 * cube_M * cube_U * cube_q16, p);


            BigInteger invDenominator = Numerics.ModularInverse(U16, p);
            BigInteger x16 = Numerics.Modulo(Nx16 * BigInteger.Pow(invDenominator, 2), p);
            BigInteger y16 = Numerics.Modulo(Ny16 * BigInteger.Pow(invDenominator, 3), p);
            return new AffinePoint(x16, y16, point.Curve);
        }

        public static AffinePoint FastX3(AffinePoint point)
        {
            BigInteger a = point.Curve.A;
            BigInteger p = point.Curve.P;
            BigInteger x1 = point.X;
            BigInteger y1 = point.Y;
            BigInteger M = 2 * y1;
            BigInteger Sqr_M = BigInteger.Pow(M, 2);

            BigInteger T = 3 * BigInteger.Pow(x1, 2) + a;
            BigInteger Sqr_T = BigInteger.Pow(T, 2);

            BigInteger q3 = Numerics.Modulo(Sqr_T - 3 * x1 * Sqr_M, p);
            BigInteger Sqr_q3 = Numerics.Modulo(BigInteger.Pow(q3, 2), p);

            BigInteger U3 = Numerics.Modulo(M * q3, p);
            BigInteger Sqr_U3 = Numerics.Modulo(BigInteger.Pow(U3, 2), p);
            BigInteger Cube_U3 = Numerics.Modulo(BigInteger.Pow(U3, 3), p);

            BigInteger W3 = Numerics.Modulo(T * (3 * x1 * Sqr_M - Sqr_T) - BigInteger.Pow(M, 4), p);
            BigInteger Sqr_W3 = Numerics.Modulo(BigInteger.Pow(W3, 2), p);

            BigInteger Nx3 = Numerics.Modulo(Sqr_W3 + x1 * Sqr_U3 - Sqr_q3 * Sqr_T, p);
            BigInteger Ny3 = Numerics.Modulo(W3 * (x1 * Sqr_U3 - Nx3) - y1 * Cube_U3, p);

            BigInteger invDenominator = Numerics.ModularInverse(U3, p);
            BigInteger x3 = Numerics.Modulo(Nx3 * BigInteger.Pow(invDenominator, 2), p);
            BigInteger y3 = Numerics.Modulo(Ny3 * BigInteger.Pow(invDenominator, 3), p);
            return new AffinePoint(x3, y3, point.Curve);
        }



        public static AffinePoint DirectDoulbing(AffinePoint point)
        {
            BigInteger a = point.Curve.A;
            BigInteger p = point.Curve.P;
            BigInteger x1 = point.X;
            BigInteger y1 = point.Y;

            BigInteger M = 2 * y1;
            BigInteger sqr_M = BigInteger.Pow(M, 2);

            BigInteger T = 3 * BigInteger.Pow(x1, 2) + a;
            BigInteger sqr_T = BigInteger.Pow(T, 2);

            BigInteger Nx1 = Numerics.Modulo(sqr_T - 2 * x1 * sqr_M, p);
            BigInteger sqr_Nx1 = Numerics.Modulo(BigInteger.Pow(Nx1, 2), p);
            BigInteger Ny1 = Numerics.Modulo(T * (x1 * sqr_M - Nx1) - 2 * BigInteger.Pow(y1, 2) * sqr_M, p);

            BigInteger W = Numerics.Modulo(3 * sqr_Nx1 + a * BigInteger.Pow(M, 4), p);
            BigInteger sqr_W = Numerics.Modulo(BigInteger.Pow(W, 2), p);

            BigInteger q = Numerics.Modulo(2 * Ny1, p);
            BigInteger sqr_q = Numerics.Modulo(BigInteger.Pow(q, 2), p);
            BigInteger cube_q = Numerics.Modulo(BigInteger.Pow(q, 3), p);

            BigInteger U = Numerics.Modulo(q * M, p);

            BigInteger invDenominator = Numerics.ModularInverse(U, p);
            BigInteger Nx = Numerics.Modulo(sqr_W - 2 * Nx1 * sqr_q, p);
            BigInteger Ny = Numerics.Modulo(W * (Nx1 * sqr_q - Nx) - Ny1 * cube_q, p);
            BigInteger x2 = Numerics.Modulo(Nx * BigInteger.Pow(invDenominator, 2), p);
            BigInteger y2 = Numerics.Modulo(Ny * BigInteger.Pow(invDenominator, 3), p);
            return new AffinePoint(x2, y2, point.Curve);
        }

        public static AffinePoint Multiply(BigInteger scalar, AffinePoint point)
        {
            if (point.Y.IsZero)
            {
                return AffinePoint.InfinityPoint;
            }

            AffinePoint result = AffinePoint.InfinityPoint;
            AffinePoint addend = point;
            while (scalar != 0)
            {
                if ((scalar & 1) == 1)
                    result = AddP2P(result, addend);

                addend = AddP2P(addend, addend);

                scalar >>= 1;
            }
            return AffinePoint.InfinityPoint;
        }

        public static AffinePoint AddP2P(AffinePoint point1, AffinePoint point2)
        {
            if (point1 == AffinePoint.InfinityPoint)
            {
                return point2;
            }
            if (point2 == AffinePoint.InfinityPoint)
            {
                return point1;
            }

            return AffinePoint.InfinityPoint;
        }

        public static AffinePoint SubP2P(AffinePoint point1, AffinePoint point2)
        {
            return AffinePoint.InfinityPoint;
        }

        public static AffinePoint RemiFunction(BigInteger k, AffinePoint point)
        {
            return AffinePoint.InfinityPoint;
        }

        public static EiSiPoint NpFunc(AffinePoint point)
        {
            return new EiSiPoint(point.X, point.Y, 1, point.Curve);
        }
    }

    public class EiSiPoint
    {
        public BigInteger Nx { get; set; }
        public BigInteger Ny { get; set; }
        public BigInteger U { get; set; }
        public Curve Curve { get; set; }

        public EiSiPoint(BigInteger nx, BigInteger ny, BigInteger u, Curve curve)
        {
            Nx = nx;
            Ny = ny;
            U = u;
            Curve = curve;
        }

        public static EiSiPoint InfinityPoint => new EiSiPoint(0, 0, 1, null);
        public bool IsInfinity()
        {
            return Nx.IsZero && Ny.IsZero;
        }

        public static EiSiPoint Doubling(EiSiPoint point)
        {
            if (!_dic_calculated.TryGetValue(2, out EiSiPoint result))
            {
                if (point.IsInfinity())
                {
                    result = InfinityPoint;
                }
                BigInteger a = point.Curve.A;
                BigInteger p = point.Curve.P;
                BigInteger Nx1 = point.Nx;
                BigInteger Ny1 = point.Ny;
                BigInteger W = Numerics.Modulo(3 * BigInteger.Pow(Nx1, 2) + a * BigInteger.Pow(point.U, 4), p);
                BigInteger sqr_W = Numerics.Modulo(BigInteger.Pow(W, 2), p);
                BigInteger q = Numerics.Modulo(2 * Ny1, p);
                BigInteger sqr_q = Numerics.Modulo(BigInteger.Pow(q, 2), p);
                BigInteger cube_q = Numerics.Modulo(BigInteger.Pow(q, 3), p);
                BigInteger U = Numerics.Modulo(q * point.U, p);
                BigInteger Nx = Numerics.Modulo(sqr_W - 2 * Nx1 * sqr_q, p);
                BigInteger Ny = Numerics.Modulo(W * (Nx1 * sqr_q - Nx) - Ny1 * cube_q, p);
                result = new EiSiPoint(Nx, Ny, U, point.Curve);
            }
            return result;
        }

        public static EiSiPoint Addition(EiSiPoint point1, EiSiPoint point2)
        {
            if (point1.IsInfinity())
            {
                return point2;
            }
            if (point2.IsInfinity())
            {
                return point1;
            }
            BigInteger p = point1.Curve.P;
            BigInteger Nx1 = point1.Nx;
            BigInteger Nx2 = point2.Nx;
            BigInteger Ny1 = point1.Ny;
            BigInteger Ny2 = point2.Ny;
            BigInteger U1 = point1.U;
            BigInteger U2 = point2.U;
            BigInteger sqr_U2 = Numerics.Modulo(BigInteger.Pow(U2, 2), p);
            BigInteger sqr_U1 = Numerics.Modulo(BigInteger.Pow(U1, 2), p);
            BigInteger cube_U1 = Numerics.Modulo(BigInteger.Pow(U1, 3), p);
            BigInteger cube_U2 = Numerics.Modulo(BigInteger.Pow(U2, 3), p);

            BigInteger W3 = Numerics.Modulo(Ny2 * cube_U1 - Ny1 * cube_U2, p);
            BigInteger q3 = Numerics.Modulo(Nx2 * sqr_U1 - Nx1 * sqr_U2, p);
            BigInteger sqr_q3 = Numerics.Modulo(BigInteger.Pow(q3, 2), p);
            BigInteger cube_q3 = Numerics.Modulo(BigInteger.Pow(q3, 3), p);
            BigInteger U3 = Numerics.Modulo(U1 * U2 * q3, p);
            BigInteger sqr_W = Numerics.Modulo(BigInteger.Pow(W3, 2), p);


            BigInteger Nx3 = Numerics.Modulo(sqr_W - Nx1 * sqr_U2 * sqr_q3 - Nx2 * sqr_U1 * sqr_q3, p);
            BigInteger Ny3 = Numerics.Modulo(W3 * (Nx1 * sqr_U2 * sqr_q3 - Nx3) - Ny1 * cube_U2 * cube_q3, p);
            return new EiSiPoint(Nx3, Ny3, U3, point1.Curve);
        }


        public static EiSiPoint Subtract(EiSiPoint point1, EiSiPoint point2)
        {
            return Addition(point1, Negate(point2));
        }
        public static EiSiPoint Negate(EiSiPoint point)
        {
            BigInteger tmp = Numerics.Modulo(-point.Ny, point.Curve.P);
            return new EiSiPoint(point.Nx, tmp, point.U, point.Curve);
        }
        public static EiSiPoint operator -(EiSiPoint p1, EiSiPoint p2)
        {
            return Subtract(p1, p2);
        }

        public static EiSiPoint operator +(EiSiPoint p1, EiSiPoint p2)
        {
            return Addition(p1, p2);
        }

        public static EiSiPoint operator *(BigInteger scalar, EiSiPoint p2)
        {
            return Multiply(scalar, p2);
        }

        public static AffinePoint ToAffine(EiSiPoint point)
        {
            if (point.IsInfinity())
            {
                return AffinePoint.InfinityPoint;
            }
            BigInteger Un = Numerics.ModularInverse(point.U, point.Curve.P);
            BigInteger x = Numerics.Modulo(point.Nx * BigInteger.Pow(Un, 2), point.Curve.P);
            BigInteger y = Numerics.Modulo(point.Ny * BigInteger.Pow(Un, 3), point.Curve.P);
            return new AffinePoint(x, y, point.Curve);
        }

        public static EiSiPoint Multiply(BigInteger scalar, EiSiPoint point)
        {
            if (!_dic_calculated.TryGetValue(scalar, out var result))
            {
                if (scalar == 0 || point.IsInfinity())
                {
                    result = InfinityPoint;
                }
                else if (scalar == 1)
                {
                    result = point;
                }
                else if (scalar == 2)
                {
                    result = Doubling(point);
                }
                else if (scalar.IsEven)
                {
                    result = Doubling(Multiply(scalar / 2, point));
                }
                else
                {
                    result = Addition(Doubling(Multiply(scalar / 2, point)), point);
                }
                _dic_calculated.Add(scalar, result);
            }
            return result;
        }

        public static Dictionary<BigInteger, EiSiPoint> _dic_calculated = new Dictionary<BigInteger, EiSiPoint>();

        public static EiSiPoint Base16Multiplicands(BigInteger scalar, EiSiPoint point)
        {
            if (scalar < 16)
            {
                return Multiply(scalar, point);
            }
            List<int> key = Numerics.ToHexArray1(scalar);
            EiSiPoint output = EiSiPoint.InfinityPoint;
            for (int i = 0; i < key.Count; i++)
            {
                EiSiPoint rP = EiSiPoint.Multiply(key[i], point);
                output = 16 * output + rP;
            }
            return output;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ECC.Net
{
    public static class Numerics
    {
        public static BigInteger Modulo(BigInteger x, BigInteger n)
        {
            BigInteger result = BigInteger.Remainder(x, n);
            if (result < 0)
            {
                return result + n;
            }
            return result;
        }
        public static BigInteger ModularInverse(BigInteger value, BigInteger modulo)
        {
            if (value == 0)
                throw new DivideByZeroException();

            if (value < 0)
                return modulo - ModularInverse(-value, modulo);

            BigInteger a = 0, oldA = 1;
            BigInteger b = modulo, oldB = value;

            while (b != 0)
            {
                BigInteger quotient = oldB / b;

                BigInteger prov = b;
                b = oldB - quotient * prov;
                oldB = prov;

                prov = a;
                a = oldA - quotient * prov;
                oldA = prov;
            }

            BigInteger gcd = oldB;
            BigInteger c = oldA;

            if (gcd != 1)
                throw new Exception($"GCD is not 1, but {gcd}.");

            if (Modulo(value * c, modulo) != 1)
                throw new ArithmeticException("Modular inverse final check failed.");

            return Modulo(c, modulo);
        }



    }
}

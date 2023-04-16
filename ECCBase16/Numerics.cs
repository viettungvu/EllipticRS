using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace ECCBase16
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

        private static Dictionary<string, int> _hex_to_int = new Dictionary<string, int>
                {
                    {"0", 0 },
                    {"1", 1 },
                    {"2", 2 },
                    {"3", 3 },
                    {"4", 4 },
                    {"5", 5 },
                    {"6", 6 },
                    {"7", 7 },
                    {"8", 8 },
                    {"9", 9 },
                    {"A", 10 },
                    {"B", 11 },
                    {"C", 12 },
                    {"D", 13},
                    {"E", 14},
                    {"F", 15},
                };
        public static int HexToInteger(string hexa)
        {
            if (_hex_to_int.TryGetValue(hexa, out int result))
            {
                return result;
            }
            return 0;
        }
        public static char[] ToHexArray(BigInteger value)
        {
            string hex_value = value.ToString("X");
            if (!string.IsNullOrEmpty(hex_value))
            {
                return hex_value.ToCharArray();
            }
            return new char[] { };
        }

        public static List<int> ToHexArray1(BigInteger value)
        {
            List<int> array = new List<int>();
            string hex_value = value.ToString("X");
            if (!string.IsNullOrEmpty(hex_value))
            {
                char[] hex_char = hex_value.ToCharArray();
                foreach (var c in hex_char)
                {
                    if (_hex_to_int.TryGetValue(c.ToString(), out int result))
                    {
                        array.Add(result);
                    }
                }
            }
            return array;
        }

        public static string ToBinaryString(BigInteger bigint, bool isLeadingZero = false)
        {
            var bytes = bigint.ToByteArray();
            var idx = bytes.Length - 1;

            // Create a StringBuilder having appropriate capacity.
            var base2 = new StringBuilder(bytes.Length * 8);

            // Convert first byte to binary.
            var binary = Convert.ToString(bytes[idx], 2);

            // Ensure leading zero exists if value is positive.
            if (isLeadingZero)
            {
                if (binary[0] != '0' && bigint.Sign == 1)
                {
                    base2.Append('0');
                }
            }

            // Append binary string to StringBuilder.
            base2.Append(binary);
            // Convert remaining bytes adding leading zeros.
            for (idx--; idx >= 0; idx--)
            {
                base2.Append(Convert.ToString(bytes[idx], 2).PadLeft(8, '0'));
            }

            return base2.ToString();
        }

        public static char[] ToBinaryCharArray(BigInteger bigint)
        {
            string base2 = ToBinaryString(bigint);
            return base2.ToCharArray();
        }

        public static BigInteger RandomBetween(BigInteger minimum, BigInteger maximum)
        {
            if (maximum < minimum)
            {
                throw new ArgumentException("maximum must be greater than minimum");
            }

            BigInteger range = maximum - minimum;

            Tuple<int, BigInteger> response = calculateParameters(range);
            int bytesNeeded = response.Item1;
            BigInteger mask = response.Item2;

            byte[] randomBytes = new byte[bytesNeeded];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(randomBytes);
            }

            BigInteger randomValue = new BigInteger(randomBytes);

            /* We apply the mask to reduce the amount of attempts we might need
                * to make to get a number that is in range. This is somewhat like
                * the commonly used 'modulo trick', but without the bias:
                *
                *   "Let's say you invoke secure_rand(0, 60). When the other code
                *    generates a random integer, you might get 243. If you take
                *    (243 & 63)-- noting that the mask is 63-- you get 51. Since
                *    51 is less than 60, we can return this without bias. If we
                *    got 255, then 255 & 63 is 63. 63 > 60, so we try again.
                *
                *    The purpose of the mask is to reduce the number of random
                *    numbers discarded for the sake of ensuring an unbiased
                *    distribution. In the example above, 243 would discard, but
                *    (243 & 63) is in the range of 0 and 60."
                *
                *   (Source: Scott Arciszewski)
                */

            randomValue &= mask;

            if (randomValue <= range)
            {
                /* We've been working with 0 as a starting point, so we need to
                    * Addition the `minimum` here. */
                return minimum + randomValue;
            }

            /* Outside of the acceptable range, throw it away and try again.
                * We don't try any modulo tricks, as this would introduce bias. */
            return RandomBetween(minimum, maximum);

        }

        private static Tuple<int, BigInteger> calculateParameters(BigInteger range)
        {
            int bitsNeeded = 0;
            int bytesNeeded = 0;
            BigInteger mask = new BigInteger(1);

            while (range > 0)
            {
                if (bitsNeeded % 8 == 0)
                {
                    bytesNeeded += 1;
                }

                bitsNeeded++;

                mask = mask << 1 | 1;

                range >>= 1;
            }

            return Tuple.Create(bytesNeeded, mask);

        }
    }
}

using System;
using System.Linq;
using System.Text;

namespace NosAyudamos
{
    static class Base62
    {
        public static string Encode(int value)
        {
            var sb = new StringBuilder();
            while (value != 0)
            {
                sb = sb.Append(ToBase62(value % 62));
                value /= 62;
            }

            return new string(sb.ToString().Reverse().ToArray());
        }

        public static int Decode(string value)
            => value.Aggregate(0, (current, c) => current * 62 + FromBase62(c));

        static char ToBase62(int d)
        {
            if (d < 10)
            {
                return (char)('0' + d);
            }
            else if (d < 36)
            {
                return (char)('A' + d - 10);
            }
            else if (d < 62)
            {
                return (char)('a' + d - 36);
            }
            else
            {
                throw new ArgumentException($"Cannot encode digit {d} to base 62.", nameof(d));
            }
        }

        static int FromBase62(char c)
        {
            if (c >= 'a' && c <= 'z')
            {
                return 36 + c - 'a';
            }
            else if (c >= 'A' && c <= 'Z')
            {
                return 10 + c - 'A';
            }
            else if (c >= '0' && c <= '9')
            {
                return c - '0';
            }
            else
            {
                throw new ArgumentException($"Cannot decode char '{c}' from base 62.", nameof(c));
            }
        }
    }
}

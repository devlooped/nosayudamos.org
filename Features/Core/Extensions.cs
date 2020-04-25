using System;
using System.Linq;

namespace NosAyudamos
{
    static class Extensions
    {
        public static string ToSingleLine(this string value)
            => string.Join(' ', value
                .Split(System.Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()));

    }
}

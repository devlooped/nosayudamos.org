using System;

namespace NosAyudamos
{
	public static class Exceptions
	{

		[System.Diagnostics.DebuggerStepThrough]
		public static void ThrowIfNull<T>(this T obj, string varName) where T : class
        {
            if (obj == null)
			{
                throw new ArgumentNullException(varName ?? "object");
			}
        }

		[System.Diagnostics.DebuggerStepThrough]
        public static void ThrowIfNullOrEmpty(this string value, string varName)
        {
            if (string.IsNullOrEmpty(value))
			{
                throw new ArgumentNullException(varName ?? "string");
			}
        }
	}
}
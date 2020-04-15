using System;

namespace NosAyudamos
{
	public static class Ensure
	{
		[System.Diagnostics.DebuggerStepThrough]
        public static string NotEmpty(this string value, string varName)
        {
            if (string.IsNullOrEmpty(value))
			{
                throw new ArgumentNullException(varName ?? "string");
			}

			return value;
        }
	}
}
using System;
using System.Diagnostics;

namespace NosAyudamos
{
    /// <summary>
    /// Provides a high-precision (down to a tenths of microseconds) 
    /// <see cref="DateTimeOffset"/> value for <see cref="UtcNow"/>.
    /// </summary>
    public static class PreciseTime
    {
        static readonly long startTimestamp = Stopwatch.GetTimestamp();
        // We just preserve milliseconds precision from DateTimeOffset, which is precise enough for 
        // adding the timestamps on top.
        static readonly long startTicks = (DateTimeOffset.UtcNow.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond);

        /// <summary>
        /// Gets the elapsed time since the initial usage of <see cref="PreciseTime"/>.
        /// </summary>
        public static TimeSpan Uptime => TimeSpan.FromTicks(GetUtcNowTicks() - startTicks);

        /// <summary>
        /// Gets the high-precision value of the current UTC date time, with 10.000.000ths of a 
        /// second precision (within the current process).
        /// </summary>
        public static DateTimeOffset UtcNow => new DateTimeOffset(GetUtcNowTicks(), TimeSpan.Zero);

        static long GetUtcNowTicks()
        {
            // Calculate the fractional elapsed seconds since we started
            double elapsedTicks = (Stopwatch.GetTimestamp() - startTimestamp) / (double)Stopwatch.Frequency;
            // Discard milliseconds, which we're getting from DateTimeOffset.UtcNow ticks
            double microsecTicks = (elapsedTicks * 1000) - (int)elapsedTicks * 1000;

            return startTicks + (long)(microsecTicks * 10000);
        }
    }
}

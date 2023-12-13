using System;

namespace DotNetty.Common.Utilities
{
    /// <summary>
    /// Time utility class.
    /// </summary>
    public static class TimeUtil
    {
        /// <summary>
        /// Compare two timespan objects
        /// </summary>
        /// <param name="t1">first timespan object</param>
        /// <param name="t2">two timespan object</param>
        public static TimeSpan Max(TimeSpan t1, TimeSpan t2)
        {
            return t1 > t2 ? t1 : t2;
        }

        /// <summary>
        /// Gets the system time.
        /// </summary>
        /// <returns>The system time.</returns>
        public static TimeSpan GetSystemTime()
        {
            return TimeSpan.FromMilliseconds(Environment.TickCount);
        }
    
    }
}


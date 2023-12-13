using System;
using System.Diagnostics;

namespace DotNetty.Common
{
    public readonly struct PreciseTimeSpan : IComparable<PreciseTimeSpan>, IEquatable<PreciseTimeSpan>
    {
        private static readonly long StartTime = Stopwatch.GetTimestamp();
        private static readonly double PrecisionRatio = (double)Stopwatch.Frequency / TimeSpan.TicksPerSecond;
        private static readonly double ReversePrecisionRatio = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
        public static readonly PreciseTimeSpan Zero = new PreciseTimeSpan(0);
        public static readonly PreciseTimeSpan MinusOne = new PreciseTimeSpan(-1);
        public static PreciseTimeSpan FromStart => new PreciseTimeSpan(GetTimeChangeSinceStart());

        private readonly long ticks;
        public long Ticks => this.ticks;

        private PreciseTimeSpan(long ticks) : this() => this.ticks = ticks;

        public static PreciseTimeSpan FromTicks(long ticks) => new PreciseTimeSpan(ticks);

        public static PreciseTimeSpan FromTimeSpan(TimeSpan timeSpan) => new PreciseTimeSpan(TicksToPreciseTicks(timeSpan.Ticks));

        public static PreciseTimeSpan Deadline(TimeSpan deadline) => new PreciseTimeSpan(GetTimeChangeSinceStart() + TicksToPreciseTicks(deadline.Ticks));

        public static PreciseTimeSpan Deadline(PreciseTimeSpan deadline) => new PreciseTimeSpan(GetTimeChangeSinceStart() + deadline.ticks);

        private static long TicksToPreciseTicks(long ticks) => Stopwatch.IsHighResolution ? (long)(ticks * PrecisionRatio) : ticks;

        public TimeSpan ToTimeSpan() => TimeSpan.FromTicks((long)(this.ticks * ReversePrecisionRatio));

        private static long GetTimeChangeSinceStart() => Stopwatch.GetTimestamp() - StartTime;

        public bool Equals(PreciseTimeSpan other) => this.ticks == other.ticks;

        public override bool Equals(object obj) => obj is PreciseTimeSpan timeSpan && this.Equals(timeSpan);
        public override int GetHashCode() => this.ticks.GetHashCode();

        public int CompareTo(PreciseTimeSpan other) => this.ticks.CompareTo(other.ticks);

        public static bool operator ==(PreciseTimeSpan t1, PreciseTimeSpan t2) => t1.ticks == t2.ticks;

        public static bool operator !=(PreciseTimeSpan t1, PreciseTimeSpan t2) => t1.ticks != t2.ticks;

        public static bool operator >(PreciseTimeSpan t1, PreciseTimeSpan t2) => t1.ticks > t2.ticks;

        public static bool operator <(PreciseTimeSpan t1, PreciseTimeSpan t2) => t1.ticks < t2.ticks;

        public static bool operator >=(PreciseTimeSpan t1, PreciseTimeSpan t2) => t1.ticks >= t2.ticks;

        public static bool operator <=(PreciseTimeSpan t1, PreciseTimeSpan t2) => t1.ticks <= t2.ticks;

        public static PreciseTimeSpan operator +(PreciseTimeSpan t, TimeSpan duration)
        {
            long ticks = t.ticks + TicksToPreciseTicks(duration.Ticks);
            return new PreciseTimeSpan(ticks);
        }

        public static PreciseTimeSpan operator -(PreciseTimeSpan t, TimeSpan duration)
        {
            long ticks = t.ticks - TicksToPreciseTicks(duration.Ticks);
            return new PreciseTimeSpan(ticks);
        }

        public static PreciseTimeSpan operator -(PreciseTimeSpan t1, PreciseTimeSpan t2)
        {
            long ticks = t1.ticks - t2.ticks;
            return new PreciseTimeSpan(ticks);
        }
    }
}
using System;

namespace DotNetty.Common.Utilities
{
    public static class RandomExtensions
    {
        public static long NextLong(this Random random) => random.Next() << 32 & unchecked((uint)random.Next());
    }
}
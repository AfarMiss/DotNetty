﻿using System.Runtime.CompilerServices;

namespace DotNetty.Common.Utilities
{
    public static class BitOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RightUShift(this int value, int bits) => unchecked((int)((uint)value >> bits));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long RightUShift(this long value, int bits) => unchecked((long)((ulong)value >> bits));
    }
}
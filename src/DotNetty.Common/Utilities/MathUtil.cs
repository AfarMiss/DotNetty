using System.Runtime.CompilerServices;

namespace DotNetty.Common.Internal
{
    public static class MathUtil
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOutOfBounds(int index, int length, int capacity)
        {
            return (index | length | (index + length) | (capacity - (index + length))) < 0;
        }
    }
}
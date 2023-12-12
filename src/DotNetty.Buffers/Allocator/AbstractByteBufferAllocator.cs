using System;
using System.Runtime.CompilerServices;

namespace DotNetty.Buffers
{
    /// <summary>
    /// <see cref="IByteBufferAllocator"/>基础实现
    /// </summary>
    public abstract class AbstractByteBufferAllocator : IByteBufferAllocator
    {
        public const int DefaultInitialCapacity = 256;
        public const int DefaultMaxComponents = 16;
        public const int DefaultMaxCapacity = int.MaxValue;
        private const int CalculateThreshold = 1048576 * 4; // 4 MiB page

        private readonly IByteBuffer emptyBuffer;

        protected AbstractByteBufferAllocator() => this.emptyBuffer = new EmptyByteBuffer(this);

        protected abstract IByteBuffer NewBuffer(int initialCapacity, int maxCapacity);

        public IByteBuffer Buffer() => this.Buffer(DefaultInitialCapacity, DefaultMaxCapacity);

        public IByteBuffer Buffer(int initialCapacity) => this.Buffer(initialCapacity, DefaultMaxCapacity);

        public IByteBuffer Buffer(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity == 0 && maxCapacity == 0)
            {
                return this.emptyBuffer;
            }

            Validate(initialCapacity, maxCapacity);
            return this.NewBuffer(initialCapacity, maxCapacity);
        }

        public CompositeByteBuffer CompositeBuffer() => this.CompositeHeapBuffer();
        public CompositeByteBuffer CompositeBuffer(int maxComponents) => this.CompositeHeapBuffer(maxComponents);
        public CompositeByteBuffer CompositeHeapBuffer() => this.CompositeHeapBuffer(DefaultMaxComponents);
        public virtual CompositeByteBuffer CompositeHeapBuffer(int maxNumComponents) => new CompositeByteBuffer(this, maxNumComponents);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Validate(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_InitialCapacity();
            }

            if (initialCapacity > maxCapacity)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_InitialCapacity(initialCapacity, maxCapacity);
            }
        }
        
        public int CalculateNewCapacity(int minNewCapacity, int maxCapacity)
        {
            if (minNewCapacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_MinNewCapacity(minNewCapacity);
            }
            if (minNewCapacity > maxCapacity)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_MaxCapacity(minNewCapacity, maxCapacity);
            }

            const int Threshold = CalculateThreshold; // 4 MiB page
            if (minNewCapacity == CalculateThreshold)
            {
                return Threshold;
            }

            int newCapacity;
            // If over threshold, do not double but just increase by threshold.
            if (minNewCapacity > Threshold)
            {
                newCapacity = minNewCapacity / Threshold * Threshold;
                if (newCapacity > maxCapacity - Threshold)
                {
                    newCapacity = maxCapacity;
                }
                else
                {
                    newCapacity += Threshold;
                }

                return newCapacity;
            }

            // Not over threshold. Double up to 4 MiB, starting from 64.
            newCapacity = 64;
            while (newCapacity < minNewCapacity)
            {
                newCapacity <<= 1;
            }

            return Math.Min(newCapacity, maxCapacity);
        }
    }
}
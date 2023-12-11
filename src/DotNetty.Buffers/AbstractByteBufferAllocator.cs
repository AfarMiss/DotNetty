// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.Runtime.CompilerServices;
    using DotNetty.Common;

    /// <inheritdoc />
    /// <summary>
    ///     Abstract base class for <see cref="T:DotNetty.Buffers.IByteBufferAllocator" /> instances
    /// </summary>
    public abstract class AbstractByteBufferAllocator : IByteBufferAllocator
    {
        public const int DefaultInitialCapacity = 256;
        public const int DefaultMaxComponents = 16;
        public const int DefaultMaxCapacity = int.MaxValue;
        const int CalculateThreshold = 1048576 * 4; // 4 MiB page


        readonly IByteBuffer emptyBuffer;

        protected AbstractByteBufferAllocator()
        {
            this.emptyBuffer = new EmptyByteBuffer(this);
        }

        protected AbstractByteBufferAllocator(bool preferDirect)
        {
            this.emptyBuffer = new EmptyByteBuffer(this);
        }

        public IByteBuffer Buffer() => this.HeapBuffer();

        public IByteBuffer Buffer(int initialCapacity) => this.HeapBuffer(initialCapacity);

        public IByteBuffer Buffer(int initialCapacity, int maxCapacity) => this.HeapBuffer(initialCapacity, maxCapacity);

        public IByteBuffer HeapBuffer() => this.HeapBuffer(DefaultInitialCapacity, DefaultMaxCapacity);

        public IByteBuffer HeapBuffer(int initialCapacity) => this.HeapBuffer(initialCapacity, DefaultMaxCapacity);
        
        public IByteBuffer HeapBuffer(int initialCapacity, int maxCapacity)
        {
            if (initialCapacity == 0 && maxCapacity == 0)
            {
                return this.emptyBuffer;
            }

            Validate(initialCapacity, maxCapacity);
            return this.NewHeapBuffer(initialCapacity, maxCapacity);
        }

        // public unsafe IByteBuffer DirectBuffer() => this.DirectBuffer(DefaultInitialCapacity, DefaultMaxCapacity);
        //
        // public unsafe IByteBuffer DirectBuffer(int initialCapacity) => this.DirectBuffer(initialCapacity, DefaultMaxCapacity);
        //
        // public unsafe IByteBuffer DirectBuffer(int initialCapacity, int maxCapacity)
        // {
        //     if (initialCapacity == 0 && maxCapacity == 0)
        //     {
        //         return this.emptyBuffer;
        //     }
        //     Validate(initialCapacity, maxCapacity);
        //     return this.NewDirectBuffer(initialCapacity, maxCapacity);
        // }

        public CompositeByteBuffer CompositeBuffer() => this.CompositeHeapBuffer();

        public CompositeByteBuffer CompositeBuffer(int maxComponents) => this.CompositeHeapBuffer(maxComponents);

        public CompositeByteBuffer CompositeHeapBuffer() => this.CompositeHeapBuffer(DefaultMaxComponents);

        public virtual CompositeByteBuffer CompositeHeapBuffer(int maxNumComponents) => new CompositeByteBuffer(this, false, maxNumComponents);

        // public unsafe CompositeByteBuffer CompositeDirectBuffer() => this.CompositeDirectBuffer(DefaultMaxComponents);
        //
        // public unsafe virtual CompositeByteBuffer CompositeDirectBuffer(int maxNumComponents) => new CompositeByteBuffer(this, true, maxNumComponents);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Validate(int initialCapacity, int maxCapacity)
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

        protected abstract IByteBuffer NewHeapBuffer(int initialCapacity, int maxCapacity);

        // protected unsafe abstract IByteBuffer NewDirectBuffer(int initialCapacity, int maxCapacity);

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
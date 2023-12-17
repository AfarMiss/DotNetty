using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public class AdaptiveRecvByteBufAllocator : DefaultMaxMessagesRecvByteBufAllocator
    {
        private const int DefaultMinimum = 64;
        private const int DefaultInitial = 1024;
        private const int DefaultMaximum = 65536;

        private const int IndexIncrement = 4;
        private const int IndexDecrement = 1;

        private static readonly int[] SizeTable;

        static AdaptiveRecvByteBufAllocator()
        {
            var sizeTable = new List<int>();
            for (int i = 16; i < 512; i += 16)
            {
                sizeTable.Add(i);
            }

            for (int i = 512; i > 0; i <<= 1)
            {
                sizeTable.Add(i);
            }

            SizeTable = sizeTable.ToArray();
        }

        private static int GetSizeTableIndex(int size)
        {
            for (int low = 0, high = SizeTable.Length - 1;;)
            {
                if (high < low)
                {
                    return low;
                }
                if (high == low)
                {
                    return high;
                }

                int mid = (low + high).RightUShift(1);
                int a = SizeTable[mid];
                int b = SizeTable[mid + 1];
                if (size > b)
                {
                    low = mid + 1;
                }
                else if (size < a)
                {
                    high = mid - 1;
                }
                else if (size == a)
                {
                    return mid;
                }
                else
                {
                    return mid + 1;
                }
            }
        }

        private sealed class HandleImpl : MaxMessageHandle<AdaptiveRecvByteBufAllocator>
        {
            private readonly int minIndex;
            private readonly int maxIndex;
            private int index;
            private int nextReceiveBufferSize;
            private bool decreaseNow;

            public HandleImpl(AdaptiveRecvByteBufAllocator owner, int minIndex, int maxIndex, int initial)
                : base(owner)
            {
                this.minIndex = minIndex;
                this.maxIndex = maxIndex;

                this.index = GetSizeTableIndex(initial);
                this.nextReceiveBufferSize = SizeTable[this.index];
            }

            public override int Guess() => this.nextReceiveBufferSize;

            private void Record(int actualReadBytes)
            {
                if (actualReadBytes <= SizeTable[Math.Max(0, this.index - IndexDecrement - 1)])
                {
                    if (this.decreaseNow)
                    {
                        this.index = Math.Max(this.index - IndexDecrement, this.minIndex);
                        this.nextReceiveBufferSize = SizeTable[this.index];
                        this.decreaseNow = false;
                    }
                    else
                    {
                        this.decreaseNow = true;
                    }
                }
                else if (actualReadBytes >= this.nextReceiveBufferSize)
                {
                    this.index = Math.Min(this.index + IndexIncrement, this.maxIndex);
                    this.nextReceiveBufferSize = SizeTable[this.index];
                    this.decreaseNow = false;
                }
            }

            public override void ReadComplete() => this.Record(this.TotalBytesRead());
        }

        private readonly int minIndex;
        private readonly int maxIndex;
        private readonly int initial;

        public AdaptiveRecvByteBufAllocator() : this(DefaultMinimum, DefaultInitial, DefaultMaximum)
        {
        }

        public AdaptiveRecvByteBufAllocator(int minimum, int initial, int maximum)
        {
            Contract.Requires(minimum > 0);
            Contract.Requires(initial >= minimum);
            Contract.Requires(maximum >= initial);

            int min = GetSizeTableIndex(minimum);
            if (SizeTable[min] < minimum)
            {
                this.minIndex = min + 1;
            }
            else
            {
                this.minIndex = min;
            }

            int max = GetSizeTableIndex(maximum);
            if (SizeTable[max] > maximum)
            {
                this.maxIndex = max - 1;
            }
            else
            {
                this.maxIndex = max;
            }

            this.initial = initial;
        }

        public override IRecvByteBufAllocatorHandle NewHandle() => new HandleImpl(this, this.minIndex, this.maxIndex, this.initial);
    }
}
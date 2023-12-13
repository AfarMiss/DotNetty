using System;
using System.Runtime.CompilerServices;
using DotNetty.Common.Internal;

namespace DotNetty.Buffers
{
    public class SlicedByteBuffer : AbstractDerivedByteBuffer
    {
        #region IByteBuffer

        private readonly IByteBuffer buffer;
        private readonly int adjustment;
        internal int Adjustment => this.adjustment;
        public override int Capacity => this.MaxCapacity;

        public SlicedByteBuffer(IByteBuffer buffer, int index, int length) : base(length)
        {
            CheckSliceOutOfBounds(index, length, buffer);

            if (buffer is SlicedByteBuffer byteBuffer)
            {
                this.buffer = byteBuffer.buffer;
                this.adjustment = byteBuffer.adjustment + index;
            }
            else if (buffer is DuplicateByteBuffer)
            {
                this.buffer = buffer.Unwrap();
                this.adjustment = index;
            }
            else
            {
                this.buffer = buffer;
                this.adjustment = index;
            }

            this.SetWriterIndex0(length);
        }

        internal int Length => this.Capacity;

        public override IByteBuffer Unwrap() => this.buffer;

        public override IByteBufferAllocator Allocator => this.Unwrap().Allocator;

        public override IByteBuffer AdjustCapacity(int newCapacity) => throw new NotSupportedException("sliced buffer");

        public override bool HasArray => this.Unwrap().HasArray;

        public override byte[] Array => this.Unwrap().Array;

        public override int ArrayOffset => this.Idx(this.Unwrap().ArrayOffset);

        public override bool HasMemoryAddress => this.Unwrap().HasMemoryAddress;

        public override ref byte GetPinnableMemoryAddress() => ref Unsafe.Add(ref this.Unwrap().GetPinnableMemoryAddress(), this.adjustment);

        public override IntPtr AddressOfPinnedMemory()
        {
            IntPtr ptr = this.Unwrap().AddressOfPinnedMemory();
            if (ptr == IntPtr.Zero)
            {
                return ptr;
            }
            return ptr + this.adjustment;
        }

        public override IByteBuffer Duplicate()
        {
            this.Unwrap().Duplicate().SetIndex(this.Idx(this.ReaderIndex), this.Idx(this.WriterIndex));
            return this;
        }

        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex0(index, length);
            return this.Unwrap().Copy(this.Idx(index), length);
        }

        public override IByteBuffer Slice(int index, int length)
        {
            this.CheckIndex0(index, length);
            return this.Unwrap().Slice(this.Idx(index), length);
        }

        public override int IoBufferCount => this.Unwrap().IoBufferCount;

        public override ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.CheckIndex0(index, length);
            return this.Unwrap().GetIoBuffer(index + this.adjustment, length);
        }

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length)
        {
            this.CheckIndex0(index, length);
            return this.Unwrap().GetIoBuffers(index + this.adjustment, length);
        }

        // Returns the index with the needed adjustment.
        protected internal int Idx(int index) => index + this.adjustment;
        
        internal static void CheckSliceOutOfBounds(int index, int length, IByteBuffer buffer)
        {
            if (MathUtil.IsOutOfBounds(index, length, buffer.Capacity))
            {
                throw new IndexOutOfRangeException($"{buffer}.Slice({index}, {length})");
            }
        }
        protected AbstractByteBuffer UnwrapCore() => (AbstractByteBuffer)this.Unwrap();


        #endregion

        public override unsafe T Get<T>(int index)
        {
            this.CheckIndex0(index, sizeof(T));
            return this.Unwrap().Get<T>(this.Idx(index));
        }
        protected internal override T _Get<T>(int index) => this.UnwrapCore()._Get<T>(this.Idx(index));
        public override unsafe void Set<T>(int index, T value)
        {
            this.CheckIndex0(index, sizeof(T));
            this.Unwrap().Set<T>(this.Idx(index), value);
        }
        protected internal override void _Set<T>(int index, T value) => this.UnwrapCore()._Set<T>(this.Idx(index), value);

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckIndex0(index, length);
            this.Unwrap().GetBytes(this.Idx(index), dst, dstIndex, length);
        }
        
        public override void GetBytes(int index, Span<byte> dst, int dstIndex, int length)
        {
            this.CheckIndex0(index, length);
            this.Unwrap().GetBytes(this.Idx(index), dst, dstIndex, length);
        }
        
        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.CheckIndex0(index, length);
            this.Unwrap().SetBytes(this.Idx(index), src, srcIndex, length);
        }
        
        public override void SetBytes(int index, Span<byte> src, int srcIndex, int length)
        {
            this.CheckIndex0(index, length);
            this.Unwrap().SetBytes(this.Idx(index), src, srcIndex, length);
        }
    }
}

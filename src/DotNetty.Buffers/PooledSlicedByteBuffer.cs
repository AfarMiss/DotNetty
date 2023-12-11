// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;
    using static AbstractUnpooledSlicedByteBuffer;

    sealed class PooledSlicedByteBuffer : AbstractPooledDerivedByteBuffer<PooledSlicedByteBuffer>
    {
        #region IByteBuffer

        internal static PooledSlicedByteBuffer NewInstance(AbstractByteBuffer unwrapped, IByteBuffer wrapped, int index, int length)
        {
            CheckSliceOutOfBounds(index, length, unwrapped);
            return NewInstance0(unwrapped, wrapped, index, length);
        }

        static PooledSlicedByteBuffer NewInstance0(AbstractByteBuffer unwrapped, IByteBuffer wrapped, int adjustment, int length)
        {
            PooledSlicedByteBuffer slice = Recycler.Acquire(out var handle);
            slice.handle = handle;
            slice.Init(unwrapped, wrapped, 0, length, length);
            slice.DiscardMarks();
            slice.adjustment = adjustment;

            return slice;
        }

        int adjustment;

        public override int Capacity => this.MaxCapacity;

        public override IByteBuffer AdjustCapacity(int newCapacity) => throw new NotSupportedException("sliced buffer");

        public override int ArrayOffset => this.Idx(this.Unwrap().ArrayOffset);

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

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length)
        {
            this.CheckIndex0(index, length);
            return this.Unwrap().GetIoBuffers(this.Idx(index), length);
        }

        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex0(index, length);
            return this.Unwrap().Copy(this.Idx(index), length);
        }

        public override IByteBuffer Slice(int index, int length)
        {
            this.CheckIndex0(index, length);
            return base.Slice(this.Idx(index), length);
        }

        public override IByteBuffer RetainedSlice(int index, int length)
        {
            this.CheckIndex0(index, length);
            return NewInstance0(this.UnwrapCore(), this, this.Idx(index), length);
        }

        public override IByteBuffer Duplicate() => this.Duplicate0().SetIndex(this.Idx(this.ReaderIndex), this.Idx(this.WriterIndex));

        public override IByteBuffer RetainedDuplicate() => PooledDuplicatedByteBuffer.NewInstance(this.UnwrapCore(), this, this.Idx(this.ReaderIndex), this.Idx(this.WriterIndex));
        
        public override int ForEachByte(int index, int length, IByteProcessor processor)
        {
            this.CheckIndex0(index, length);
            int ret = this.Unwrap().ForEachByte(this.Idx(index), length, processor);
            if (ret < this.adjustment)
            {
                return -1;
            }
            return ret - this.adjustment;
        }

        public override int ForEachByteDesc(int index, int length, IByteProcessor processor)
        {
            this.CheckIndex0(index, length);
            int ret = this.Unwrap().ForEachByteDesc(this.Idx(index), length, processor);
            if (ret < this.adjustment)
            {
                return -1;
            }
            return ret - this.adjustment;
        }

        int Idx(int index) => index + this.adjustment;

        #endregion

        #region IByteBufferProvider

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

        public override void GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            this.CheckIndex0(index, length);
            this.Unwrap().GetBytes(this.Idx(index), dst, dstIndex, length);
        }

        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.CheckIndex0(index, length);
            this.Unwrap().SetBytes(this.Idx(index), src, srcIndex, length);
        }

        public override void SetBytes(int index, byte[] src, int srcIndex, int length)
        {
            this.CheckIndex0(index, length);
            this.Unwrap().SetBytes(this.Idx(index), src, srcIndex, length);
        }

        public override ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.CheckIndex0(index, length);
            return this.Unwrap().GetIoBuffer(this.Idx(index), length);
        }

        #endregion
    }
}
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common.Internal;

    sealed class PooledHeapByteBuffer : PooledByteBuffer<byte[]>
    {
        #region IByteBuffer

        internal static PooledHeapByteBuffer NewInstance(int maxCapacity)
        {
            return new PooledHeapByteBuffer(maxCapacity);
        }

        internal PooledHeapByteBuffer(int maxCapacity) : base(maxCapacity)
        {
        }

        public override bool IsDirect => false;

        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            IByteBuffer copy = this.Allocator.HeapBuffer(length, this.MaxCapacity);
            copy.WriteBytes(this.Memory, this.Idx(index), length);
            return copy;
        }

        public override int IoBufferCount => 1;

        public override ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.CheckIndex(index, length);
            index = index + this.Offset;
            return new ArraySegment<byte>(this.Memory, index, length);
        }

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length) => new[] { this.GetIoBuffer(index, length) };

        public override bool HasArray => true;

        public override byte[] Array
        {
            get
            {
                this.EnsureAccessible();
                return this.Memory;
            }
        }

        public override int ArrayOffset => this.Offset;

        public override bool HasMemoryAddress => true;

        public override ref byte GetPinnableMemoryAddress()
        {
            this.EnsureAccessible();
            return ref this.Memory[this.Offset];
        }

        public override IntPtr AddressOfPinnedMemory() => IntPtr.Zero;

        #endregion

        #region IByteBufferProvider

        protected internal override T _Get<T>(int index) => ByteBufferEx.Read<T>(this.Memory, this.Idx(index));

        protected internal override void _Set<T>(int index, T value) => ByteBufferEx.Write<T>(this.Memory, this.Idx(index), value);

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Capacity);
            if (dst.HasArray)
            {
                this.GetBytes(index, dst.Array, dst.ArrayOffset + dstIndex, length);
            }
            else
            {
                dst.SetBytes(dstIndex, this.Memory, this.Idx(index), length);
            }
        }

        public override void GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Length);
            PlatformDependent.CopyMemory(this.Memory, this.Idx(index), dst, dstIndex, length);
        }

        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Capacity);
            if (src.HasArray)
            {
                this.SetBytes(index, src.Array, src.ArrayOffset + srcIndex, length);
            }
            else
            {
                src.GetBytes(srcIndex, this.Memory, this.Idx(index), length);
            }
        }

        public override void SetBytes(int index, byte[] src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Length);
            PlatformDependent.CopyMemory(src, srcIndex, this.Memory, this.Idx(index), length);
        }

        #endregion
    }
}
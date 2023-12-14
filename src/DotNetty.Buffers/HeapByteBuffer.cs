using System.Buffers;
using System.Runtime.CompilerServices;
using System;
using System.Diagnostics.Contracts;
using DotNetty.Common.Internal;

namespace DotNetty.Buffers
{
    public sealed class HeapByteBuffer : AbstractRefByteBuffer
    {
        #region IByteBuffer

        private byte[] array;
        private bool isPool;
        private readonly int capacity;

        public override int Capacity
        {
            get
            {
                this.EnsureAccessible();
                return this.capacity;
            }
        }
        
        public override bool HasArray => true;

        public override byte[] Array
        {
            get
            {
                this.EnsureAccessible();
                return this.array;
            }
        }

        public override int ArrayOffset => 0;

        public override bool HasMemoryAddress => true;
        public override int IoBufferCount => 1;

        internal HeapByteBuffer(IByteBufferAllocator alloc, int initialCapacity, int maxCapacity)
            : base(maxCapacity)
        {
            Contract.Requires(alloc != null);
            Contract.Requires(initialCapacity <= maxCapacity);

            this.capacity = initialCapacity;
            this.SetArray(this.NewArray(initialCapacity));
            this.SetIndex0(0, 0);
        }

        internal HeapByteBuffer(IByteBufferAllocator alloc, byte[] initialArray, int maxCapacity)
            : base(maxCapacity)
        {
            Contract.Requires(alloc != null);
            Contract.Requires(initialArray != null);

            if (initialArray.Length > maxCapacity)
            {
                throw new ArgumentException($"initialCapacity({initialArray.Length}) > maxCapacity({maxCapacity})");
            }

            this.capacity = initialArray.Length;
            this.SetArray(initialArray);
            this.SetIndex0(0, initialArray.Length);
        }

        private byte[] AllocateArray(int initialCapacity) => this.NewArray(initialCapacity);

        private byte[] NewArray(int initialCapacity)
        {
            isPool = true;
            return ArrayPool<byte>.Shared.Rent(initialCapacity);
        }

        private void FreeArray(byte[] bytes)
        {
            if (isPool)
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        private void SetArray(byte[] initialArray) => this.array = initialArray;

        public override IByteBuffer AdjustCapacity(int newCapacity)
        {
            this.CheckNewCapacity(newCapacity);

            int oldCapacity = this.array.Length;
            byte[] oldArray = this.array;
            if (newCapacity > oldCapacity)
            {
                byte[] newArray = this.AllocateArray(newCapacity);
                PlatformDependent.CopyMemory(this.array, 0, newArray, 0, oldCapacity);

                this.SetArray(newArray);
                this.FreeArray(oldArray);
            }
            else if (newCapacity < oldCapacity)
            {
                byte[] newArray = this.AllocateArray(newCapacity);
                int readerIndex = this.ReaderIndex;
                if (readerIndex < newCapacity)
                {
                    int writerIndex = this.WriterIndex;
                    if (writerIndex > newCapacity)
                    {
                        this.SetWriterIndex0(writerIndex = newCapacity);
                    }

                    PlatformDependent.CopyMemory(this.array, readerIndex, newArray, 0, writerIndex - readerIndex);
                }
                else
                {
                    this.SetIndex(newCapacity, newCapacity);
                }

                this.SetArray(newArray);
                this.FreeArray(oldArray);
            }
            return this;
        }

        public override ref byte GetPinnableMemoryAddress()
        {
            this.EnsureAccessible();
            return ref this.array[0];
        }

        public override IntPtr AddressOfPinnedMemory() => IntPtr.Zero;
        
        public override ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.EnsureAccessible();
            return new ArraySegment<byte>(this.array, index, length);
        }

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length) => new[] { this.GetIoBuffer(index, length) };

        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            var copiedArray = new byte[length];
            PlatformDependent.CopyMemory(this.array, index, copiedArray, 0, length);

            return new HeapByteBuffer(ByteBuffer.Allocator, copiedArray, this.MaxCapacity);
        }

        protected internal override void Deallocate()
        {
            this.FreeArray(this.array);
            this.array = null;
        }

        public override IByteBuffer Unwrap() => null;

        #endregion

        #region IByteBufferProvider

        public override T Get<T>(int index)
        {
            this.EnsureAccessible();
            return this._Get<T>(index);
        }
        protected internal override T _Get<T>(int index) => ByteBuffer.Read<T>(this.array, index);

        public override void Set<T>(int index, T value)
        {
            this.EnsureAccessible();
            this._Set<T>(index, value);
        }
        protected internal override void _Set<T>(int index, T value)
        {
            ByteBuffer.Write<T>(this.array, index, value);
        }

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Capacity);
            if (dst.HasArray)
            {
                this.GetBytes(index, dst.Array, dst.ArrayOffset + dstIndex, length);
            }
            else
            {
                dst.SetBytes(dstIndex, this.array, index, length);
            }
        }

        public override void GetBytes(int index, Span<byte> dst, int dstIndex, int length)
        {
            this.CheckDstIndex(index, length, dstIndex, dst.Length);
            if (length > 0)
            {
                Unsafe.CopyBlockUnaligned(ref dst[dstIndex], ref this.array[index], (uint)length);
            }
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
                src.GetBytes(srcIndex, this.array, index, length);
            }
        }

        public override void SetBytes(int index, Span<byte> src, int srcIndex, int length)
        {
            this.CheckSrcIndex(index, length, srcIndex, src.Length);
            if (length > 0)
            {
                Unsafe.CopyBlockUnaligned(ref this.array[index], ref src[srcIndex], (uint)length);
            }
        }

        #endregion
    }
}
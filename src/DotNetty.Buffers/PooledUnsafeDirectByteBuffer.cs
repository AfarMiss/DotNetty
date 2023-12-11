namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    sealed unsafe class PooledUnsafeDirectByteBuffer : PooledByteBuffer<byte[]>
    {
        #region IByteBuffer

        byte* memoryAddress;

        internal static PooledUnsafeDirectByteBuffer NewInstance(int maxCapacity)
        {
            return new PooledUnsafeDirectByteBuffer(maxCapacity);
        }

        PooledUnsafeDirectByteBuffer(int maxCapacity) : base(maxCapacity)
        {
        }

        internal override void Init(PoolChunk<byte[]> chunk, long handle, int offset, int length, int maxLength,
            PoolThreadCache<byte[]> cache)
        {
            base.Init(chunk, handle, offset, length, maxLength, cache);
            this.InitMemoryAddress();
        }

        internal override void InitUnpooled(PoolChunk<byte[]> chunk, int length)
        {
            base.InitUnpooled(chunk, length);
            this.InitMemoryAddress();
        }

        void InitMemoryAddress()
        {
            this.memoryAddress = (byte*)Unsafe.AsPointer(ref this.Memory[this.Offset]);
        }

        public override bool IsDirect => true;

        internal void Reuse(int maxCapacity)
        {
            this.SetMaxCapacity(maxCapacity);
            this.SetReferenceCount(1);
            this.SetIndex0(0, 0);
            this.DiscardMarks();
        }
        
        public override IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            return UnsafeByteBufferUtil.Copy(this, this.Addr(index), index, length);
        }

        public override int IoBufferCount => 1;

        public override ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.CheckIndex(index, length);
            index = this.Idx(index);
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

        public override IntPtr AddressOfPinnedMemory() => (IntPtr)this.memoryAddress;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte* Addr(int index) => this.memoryAddress + index;

        #endregion

        #region MyRegion

        protected internal override T _Get<T>(int index) => ByteBufferEx.Read<T>(this.Memory, index + this.Offset);

        protected internal override void _Set<T>(int index, T value) => ByteBufferEx.Write<T>(this.Memory, index + this.Offset, value);

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            UnsafeByteBufferUtil.GetBytes(this, this.Addr(index), index, dst, dstIndex, length);
        }

        public override void GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            this.CheckIndex(index, length);
            UnsafeByteBufferUtil.GetBytes(this, this.Addr(index), index, dst, dstIndex, length);
        }

        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            UnsafeByteBufferUtil.SetBytes(this, this.Addr(index), index, src, srcIndex, length);
        }

        public override void SetBytes(int index, byte[] src, int srcIndex, int length)
        {
            this.CheckIndex(index, length);
            UnsafeByteBufferUtil.SetBytes(this, this.Addr(index), index, src, srcIndex, length);
        }

        #endregion
    }
}

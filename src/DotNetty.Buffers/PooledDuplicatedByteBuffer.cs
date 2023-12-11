namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common;

    sealed class PooledDuplicatedByteBuffer : AbstractPooledDerivedByteBuffer<PooledDuplicatedByteBuffer>
    {
        #region IByteBuffer

        internal static PooledDuplicatedByteBuffer NewInstance(AbstractByteBuffer unwrapped, IByteBuffer wrapped, int readerIndex, int writerIndex)
        {
            PooledDuplicatedByteBuffer duplicate = Recycler.Acquire(out var handle);
            duplicate.handle = handle;
            duplicate.Init(unwrapped, wrapped, readerIndex, writerIndex, unwrapped.MaxCapacity);
            duplicate.MarkReaderIndex();
            duplicate.MarkWriterIndex();

            return duplicate;
        }

        public override int Capacity => this.Unwrap().Capacity;

        public override IByteBuffer AdjustCapacity(int newCapacity)
        {
            this.Unwrap().AdjustCapacity(newCapacity);
            return this;
        }

        public override int ArrayOffset => this.Unwrap().ArrayOffset;

        public override ref byte GetPinnableMemoryAddress() => ref this.Unwrap().GetPinnableMemoryAddress();

        public override IntPtr AddressOfPinnedMemory() => this.Unwrap().AddressOfPinnedMemory();

        public override ArraySegment<byte> GetIoBuffer(int index, int length) => this.Unwrap().GetIoBuffer(index, length);

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length) => this.Unwrap().GetIoBuffers(index, length);

        public override IByteBuffer Copy(int index, int length) => this.Unwrap().Copy(index, length);

        public override IByteBuffer RetainedSlice(int index, int length) => PooledSlicedByteBuffer.NewInstance(this.UnwrapCore(), this, index, length);

        public override IByteBuffer Duplicate() => this.Duplicate0().SetIndex(this.ReaderIndex, this.WriterIndex);

        public override IByteBuffer RetainedDuplicate() => NewInstance(this.UnwrapCore(), this, this.ReaderIndex, this.WriterIndex);

        #endregion

        #region MyRegion

        protected internal override T _Get<T>(int index) => this.UnwrapCore()._Get<T>(index);

        protected internal override void _Set<T>(int index, T value) => this.UnwrapCore()._Set<T>(index, value);

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length) => this.Unwrap().GetBytes(index, dst, dstIndex, length);

        public override void GetBytes(int index, byte[] dst, int dstIndex, int length) => this.Unwrap().GetBytes(index, dst, dstIndex, length);

        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length) => this.Unwrap().SetBytes(index, src, srcIndex, length);

        public override void SetBytes(int index, byte[] src, int srcIndex, int length) => this.Unwrap().SetBytes(index, src, srcIndex, length);

        #endregion
    }
}
namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common.Utilities;

    class UnpooledDuplicatedByteBuffer : AbstractDerivedByteBuffer
    {
        #region IByteBuffer

        readonly AbstractByteBuffer buffer;

        public UnpooledDuplicatedByteBuffer(AbstractByteBuffer buffer) : this(buffer, buffer.ReaderIndex, buffer.WriterIndex)
        {
        }

        internal UnpooledDuplicatedByteBuffer(AbstractByteBuffer buffer, int readerIndex, int writerIndex)
            : base(buffer.MaxCapacity)
        {
            if (buffer is UnpooledDuplicatedByteBuffer duplicated)
            {
                this.buffer = duplicated.buffer;
            }
            else if (buffer is AbstractPooledDerivedByteBuffer)
            {
                this.buffer = (AbstractByteBuffer)buffer.Unwrap();
            }
            else
            {
                this.buffer = buffer;
            }

            this.SetIndex0(readerIndex, writerIndex);
            this.MarkIndex(); // Mark read and writer index
        }

        public override IByteBuffer Unwrap() => this.UnwrapCore();

        public override IByteBuffer Copy(int index, int length) => this.Unwrap().Copy(index, length);

        protected AbstractByteBuffer UnwrapCore() => this.buffer;

        public override IByteBufferAllocator Allocator => this.Unwrap().Allocator;

        public override bool IsDirect => this.Unwrap().IsDirect;

        public override int Capacity => this.Unwrap().Capacity;

        public override IByteBuffer AdjustCapacity(int newCapacity) => this.Unwrap().AdjustCapacity(newCapacity);

        public override int IoBufferCount => this.Unwrap().IoBufferCount;

        public override bool HasArray => this.Unwrap().HasArray;

        public override byte[] Array => this.Unwrap().Array;

        public override int ArrayOffset => this.Unwrap().ArrayOffset;

        public override bool HasMemoryAddress => this.Unwrap().HasMemoryAddress;

        public override ref byte GetPinnableMemoryAddress() => ref this.Unwrap().GetPinnableMemoryAddress();

        public override IntPtr AddressOfPinnedMemory() => this.Unwrap().AddressOfPinnedMemory();

        public override int ForEachByte(int index, int length, IByteProcessor processor) => this.Unwrap().ForEachByte(index, length, processor);

        public override int ForEachByteDesc(int index, int length, IByteProcessor processor) => this.Unwrap().ForEachByteDesc(index, length, processor);

        #endregion

        #region IByteBufferProvider

        protected internal override T _Get<T>(int index) => this.UnwrapCore()._Get<T>(index);

        protected internal override void _Set<T>(int index, T value) => this.UnwrapCore()._Set<T>(index, value);

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.Unwrap().GetBytes(index, dst, dstIndex, length);
        }

        public override void GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            this.Unwrap().GetBytes(index, dst, dstIndex, length);
        }

        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.Unwrap().SetBytes(index, src, srcIndex, length);
        }

        public override void SetBytes(int index, byte[] src, int srcIndex, int length)
        {
            this.Unwrap().SetBytes(index, src, srcIndex, length);
        }

        #endregion
    }
}

using System;

namespace DotNetty.Buffers
{
    public sealed class DuplicateByteBuffer : AbstractDerivedByteBuffer
    {
        #region IByteBuffer

        private readonly AbstractByteBuffer buffer;

        public DuplicateByteBuffer(AbstractByteBuffer buffer) : this(buffer, buffer.ReaderIndex, buffer.WriterIndex)
        {
        }

        private DuplicateByteBuffer(AbstractByteBuffer buffer, int readerIndex, int writerIndex)
            : base(buffer.MaxCapacity)
        {
            this.buffer = buffer is DuplicateByteBuffer duplicated ? duplicated.buffer : buffer;
            this.SetIndex0(readerIndex, writerIndex);
            this.MarkIndex(); // Mark read and writer index
        }

        public override IByteBuffer Unwrap() => this.UnwrapCore();

        public override IByteBuffer Copy(int index, int length) => this.Unwrap().Copy(index, length);

        private AbstractByteBuffer UnwrapCore() => this.buffer;

        public override IByteBufferAllocator Allocator => this.Unwrap().Allocator;

        public override int Capacity => this.Unwrap().Capacity;

        public override IByteBuffer AdjustCapacity(int newCapacity) => this.Unwrap().AdjustCapacity(newCapacity);

        public override int IoBufferCount => this.Unwrap().IoBufferCount;

        public override bool HasArray => this.Unwrap().HasArray;

        public override byte[] Array => this.Unwrap().Array;

        public override int ArrayOffset => this.Unwrap().ArrayOffset;

        public override bool HasMemoryAddress => this.Unwrap().HasMemoryAddress;

        public override ref byte GetPinnableMemoryAddress() => ref this.Unwrap().GetPinnableMemoryAddress();

        public override IntPtr AddressOfPinnedMemory() => this.Unwrap().AddressOfPinnedMemory();

        #endregion

        #region IByteBufferProvider

        protected internal override T _Get<T>(int index) => this.UnwrapCore()._Get<T>(index);

        protected internal override void _Set<T>(int index, T value) => this.UnwrapCore()._Set<T>(index, value);

        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            this.Unwrap().GetBytes(index, dst, dstIndex, length);
        }

        public override void GetBytes(int index, Span<byte> dst, int dstIndex, int length)
        {
            this.Unwrap().GetBytes(index, dst, dstIndex, length);
        }

        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            this.Unwrap().SetBytes(index, src, srcIndex, length);
        }

        public override void SetBytes(int index, Span<byte> src, int srcIndex, int length)
        {
            this.Unwrap().SetBytes(index, src, srcIndex, length);
        }

        #endregion
    }
}

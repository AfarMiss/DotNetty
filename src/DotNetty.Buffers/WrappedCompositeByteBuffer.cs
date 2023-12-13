using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Common;

namespace DotNetty.Buffers
{
    public class WrappedCompositeByteBuffer : CompositeByteBuffer
    {
        #region IByteBuffer

        private readonly CompositeByteBuffer wrapped;
        public sealed override int ReaderIndex => this.wrapped.ReaderIndex;
        public sealed override int WriterIndex => this.wrapped.WriterIndex;
        public sealed override int ReadableBytes => this.wrapped.ReadableBytes;
        public sealed override int WritableBytes => this.wrapped.WritableBytes;
        public sealed override int MaxWritableBytes => this.wrapped.MaxWritableBytes;
        public sealed override int ReferenceCount => this.wrapped.ReferenceCount;

        public sealed override bool HasArray => this.wrapped.HasArray;

        public sealed override byte[] Array => this.wrapped.Array;

        public sealed override int ArrayOffset => this.wrapped.ArrayOffset;

        public sealed override int Capacity => this.wrapped.Capacity;
        
        public sealed override int NumComponents => this.wrapped.NumComponents;

        public sealed override int MaxNumComponents => this.wrapped.MaxNumComponents;
        public override int IoBufferCount => this.wrapped.IoBufferCount;
        public sealed override bool HasMemoryAddress => this.wrapped.HasMemoryAddress;
        public sealed override int MaxCapacity => this.wrapped.MaxCapacity;
        
        internal WrappedCompositeByteBuffer(CompositeByteBuffer wrapped) : base(Unpooled.Allocator)
        {
            this.wrapped = wrapped;
            this.SetMaxCapacity(this.wrapped.MaxCapacity);
        }

        public override bool Release(int decrement = 1) => this.wrapped.Release(decrement);

        public sealed override bool IsReadable() => this.wrapped.IsReadable();
        public sealed override bool IsReadable(int numBytes) => this.wrapped.IsReadable(numBytes);
        public sealed override bool IsWritable() => this.wrapped.IsWritable();

        public override int EnsureWritable(int minWritableBytes, bool force) => this.wrapped.EnsureWritable(minWritableBytes, force);

        public override IByteBuffer Slice() => this.wrapped.Slice();

        public override IByteBuffer Slice(int index, int length) => this.wrapped.Slice(index, length);

        public override string ToString(Encoding encoding) => this.wrapped.ToString(encoding);

        public override string ToString(int index, int length, Encoding encoding) => this.wrapped.ToString(index, length, encoding);

        public sealed override int GetHashCode() => this.wrapped.GetHashCode();

        public sealed override bool Equals(IByteBuffer buf) => this.wrapped.Equals(buf);

        public sealed override int CompareTo(IByteBuffer that) => this.wrapped.CompareTo(that);

        public override IByteBuffer Duplicate() => this.wrapped.Duplicate();

        public override IByteBuffer ReadSlice(int length) => this.wrapped.ReadSlice(length);

        public override CompositeByteBuffer AddComponent(IByteBuffer buffer)
        {
            this.wrapped.AddComponent(buffer);
            return this;
        }

        public override CompositeByteBuffer AddComponents(params IByteBuffer[] buffers)
        {
            this.wrapped.AddComponents(buffers);
            return this;
        }

        public override CompositeByteBuffer AddComponents(IEnumerable<IByteBuffer> buffers)
        {
            this.wrapped.AddComponents(buffers);
            return this;
        }

        public override CompositeByteBuffer AddComponent(int cIndex, IByteBuffer buffer)
        {
            this.wrapped.AddComponent(cIndex, buffer);
            return this;
        }

        public override CompositeByteBuffer AddComponents(int cIndex, params IByteBuffer[] buffers)
        {
            this.wrapped.AddComponents(cIndex, buffers);
            return this;
        }

        public override CompositeByteBuffer AddComponents(int cIndex, IEnumerable<IByteBuffer> buffers)
        {
            this.wrapped.AddComponents(cIndex, buffers);
            return this;
        }

        public override CompositeByteBuffer AddComponent(bool increaseWriterIndex, IByteBuffer buffer)
        {
            this.wrapped.AddComponent(increaseWriterIndex, buffer);
            return this;
        }

        public override CompositeByteBuffer AddComponents(bool increaseWriterIndex, params IByteBuffer[] buffers)
        {
            this.wrapped.AddComponents(increaseWriterIndex, buffers);
            return this;
        }

        public override CompositeByteBuffer AddComponents(bool increaseWriterIndex, IEnumerable<IByteBuffer> buffers)
        {
            this.wrapped.AddComponents(increaseWriterIndex, buffers);
            return this;
        }

        public override CompositeByteBuffer AddComponent(bool increaseWriterIndex, int cIndex, IByteBuffer buffer)
        {
            this.wrapped.AddComponent(increaseWriterIndex, cIndex, buffer);
            return this;
        }

        public override CompositeByteBuffer RemoveComponent(int cIndex)
        {
            this.wrapped.RemoveComponent(cIndex);
            return this;
        }

        public override CompositeByteBuffer RemoveComponents(int cIndex, int numComponents)
        {
            this.wrapped.RemoveComponents(cIndex, numComponents);
            return this;
        }

        public override IEnumerator<IByteBuffer> GetEnumerator() => this.wrapped.GetEnumerator();

        public override IList<IByteBuffer> Decompose(int offset, int length) => this.wrapped.Decompose(offset, length);

        public override IByteBuffer AdjustCapacity(int newCapacity)
        {
            this.wrapped.AdjustCapacity(newCapacity);
            return this;
        }

        public sealed override int ToComponentIndex(int offset) => this.wrapped.ToComponentIndex(offset);

        public sealed override int ToByteIndex(int cIndex) => this.wrapped.ToByteIndex(cIndex);

        // public override Task<int> SetBytesAsync(int index, Stream src, int length, CancellationToken cancellationToken) => this.wrapped.SetBytesAsync(index, src, length, cancellationToken);

        public override IByteBuffer Copy() => this.wrapped.Copy();

        public override IByteBuffer Copy(int index, int length) => this.wrapped.Copy(index, length);

        public sealed override IByteBuffer this[int cIndex] => this.wrapped[cIndex];

        public sealed override IByteBuffer ComponentAtOffset(int offset) => this.wrapped.ComponentAtOffset(offset);

        public sealed override IByteBuffer InternalComponent(int cIndex) => this.wrapped.InternalComponent(cIndex);

        public sealed override IByteBuffer InternalComponentAtOffset(int offset) => this.wrapped.InternalComponentAtOffset(offset);

        public override ArraySegment<byte> GetIoBuffer(int index, int length) => this.wrapped.GetIoBuffer(index, length);

        public override ArraySegment<byte>[] GetIoBuffers(int index, int length) => this.wrapped.GetIoBuffers(index, length);

        public override CompositeByteBuffer Consolidate()
        {
            this.wrapped.Consolidate();
            return this;
        }

        public override CompositeByteBuffer Consolidate(int cIndex, int numComponents)
        {
            this.wrapped.Consolidate(cIndex, numComponents);
            return this;
        }

        public override CompositeByteBuffer DiscardReadComponents()
        {
            this.wrapped.DiscardReadComponents();
            return this;
        }

        public override IByteBuffer DiscardReadBytes()
        {
            this.wrapped.DiscardReadBytes();
            return this;
        }

        public sealed override string ToString() => this.wrapped.ToString();

        public sealed override void SetReaderIndex(int readerIndex) => this.wrapped.SetReaderIndex(readerIndex);
        public sealed override void SetWriterIndex(int writerIndex) => this.wrapped.SetWriterIndex(writerIndex);
        public sealed override void SetIndex(int readerIndex, int writerIndex) => this.wrapped.SetIndex(readerIndex, writerIndex);

        public sealed override void ResetIndex() => this.wrapped.ResetIndex();

        public sealed override void MarkReaderIndex() => this.wrapped.MarkReaderIndex();

        public sealed override void ResetReaderIndex() => this.wrapped.ResetReaderIndex();

        public sealed override void MarkWriterIndex() => this.wrapped.MarkWriterIndex();

        public sealed override void ResetWriterIndex() => this.wrapped.ResetWriterIndex();

        public override IByteBuffer EnsureWritable(int minWritableBytes)
        {
            this.wrapped.EnsureWritable(minWritableBytes);
            return this;
        }

        public override IReferenceCounted Retain(int increment = 1)
        {
            this.wrapped.Retain(increment);
            return this;
        }

        public override IByteBuffer DiscardSomeReadBytes()
        {
            this.wrapped.DiscardSomeReadBytes();
            return this;
        }

        protected internal sealed override void Deallocate() => this.wrapped.Deallocate();

        public sealed override IByteBuffer Unwrap() => this.wrapped;

        public sealed override IntPtr AddressOfPinnedMemory() => this.wrapped.AddressOfPinnedMemory();

        public sealed override ref byte GetPinnableMemoryAddress() => ref this.wrapped.GetPinnableMemoryAddress();

        public sealed override bool IsWritable(int size) => this.wrapped.IsWritable(size);

        public override IByteBuffer ReadRetainedSlice(int length) => this.wrapped.ReadRetainedSlice(length);

        public override IByteBuffer RetainedDuplicate() => this.wrapped.RetainedDuplicate();

        public override IByteBuffer RetainedSlice() => this.wrapped.RetainedSlice();

        public override IByteBuffer RetainedSlice(int index, int length) => this.wrapped.RetainedSlice(index, length);

        #endregion

        public override T Get<T>(int index) => this.wrapped.Get<T>(index);
        public override void Set<T>(int index, T value) => this.wrapped.Set(index, value);
        public override T Read<T>() => this.wrapped.Read<T>();
        public override void Write<T>(T value) => this.wrapped.Write(value);

        public override void Set<T>(int index, int value) => this.wrapped.Set<T>(index, value);
        public override void Write<T>(int value) => this.wrapped.Write<T>(value);

        public override void GetBytes(int index, IByteBuffer dst, int? length = null) => this.wrapped.GetBytes(index, dst, length);
        public override void GetBytes(int index, IByteBuffer dst, int dstIndex, int length) => this.wrapped.GetBytes(index, dst, dstIndex, length);
        public override void GetBytes(int index, Span<byte> dst, int? length = null) => this.wrapped.GetBytes(index, dst, length);
        public override void GetBytes(int index, Span<byte> dst, int dstIndex, int length) => this.wrapped.GetBytes(index, dst, dstIndex, length);

        public override void SetBytes(int index, IByteBuffer src, int? length = null) => this.wrapped.SetBytes(index, src, length);
        public override void SetBytes(int index, IByteBuffer src, int srcIndex, int length) => this.wrapped.SetBytes(index, src, srcIndex, length);
        public override void SetBytes(int index, Span<byte> src, int? length = null) => this.wrapped.SetBytes(index, src, length);
        public override void SetBytes(int index, Span<byte> src, int srcIndex, int length) => this.wrapped.SetBytes(index, src, srcIndex, length);
        
        public override void SkipBytes(int length) => this.wrapped.SkipBytes(length);
        public override void ReadBytes(IByteBuffer dst, int? length = null) => this.wrapped.ReadBytes(dst, length);
        public override void ReadBytes(IByteBuffer dst, int dstIndex, int length) => this.wrapped.ReadBytes(dst, dstIndex, length);
        public override void ReadBytes(Span<byte> dst, int? length = null) => this.wrapped.ReadBytes(dst, length);
        public override void ReadBytes(Span<byte> dst, int dstIndex, int length) => this.wrapped.ReadBytes(dst, dstIndex, length);
        public override void WriteBytes(IByteBuffer src, int? length = null) => this.wrapped.WriteBytes(src, length);
        public override void WriteBytes(IByteBuffer src, int srcIndex, int length) => this.wrapped.WriteBytes(src, srcIndex, length);
        public override void WriteBytes(Span<byte> src, int? length = null) => this.wrapped.WriteBytes(src, length);
        public override void WriteBytes(Span<byte> src, int srcIndex, int length) => this.wrapped.WriteBytes(src, srcIndex, length);
        
        public override string GetString(int index, int length, Encoding encoding) => this.wrapped.GetString(index, length, encoding);
        public override void SetString(int index, string value, Encoding encoding) => this.wrapped.SetString(index, value, encoding);
        public override string ReadString(int length, Encoding encoding) => this.wrapped.ReadString(length, encoding);
        public override void WriteString(string value, Encoding encoding) => this.wrapped.WriteString(value, encoding);
    }
}
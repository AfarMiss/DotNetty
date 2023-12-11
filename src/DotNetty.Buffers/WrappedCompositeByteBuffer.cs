// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    class WrappedCompositeByteBuffer : CompositeByteBuffer
    {
        #region IByteBuffer

        readonly CompositeByteBuffer wrapped;

        internal WrappedCompositeByteBuffer(CompositeByteBuffer wrapped) : base(wrapped.Allocator)
        {
            this.wrapped = wrapped;
            this.SetMaxCapacity(this.wrapped.MaxCapacity);
        }

        public override bool Release() => this.wrapped.Release();

        public override bool Release(int decrement) => this.wrapped.Release(decrement);

        public sealed override int ReaderIndex => this.wrapped.ReaderIndex;

        public sealed override int WriterIndex => this.wrapped.WriterIndex;

        public sealed override bool IsReadable() => this.wrapped.IsReadable();

        public sealed override bool IsReadable(int numBytes) => this.wrapped.IsReadable(numBytes);

        public sealed override bool IsWritable() => this.wrapped.IsWritable();

        public sealed override int ReadableBytes => this.wrapped.ReadableBytes;

        public sealed override int WritableBytes => this.wrapped.WritableBytes;

        public sealed override int MaxWritableBytes => this.wrapped.MaxWritableBytes;

        public override int EnsureWritable(int minWritableBytes, bool force) => this.wrapped.EnsureWritable(minWritableBytes, force);

        public override IByteBuffer Slice() => this.wrapped.Slice();

        public override IByteBuffer Slice(int index, int length) => this.wrapped.Slice(index, length);

        public override string ToString(Encoding encoding) => this.wrapped.ToString(encoding);

        public override string ToString(int index, int length, Encoding encoding) => this.wrapped.ToString(index, length, encoding);

        public override int IndexOf(int fromIndex, int toIndex, byte value) => this.wrapped.IndexOf(fromIndex, toIndex, value);

        public override int BytesBefore(int index, int length, byte value) => this.wrapped.BytesBefore(index, length, value);

        public override int ForEachByte(IByteProcessor processor) => this.wrapped.ForEachByte(processor);

        public override int ForEachByte(int index, int length, IByteProcessor processor) => this.wrapped.ForEachByte(index, length, processor);

        public override int ForEachByteDesc(IByteProcessor processor) => this.wrapped.ForEachByteDesc(processor);

        public override int ForEachByteDesc(int index, int length, IByteProcessor processor) => this.wrapped.ForEachByteDesc(index, length, processor);

        public sealed override int GetHashCode() => this.wrapped.GetHashCode();

        public sealed override bool Equals(IByteBuffer buf) => this.wrapped.Equals(buf);

        public sealed override int CompareTo(IByteBuffer that) => this.wrapped.CompareTo(that);

        public sealed override int ReferenceCount => this.wrapped.ReferenceCount;

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

        public sealed override bool HasArray => this.wrapped.HasArray;

        public sealed override byte[] Array => this.wrapped.Array;

        public sealed override int ArrayOffset => this.wrapped.ArrayOffset;

        public sealed override int Capacity => this.wrapped.Capacity;

        public override IByteBuffer AdjustCapacity(int newCapacity)
        {
            this.wrapped.AdjustCapacity(newCapacity);
            return this;
        }

        public sealed override IByteBufferAllocator Allocator => this.wrapped.Allocator;

        public sealed override int NumComponents => this.wrapped.NumComponents;

        public sealed override int MaxNumComponents => this.wrapped.MaxNumComponents;

        public sealed override int ToComponentIndex(int offset) => this.wrapped.ToComponentIndex(offset);

        public sealed override int ToByteIndex(int cIndex) => this.wrapped.ToByteIndex(cIndex);

        // public override Task<int> SetBytesAsync(int index, Stream src, int length, CancellationToken cancellationToken) => this.wrapped.SetBytesAsync(index, src, length, cancellationToken);

        public override IByteBuffer Copy() => this.wrapped.Copy();

        public override IByteBuffer Copy(int index, int length) => this.wrapped.Copy(index, length);

        public sealed override IByteBuffer this[int cIndex] => this.wrapped[cIndex];

        public sealed override IByteBuffer ComponentAtOffset(int offset) => this.wrapped.ComponentAtOffset(offset);

        public sealed override IByteBuffer InternalComponent(int cIndex) => this.wrapped.InternalComponent(cIndex);

        public sealed override IByteBuffer InternalComponentAtOffset(int offset) => this.wrapped.InternalComponentAtOffset(offset);

        public override int IoBufferCount => this.wrapped.IoBufferCount;

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

        public sealed override IByteBuffer SetReaderIndex(int readerIndex)
        {
            this.wrapped.SetReaderIndex(readerIndex);
            return this;
        }

        public sealed override IByteBuffer SetWriterIndex(int writerIndex)
        {
            this.wrapped.SetWriterIndex(writerIndex);
            return this;
        }

        public sealed override IByteBuffer SetIndex(int readerIndex, int writerIndex)
        {
            this.wrapped.SetIndex(readerIndex, writerIndex);
            return this;
        }

        public sealed override IByteBuffer Clear()
        {
            this.wrapped.Clear();
            return this;
        }

        public sealed override IByteBuffer MarkReaderIndex()
        {
            this.wrapped.MarkReaderIndex();
            return this;
        }

        public sealed override IByteBuffer ResetReaderIndex()
        {
            this.wrapped.ResetReaderIndex();
            return this;
        }

        public sealed override IByteBuffer MarkWriterIndex()
        {
            this.wrapped.MarkWriterIndex();
            return this;
        }

        public sealed override IByteBuffer ResetWriterIndex()
        {
            this.wrapped.ResetWriterIndex();
            return this;
        }

        public override IByteBuffer EnsureWritable(int minWritableBytes)
        {
            this.wrapped.EnsureWritable(minWritableBytes);
            return this;
        }

        public override IReferenceCounted Retain(int increment)
        {
            this.wrapped.Retain(increment);
            return this;
        }

        public override IReferenceCounted Retain()
        {
            this.wrapped.Retain();
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

        public sealed override bool HasMemoryAddress => this.wrapped.HasMemoryAddress;

        public sealed override bool IsWritable(int size) => this.wrapped.IsWritable(size);

        public sealed override int MaxCapacity => this.wrapped.MaxCapacity;

        public override IByteBuffer ReadRetainedSlice(int length) => this.wrapped.ReadRetainedSlice(length);

        public override IByteBuffer RetainedDuplicate() => this.wrapped.RetainedDuplicate();

        public override IByteBuffer RetainedSlice() => this.wrapped.RetainedSlice();

        public override IByteBuffer RetainedSlice(int index, int length) => this.wrapped.RetainedSlice(index, length);

        #endregion
    }
}
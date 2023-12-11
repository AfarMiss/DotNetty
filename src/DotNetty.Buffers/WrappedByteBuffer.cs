// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    /// Wraps another <see cref="IByteBuffer"/>.
    /// 
    /// It's important that the {@link #readerIndex()} and {@link #writerIndex()} will not do any adjustments on the
    /// indices on the fly because of internal optimizations made by {@link ByteBufUtil#writeAscii(ByteBuf, CharSequence)}
    /// and {@link ByteBufUtil#writeUtf8(ByteBuf, CharSequence)}.
    class WrappedByteBuffer : IByteBuffer
    {
        #region IByteBuffer

        protected readonly IByteBuffer Buf;

        protected WrappedByteBuffer(IByteBuffer buf)
        {
            Contract.Requires(buf != null);

            this.Buf = buf;
        }

        public bool HasMemoryAddress => this.Buf.HasMemoryAddress;

        public ref byte GetPinnableMemoryAddress() => ref this.Buf.GetPinnableMemoryAddress();

        public IntPtr AddressOfPinnedMemory() => this.Buf.AddressOfPinnedMemory();

        public int Capacity => this.Buf.Capacity;

        public virtual IByteBuffer AdjustCapacity(int newCapacity)
        {
            this.Buf.AdjustCapacity(newCapacity);
            return this;
        }

        public int MaxCapacity => this.Buf.MaxCapacity;

        public IByteBufferAllocator Allocator => this.Buf.Allocator;

        public IByteBuffer Unwrap() => this.Buf;

        public int ReaderIndex => this.Buf.ReaderIndex;

        public IByteBuffer SetReaderIndex(int readerIndex)
        {
            this.Buf.SetReaderIndex(readerIndex);
            return this;
        }

        public int WriterIndex => this.Buf.WriterIndex;

        public IByteBuffer SetWriterIndex(int writerIndex)
        {
            this.Buf.SetWriterIndex(writerIndex);
            return this;
        }

        public virtual IByteBuffer SetIndex(int readerIndex, int writerIndex)
        {
            this.Buf.SetIndex(readerIndex, writerIndex);
            return this;
        }

        public int ReadableBytes => this.Buf.ReadableBytes;

        public int WritableBytes => this.Buf.WritableBytes;

        public int MaxWritableBytes => this.Buf.MaxWritableBytes;

        public bool IsReadable() => this.Buf.IsReadable();

        public bool IsWritable() => this.Buf.IsWritable();

        public IByteBuffer Clear()
        {
            this.Buf.Clear();
            return this;
        }

        public IByteBuffer MarkReaderIndex()
        {
            this.Buf.MarkReaderIndex();
            return this;
        }

        public IByteBuffer ResetReaderIndex()
        {
            this.Buf.ResetReaderIndex();
            return this;
        }

        public IByteBuffer MarkWriterIndex()
        {
            this.Buf.MarkWriterIndex();
            return this;
        }

        public IByteBuffer ResetWriterIndex()
        {
            this.Buf.ResetWriterIndex();
            return this;
        }

        public virtual IByteBuffer DiscardReadBytes()
        {
            this.Buf.DiscardReadBytes();
            return this;
        }

        public virtual IByteBuffer DiscardSomeReadBytes()
        {
            this.Buf.DiscardSomeReadBytes();
            return this;
        }

        public virtual IByteBuffer EnsureWritable(int minWritableBytes)
        {
            this.Buf.EnsureWritable(minWritableBytes);
            return this;
        }

        public virtual int EnsureWritable(int minWritableBytes, bool force) => this.Buf.EnsureWritable(minWritableBytes, force);

        public virtual int IndexOf(int fromIndex, int toIndex, byte value) => this.Buf.IndexOf(fromIndex, toIndex, value);

        public virtual int BytesBefore(byte value) => this.Buf.BytesBefore(value);

        public virtual int BytesBefore(int length, byte value) => this.Buf.BytesBefore(length, value);

        public virtual int BytesBefore(int index, int length, byte value) => this.Buf.BytesBefore(index, length, value);

        public virtual int ForEachByte(IByteProcessor processor) => this.Buf.ForEachByte(processor);

        public virtual int ForEachByte(int index, int length, IByteProcessor processor) => this.Buf.ForEachByte(index, length, processor);

        public virtual int ForEachByteDesc(IByteProcessor processor) => this.Buf.ForEachByteDesc(processor);

        public virtual int ForEachByteDesc(int index, int length, IByteProcessor processor) => this.Buf.ForEachByteDesc(index, length, processor);

        public virtual IByteBuffer Copy() => this.Buf.Copy();

        public virtual IByteBuffer Copy(int index, int length) => this.Buf.Copy(index, length);

        public virtual IByteBuffer Slice() => this.Buf.Slice();

        public virtual IByteBuffer RetainedSlice() => this.Buf.RetainedSlice();
        
        public virtual IByteBuffer Slice(int index, int length) => this.Buf.Slice(index, length);

        public virtual IByteBuffer RetainedSlice(int index, int length) => this.Buf.RetainedSlice(index, length);

        public virtual IByteBuffer Duplicate() => this.Buf.Duplicate();

        public virtual IByteBuffer RetainedDuplicate() => this.Buf.RetainedDuplicate();

        public virtual int IoBufferCount => this.Buf.IoBufferCount;

        public virtual ArraySegment<byte> GetIoBuffer() => this.Buf.GetIoBuffer();

        public virtual ArraySegment<byte> GetIoBuffer(int index, int length) => this.Buf.GetIoBuffer(index, length);

        public virtual ArraySegment<byte>[] GetIoBuffers() => this.Buf.GetIoBuffers();

        public virtual ArraySegment<byte>[] GetIoBuffers(int index, int length) => this.Buf.GetIoBuffers(index, length);

        public bool HasArray => this.Buf.HasArray;

        public int ArrayOffset => this.Buf.ArrayOffset;

        public byte[] Array => this.Buf.Array;

        public virtual string ToString(Encoding encoding) => this.Buf.ToString(encoding);

        public virtual string ToString(int index, int length, Encoding encoding) => this.Buf.ToString(index, length, encoding);

        public override int GetHashCode() => this.Buf.GetHashCode();

        public override bool Equals(object obj) => this.Buf.Equals(obj);

        public bool Equals(IByteBuffer buffer) => this.Buf.Equals(buffer);

        public int CompareTo(IByteBuffer buffer) => this.Buf.CompareTo(buffer);

        public override string ToString() => this.GetType().Name + '(' + this.Buf + ')';

        public virtual IReferenceCounted Retain(int increment)
        {
            this.Buf.Retain(increment);
            return this;
        }

        public virtual IReferenceCounted Retain()
        {
            this.Buf.Retain();
            return this;
        }

        public bool IsReadable(int size) => this.Buf.IsReadable(size);

        public bool IsWritable(int size) => this.Buf.IsWritable(size);

        public int ReferenceCount => this.Buf.ReferenceCount;

        public virtual bool Release() => this.Buf.Release();

        public virtual bool Release(int decrement) => this.Buf.Release(decrement);

        public virtual IByteBuffer ReadSlice(int length) => this.Buf.ReadSlice(length);

        public virtual IByteBuffer ReadRetainedSlice(int length) => this.Buf.ReadRetainedSlice(length);
        
        #endregion

        #region IByteBufferProvider

        public virtual T Get<T>(int index) where T : unmanaged => this.Buf.Get<T>(index);

        public virtual void Set<T>(int index, T value) where T : unmanaged => this.Buf.Set<T>(index, value);
        public void Set<T>(int index, int value) where T : unmanaged => this.Buf.Set<T>(index, value);
        public virtual T Read<T>() where T : unmanaged => this.Buf.Read<T>();

        public virtual void Write<T>(T value) where T : unmanaged => this.Buf.Write<T>(value);
        public void Write<T>(int value) where T : unmanaged =>this.Buf.Write<T>(value);

        public virtual void GetBytes(int index, IByteBuffer dst, int? length = null) => this.Buf.GetBytes(index, dst);

        public virtual void GetBytes(int index, IByteBuffer dst, int dstIndex, int length) => this.Buf.GetBytes(index, dst, length);

        public virtual void GetBytes(int index, byte[] dst, int? length = null) => this.Buf.GetBytes(index, dst);

        public virtual void GetBytes(int index, byte[] dst, int dstIndex, int length) => this.Buf.GetBytes(index, dst, dstIndex, length);

        public virtual void SetBytes(int index, IByteBuffer src, int? length = null) => this.Buf.SetBytes(index, src, length);

        public virtual void SetBytes(int index, IByteBuffer src, int srcIndex, int length) => this.Buf.SetBytes(index, src, srcIndex, length);

        public virtual void SetBytes(int index, byte[] src, int? length = null) => this.Buf.SetBytes(index, src, length);

        public virtual void SetBytes(int index, byte[] src, int srcIndex, int length) => this.Buf.SetBytes(index, src, srcIndex, length);

        public virtual void SkipBytes(int length) => this.Buf.SkipBytes(length);

        public virtual void ReadBytes(IByteBuffer dst, int? length = null) => this.Buf.ReadBytes(dst, length);

        public virtual void ReadBytes(IByteBuffer dst, int dstIndex, int length) => this.Buf.ReadBytes(dst, dstIndex, length);

        public virtual void ReadBytes(byte[] dst, int? length = null) => this.Buf.ReadBytes(dst, length);

        public virtual void ReadBytes(byte[] dst, int dstIndex, int length) => this.Buf.ReadBytes(dst, dstIndex, length);

        public virtual void WriteBytes(IByteBuffer src, int? length = null) => this.Buf.WriteBytes(src, length);

        public virtual void WriteBytes(IByteBuffer src, int srcIndex, int length) => this.Buf.WriteBytes(src, srcIndex, length);

        public virtual void WriteBytes(byte[] src, int? length = null) => this.Buf.WriteBytes(src, length);

        public virtual void WriteBytes(byte[] src, int srcIndex, int length) => this.Buf.WriteBytes(src, srcIndex, length);

        public string GetString(int index, int length, Encoding encoding) => this.Buf.GetString(index, length, encoding);

        public void SetString(int index, string value, Encoding encoding) => this.Buf.SetString(index, value, encoding);
        
        public string ReadString(int length, Encoding encoding) => this.Buf.ReadString(length, encoding);

        public void WriteString(string value, Encoding encoding) => this.Buf.WriteString(value, encoding);

        #endregion
    }
}
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

    /// <inheritdoc />
    /// <summary>
    ///     Represents an empty byte buffer
    /// </summary>
    public sealed class EmptyByteBuffer : IByteBuffer
    {
        #region IByteBuffer

        static readonly ArraySegment<byte> EmptyBuffer = new ArraySegment<byte>(ArrayExtensions.ZeroBytes);
        static readonly ArraySegment<byte>[] EmptyBuffers = { EmptyBuffer };

        public EmptyByteBuffer(IByteBufferAllocator allocator)
        {
            Contract.Requires(allocator != null);

            this.Allocator = allocator;
        }

        public int Capacity => 0;

        public IByteBuffer AdjustCapacity(int newCapacity) =>throw new NotSupportedException();

        public int MaxCapacity => 0;

        public IByteBufferAllocator Allocator { get; }

        public IByteBuffer Unwrap() => null;

        public int ReaderIndex => 0;

        public IByteBuffer SetReaderIndex(int readerIndex) => this.CheckIndex(readerIndex);

        public int WriterIndex => 0;

        public IByteBuffer SetWriterIndex(int writerIndex) => this.CheckIndex(writerIndex);

        public IByteBuffer SetIndex(int readerIndex, int writerIndex)
        {
            this.CheckIndex(readerIndex);
            this.CheckIndex(writerIndex);
            return this;
        }

        public int ReadableBytes => 0;

        public int WritableBytes => 0;

        public int MaxWritableBytes => 0;

        public bool IsWritable() => false;

        public bool IsWritable(int size) => false;

        public IByteBuffer Clear() => this;

        public IByteBuffer MarkReaderIndex() => this;

        public IByteBuffer ResetReaderIndex() => this;

        public IByteBuffer MarkWriterIndex() => this;

        public IByteBuffer ResetWriterIndex() => this;

        public IByteBuffer DiscardReadBytes() => this;

        public IByteBuffer DiscardSomeReadBytes() => this;

        public IByteBuffer EnsureWritable(int minWritableBytes)
        {
            Contract.Requires(minWritableBytes >= 0);

            if (minWritableBytes != 0)
            {
                throw new IndexOutOfRangeException();
            }
            return this;
        }

        public int EnsureWritable(int minWritableBytes, bool force)
        {
            Contract.Requires(minWritableBytes >= 0);

            if (minWritableBytes == 0)
            {
                return 0;
            }

            return 1;
        }

        public int IndexOf(int fromIndex, int toIndex, byte value)
        {
            this.CheckIndex(fromIndex);
            this.CheckIndex(toIndex);
            return -1;
        }

        public int BytesBefore(byte value) => -1;

        public int BytesBefore(int length, byte value)
        {
            this.CheckLength(length);
            return -1;
        }

        public int BytesBefore(int index, int length, byte value)
        {
            this.CheckIndex(index, length);
            return -1;
        }

        public int ForEachByte(IByteProcessor processor) => -1;

        public int ForEachByte(int index, int length, IByteProcessor processor)
        {
            this.CheckIndex(index, length);
            return -1;
        }

        public int ForEachByteDesc(IByteProcessor processor) => -1;

        public int ForEachByteDesc(int index, int length, IByteProcessor processor)
        {
            this.CheckIndex(index, length);
            return -1;
        }

        public IByteBuffer Copy() => this;

        public IByteBuffer Copy(int index, int length)
        {
            this.CheckIndex(index, length);
            return this;
        }

        public IByteBuffer Slice() => this;

        public IByteBuffer RetainedSlice() => this;

        public IByteBuffer Slice(int index, int length) => this.CheckIndex(index, length);

        public IByteBuffer RetainedSlice(int index, int length) => this.CheckIndex(index, length);

        public IByteBuffer Duplicate() => this;

        public int IoBufferCount => 1;

        public ArraySegment<byte> GetIoBuffer() => EmptyBuffer;

        public ArraySegment<byte> GetIoBuffer(int index, int length)
        {
            this.CheckIndex(index, length);
            return this.GetIoBuffer();
        }

        public ArraySegment<byte>[] GetIoBuffers() => EmptyBuffers;

        public ArraySegment<byte>[] GetIoBuffers(int index, int length)
        {
            this.CheckIndex(index, length);
            return this.GetIoBuffers();
        }

        public bool HasArray => true;

        public byte[] Array => ArrayExtensions.ZeroBytes;

        public byte[] ToArray() => ArrayExtensions.ZeroBytes;

        public int ArrayOffset => 0;

        public bool HasMemoryAddress => false;

        public ref byte GetPinnableMemoryAddress() => throw new NotSupportedException();

        public IntPtr AddressOfPinnedMemory() => IntPtr.Zero;

        public string ToString(Encoding encoding) => string.Empty;

        public string ToString(int index, int length, Encoding encoding)
        {
            this.CheckIndex(index, length);
            return this.ToString(encoding);
        }

        public override int GetHashCode() => 0;

        public bool Equals(IByteBuffer buffer) => buffer != null && !buffer.IsReadable();

        public override bool Equals(object obj)
        {
            var buffer = obj as IByteBuffer;
            return this.Equals(buffer);
        }

        public int CompareTo(IByteBuffer buffer) => buffer.IsReadable() ? -1 : 0;

        public override string ToString() => string.Empty;

        public bool IsReadable() => false;

        public bool IsReadable(int size) => false;

        public int ReferenceCount => 1;

        public IReferenceCounted Retain() => this;

        public IByteBuffer RetainedDuplicate() => this;

        public IReferenceCounted Retain(int increment) => this;

        public IReferenceCounted Touch() => this;

        public IReferenceCounted Touch(object hint) => this;

        public bool Release() => false;

        public bool Release(int decrement) => false;

        public IByteBuffer ReadSlice(int length) => this.CheckLength(length);

        public IByteBuffer ReadRetainedSlice(int length) => this.CheckLength(length);

        // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
        IByteBuffer CheckIndex(int index)
        {
            if (index != 0)
            {
                throw new IndexOutOfRangeException();
            }
            return this;
        }

        IByteBuffer CheckIndex(int index, int length)
        {
            if (length < 0)
            {
                throw new ArgumentException("length: " + length);
            }
            if (index != 0 || length != 0)
            {
                throw new IndexOutOfRangeException();
            }

            return this;
        }
        // ReSharper restore ParameterOnlyUsedForPreconditionCheck.Local

        IByteBuffer CheckLength(int length)
        {
            if (length < 0)
            {
                throw new ArgumentException("length: " + length + " (expected: >= 0)");
            }
            if (length != 0)
            {
                throw new IndexOutOfRangeException();
            }
            return this;
        }

        #endregion

        #region IByteBufferProvider

        public T Get<T>(int index) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void Set<T>(int index, T value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void Set<T>(int index, int value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public T Read<T>() where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void Write<T>(T value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void Write<T>(int value) where T : unmanaged
        {
            throw new NotImplementedException();
        }

        public void GetBytes(int index, IByteBuffer dst, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void GetBytes(int index, IByteBuffer dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void GetBytes(int index, byte[] dst, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void GetBytes(int index, byte[] dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void SetBytes(int index, IByteBuffer src, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void SetBytes(int index, IByteBuffer src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void SetBytes(int index, byte[] src, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void SetBytes(int index, byte[] src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void SkipBytes(int length)
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(IByteBuffer dst, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(IByteBuffer dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(byte[] dst, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void ReadBytes(byte[] dst, int dstIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(IByteBuffer src, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(IByteBuffer src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(byte[] src, int? length = null)
        {
            throw new NotImplementedException();
        }

        public void WriteBytes(byte[] src, int srcIndex, int length)
        {
            throw new NotImplementedException();
        }

        public string GetString(int index, int length, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public void SetString(int index, string value, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public string ReadString(int length, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        public void WriteString(string value, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
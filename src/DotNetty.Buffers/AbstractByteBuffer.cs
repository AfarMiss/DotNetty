using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using DotNetty.Common;
using DotNetty.Common.Internal;
using DotNetty.Common.Utilities;

namespace DotNetty.Buffers
{
    /// <summary>
    /// <see cref="IByteBuffer"/> 基础实现
    /// </summary>
    public abstract class AbstractByteBuffer : IByteBuffer
    {
        #region IByteBuffer

        private int readerIndex;
        private int writerIndex;

        private int markedReaderIndex;
        private int markedWriterIndex;
        private int maxCapacity;

        public virtual int MaxCapacity => this.maxCapacity;
        public virtual int ReaderIndex => this.readerIndex;
        public virtual int WriterIndex => this.writerIndex;
        public virtual int ReadableBytes => this.writerIndex - this.readerIndex;
        public virtual int WritableBytes => this.Capacity - this.writerIndex;
        public virtual int MaxWritableBytes => this.MaxCapacity - this.writerIndex;
        
        public abstract int Capacity { get; }
        
        protected AbstractByteBuffer(int maxCapacity)
        {
            Contract.Requires(maxCapacity >= 0);
            this.maxCapacity = maxCapacity;
        }

        public abstract IByteBuffer AdjustCapacity(int newCapacity);

        protected void SetMaxCapacity(int newMaxCapacity)
        {
            Contract.Requires(newMaxCapacity >= 0);

            this.maxCapacity = newMaxCapacity;
        }

        public virtual void SetReaderIndex(int index)
        {
            if (index < 0 || index > this.writerIndex)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_ReaderIndex(index, this.WriterIndex);
            }

            this.readerIndex = index;
        }

        public virtual void SetWriterIndex(int index)
        {
            if (index < this.readerIndex || index > this.Capacity)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_WriterIndex(index, this.readerIndex, this.Capacity);
            }

            this.SetWriterIndex0(index);
        }

        protected void SetWriterIndex0(int index)
        {
            this.writerIndex = index;
        }

        public virtual void SetIndex(int readerIdx, int writerIdx)
        {
            if (readerIdx < 0 || readerIdx > writerIdx || writerIdx > this.Capacity)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_ReaderWriterIndex(readerIdx, writerIdx, this.Capacity);
            }

            this.SetIndex0(readerIdx, writerIdx);
        }

        public virtual void ResetIndex() => this.readerIndex = this.writerIndex = 0;
        
        public virtual bool IsReadable() => this.writerIndex > this.readerIndex;
        public virtual bool IsReadable(int size) => this.writerIndex - this.readerIndex >= size;
        public virtual bool IsWritable() => this.Capacity > this.writerIndex;
        public virtual bool IsWritable(int size) => this.Capacity - this.writerIndex >= size;

        public virtual void MarkReaderIndex() => this.markedReaderIndex = this.readerIndex;

        public virtual void ResetReaderIndex() => this.SetReaderIndex(this.markedReaderIndex);

        public virtual void MarkWriterIndex() => this.markedWriterIndex = this.writerIndex;

        public virtual void ResetWriterIndex() => this.SetWriterIndex(this.markedWriterIndex);

        protected void MarkIndex()
        {
            this.markedReaderIndex = this.readerIndex;
            this.markedWriterIndex = this.writerIndex;
        }

        public virtual IByteBuffer DiscardReadBytes()
        {
            this.EnsureAccessible();
            if (this.readerIndex == 0)
            {
                return this;
            }

            if (this.readerIndex != this.writerIndex)
            {
                this.SetBytes(0, this, this.readerIndex, this.writerIndex - this.readerIndex);
                this.writerIndex -= this.readerIndex;
                this.AdjustMarkers(this.readerIndex);
                this.readerIndex = 0;
            }
            else
            {
                this.AdjustMarkers(this.readerIndex);
                this.writerIndex = this.readerIndex = 0;
            }

            return this;
        }

        public virtual IByteBuffer DiscardSomeReadBytes()
        {
            this.EnsureAccessible();
            if (this.readerIndex == 0)
            {
                return this;
            }

            if (this.readerIndex == this.writerIndex)
            {
                this.AdjustMarkers(this.readerIndex);
                this.writerIndex = this.readerIndex = 0;
                return this;
            }

            if (this.readerIndex >= this.Capacity.RightUShift(1))
            {
                this.SetBytes(0, this, this.readerIndex, this.writerIndex - this.readerIndex);
                this.writerIndex -= this.readerIndex;
                this.AdjustMarkers(this.readerIndex);
                this.readerIndex = 0;
            }

            return this;
        }

        protected void AdjustMarkers(int decrement)
        {
            int markedReaderIdx = this.markedReaderIndex;
            if (markedReaderIdx <= decrement)
            {
                this.markedReaderIndex = 0;
                int markedWriterIdx = this.markedWriterIndex;
                if (markedWriterIdx <= decrement)
                {
                    this.markedWriterIndex = 0;
                }
                else
                {
                    this.markedWriterIndex = markedWriterIdx - decrement;
                }
            }
            else
            {
                this.markedReaderIndex = markedReaderIdx - decrement;
                this.markedWriterIndex -= decrement;
            }
        }

        public virtual IByteBuffer EnsureWritable(int minWritableBytes)
        {
            if (minWritableBytes < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_MinWritableBytes();
            }

            this.EnsureWritable0(minWritableBytes);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void EnsureWritable0(int minWritableBytes)
        {
            this.EnsureAccessible();
            if (minWritableBytes <= this.WritableBytes)
            {
                return;
            }

            if (minWritableBytes > this.MaxCapacity - this.writerIndex)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_WriterIndex(minWritableBytes, this.writerIndex, this.MaxCapacity, this);
            }

            // Normalize the current capacity to the power of 2.
            int newCapacity = ByteBuffer.Allocator.CalculateNewCapacity(this.writerIndex + minWritableBytes, this.MaxCapacity);

            // Adjust to the new capacity.
            this.AdjustCapacity(newCapacity);
        }

        public virtual int EnsureWritable(int minWritableBytes, bool force)
        {
            Contract.Ensures(minWritableBytes >= 0);

            this.EnsureAccessible();
            if (minWritableBytes <= this.WritableBytes)
            {
                return 0;
            }

            if (minWritableBytes > this.MaxCapacity - this.writerIndex)
            {
                if (!force || this.Capacity == this.MaxCapacity)
                {
                    return 1;
                }

                this.AdjustCapacity(this.MaxCapacity);
                return 3;
            }

            // Normalize the current capacity to the power of 2.
            int newCapacity = ByteBuffer.Allocator.CalculateNewCapacity(this.writerIndex + minWritableBytes, this.MaxCapacity);

            // Adjust to the new capacity.
            this.AdjustCapacity(newCapacity);
            return 2;
        }

        
        public virtual IByteBuffer Copy() => this.Copy(this.readerIndex, this.ReadableBytes);

        public abstract IByteBuffer Copy(int index, int length);

        public virtual IByteBuffer Duplicate() => new DuplicateByteBuffer(this);

        public virtual IByteBuffer RetainedDuplicate() => (IByteBuffer)this.Duplicate().Retain();

        public virtual IByteBuffer Slice() => this.Slice(this.readerIndex, this.ReadableBytes);

        public virtual IByteBuffer RetainedSlice() => (IByteBuffer)this.Slice().Retain();

        public virtual IByteBuffer Slice(int index, int length) => new SlicedByteBuffer(this, index, length);

        public virtual IByteBuffer RetainedSlice(int index, int length) => (IByteBuffer)this.Slice(index, length).Retain();

        public virtual string ToString(Encoding encoding) => this.ToString(this.readerIndex, this.ReadableBytes, encoding);

        public virtual string ToString(int index, int length, Encoding encoding) => ByteBufferUtil.DecodeString(this, index, length, encoding);

        public virtual IByteBuffer ReadSlice(int length)
        {
            this.CheckReadableBytes(length);
            IByteBuffer slice = this.Slice(this.readerIndex, length);
            this.readerIndex += length;
            return slice;
        }

        public virtual IByteBuffer ReadRetainedSlice(int length)
        {
            this.CheckReadableBytes(length);
            IByteBuffer slice = this.RetainedSlice(this.readerIndex, length);
            this.readerIndex += length;
            return slice;
        }

        public override int GetHashCode() => ByteBufferUtil.HashCode(this);

        public sealed override bool Equals(object o) => this.Equals(o as IByteBuffer);

        public virtual bool Equals(IByteBuffer buffer) =>
            ReferenceEquals(this, buffer) || buffer != null && ByteBufferUtil.Equals(this, buffer);

        public virtual int CompareTo(IByteBuffer that) => ByteBufferUtil.Compare(this, that);

        public override string ToString()
        {
            if (this.ReferenceCount == 0) return this.GetType().Name + "(freed)";

            var buf = new StringBuilder()
                .Append("RIdx: ").Append(this.readerIndex)
                .Append(" ")
                .Append("WIdx: ").Append(this.writerIndex);
            buf.Append(this.Capacity).Append('/').Append(this.MaxCapacity);

            var unwrapped = this.Unwrap();
            if (unwrapped != null) buf.Append(" unwrapped: ").Append(unwrapped);
            return buf.ToString();
        }

        protected void CheckIndex(int index) => this.CheckIndex(index, 1);

        protected internal void CheckIndex(int index, int fieldLength)
        {
            this.EnsureAccessible();
            this.CheckIndex0(index, fieldLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected internal void CheckIndex0(int index, int fieldLength)
        {
            if (MathUtil.IsOutOfBounds(index, fieldLength, this.Capacity))
            {
                ThrowHelper.ThrowIndexOutOfRangeException_Index(index, fieldLength, this.Capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckSrcIndex(int index, int length, int srcIndex, int srcCapacity)
        {
            this.CheckIndex(index, length);
            if (MathUtil.IsOutOfBounds(srcIndex, length, srcCapacity))
            {
                ThrowHelper.ThrowIndexOutOfRangeException_SrcIndex(srcIndex, length, srcCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckDstIndex(int index, int length, int dstIndex, int dstCapacity)
        {
            this.CheckIndex(index, length);
            if (MathUtil.IsOutOfBounds(dstIndex, length, dstCapacity))
            {
                ThrowHelper.ThrowIndexOutOfRangeException_DstIndex(dstIndex, length, dstCapacity);
            }
        }

        protected internal void CheckReadableBytes(int minimumReadableBytes)
        {
            if (minimumReadableBytes < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_MinimumReadableBytes(minimumReadableBytes);
            }

            this.CheckReadableBytes0(minimumReadableBytes);
        }

        protected void CheckNewCapacity(int newCapacity)
        {
            this.EnsureAccessible();
            if (newCapacity < 0 || newCapacity > this.MaxCapacity)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_Capacity(newCapacity, this.MaxCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void CheckReadableBytes0(int minimumReadableBytes)
        {
            this.EnsureAccessible();
            if (this.readerIndex > this.writerIndex - minimumReadableBytes)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_ReaderIndex(minimumReadableBytes, this.readerIndex, this.writerIndex, this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EnsureAccessible()
        {
            if (this.ReferenceCount == 0) ThrowHelper.ThrowIllegalReferenceCountException(0);
        }

        protected void SetIndex0(int readerIdx, int writerIdx)
        {
            this.readerIndex = readerIdx;
            this.writerIndex = writerIdx;
        }

        protected void DiscardMarks()
        {
            this.markedReaderIndex = this.markedWriterIndex = 0;
        }

        public abstract int IoBufferCount { get; }

        public ArraySegment<byte> GetIoBuffer() => this.GetIoBuffer(this.readerIndex, this.ReadableBytes);

        public abstract ArraySegment<byte> GetIoBuffer(int index, int length);

        public ArraySegment<byte>[] GetIoBuffers() => this.GetIoBuffers(this.readerIndex, this.ReadableBytes);

        public abstract ArraySegment<byte>[] GetIoBuffers(int index, int length);

        public abstract bool HasArray { get; }

        public abstract byte[] Array { get; }

        public abstract int ArrayOffset { get; }

        public abstract bool HasMemoryAddress { get; }

        public abstract ref byte GetPinnableMemoryAddress();

        public abstract IntPtr AddressOfPinnedMemory();

        public abstract IByteBuffer Unwrap();

        public abstract int ReferenceCount { get; }
        public abstract IReferenceCounted Retain(int increment = 1);
        public abstract bool Release(int decrement = 1);

        #endregion

        #region IByteBufferAccessor

        public virtual unsafe T Get<T>(int index) where T : unmanaged
        {
            this.CheckIndex(index, sizeof(T));
            return this._Get<T>(index);
        }
        protected internal abstract T _Get<T>(int index) where T : unmanaged;

        public virtual unsafe void Set<T>(int index, T value) where T : unmanaged
        {
            this.CheckIndex(index, sizeof(T));
            this._Set(index, value);
        }

        public virtual void Set<T>(int index, int value) where T : unmanaged => this.Set<T>(index, Unsafe.As<int, T>(ref value));

        protected internal abstract void _Set<T>(int index, T value) where T : unmanaged;

        public virtual unsafe T Read<T>() where T : unmanaged
        {
            var size = sizeof(T);
            this.CheckReadableBytes0(size);
            var value = this._Get<T>(this.readerIndex);
            this.readerIndex += size;
            return value;
        }

        public virtual unsafe void Write<T>(T value) where T : unmanaged
        {
            var size = sizeof(T);
            this.EnsureWritable0(size);
            this._Set(this.writerIndex, value);
            this.writerIndex += size;
        }
        public virtual void Write<T>(int value) where T : unmanaged => this.Write<T>(Unsafe.As<int, T>(ref value));

        public virtual void GetBytes(int index, IByteBuffer dst, int? length = null)
        {
            var writeLength = length.GetValueOrDefault(dst.WritableBytes);
            this.GetBytes(index, dst, dst.WriterIndex, writeLength);
            dst.SetWriterIndex(dst.WriterIndex + writeLength);
        }
        public abstract void GetBytes(int index, IByteBuffer dst, int dstIndex, int length);

        public virtual void GetBytes(int index, Span<byte> dst, int? length = null)
        {
            var writeLength = length.GetValueOrDefault(dst.Length);
            this.GetBytes(index, dst, 0, writeLength);
        }
        public abstract void GetBytes(int index, Span<byte> dst, int dstIndex, int length);

        public virtual void SetBytes(int index, IByteBuffer src, int? length = null)
        {
            var readLength = length.GetValueOrDefault(src.ReadableBytes);
            this.CheckIndex(index, readLength);
            if (readLength > src.ReadableBytes)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_ReadableBytes(readLength, src);
            }
            this.SetBytes(index, src, src.ReaderIndex, readLength);
            src.SetReaderIndex(src.ReaderIndex + readLength);
        }
        public abstract void SetBytes(int index, IByteBuffer src, int srcIndex, int length);

        public virtual void SetBytes(int index, Span<byte> src, int? length = null)
        {
            var readLength = length.GetValueOrDefault(src.Length);
            this.SetBytes(index, src, 0, readLength);
        }
        public abstract void SetBytes(int index, Span<byte> src, int srcIndex, int length);
        
        public virtual void SkipBytes(int length)
        {
            this.CheckReadableBytes(length);
            this.readerIndex += length;
        }

        public virtual void ReadBytes(IByteBuffer dst, int? length = null)
        {
            var readLength = length.GetValueOrDefault(dst.WritableBytes);
            if (readLength > dst.WritableBytes)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_WritableBytes(readLength, dst);
            }
            this.ReadBytes(dst, dst.WriterIndex, readLength);
            dst.SetWriterIndex(dst.WriterIndex + readLength);
        }

        public virtual void ReadBytes(IByteBuffer dst, int dstIndex, int length)
        {
            this.CheckReadableBytes(length);
            this.GetBytes(this.readerIndex, dst, dstIndex, length);
            this.readerIndex += length;
        }

        public virtual void ReadBytes(Span<byte> dst, int? length = null)
        {
            var readLength = length.GetValueOrDefault(dst.Length);
            this.ReadBytes(dst, 0, readLength);
        }

        public virtual void ReadBytes(Span<byte> dst, int dstIndex, int length)
        {
            this.CheckReadableBytes(length);
            this.GetBytes(this.readerIndex, dst, dstIndex, length);
            this.readerIndex += length;
        }

        public virtual void WriteBytes(IByteBuffer src, int? length = null)
        {
            var writeLength = length.GetValueOrDefault(src.ReadableBytes);
            if (length > src.ReadableBytes)
            {
                ThrowHelper.ThrowIndexOutOfRangeException_ReadableBytes(writeLength, src);
            }
            this.WriteBytes(src, src.ReaderIndex, writeLength);
            src.SetReaderIndex(src.ReaderIndex + writeLength);
        }

        public virtual void WriteBytes(IByteBuffer src, int srcIndex, int length)
        {
            this.EnsureWritable(length);
            this.SetBytes(this.writerIndex, src, srcIndex, length);
            this.writerIndex += length;
        }

        public virtual void WriteBytes(Span<byte> src, int? length = null)
        {
            var writeLength = length.GetValueOrDefault(src.Length);
            this.WriteBytes(src, 0, writeLength);
        }

        public virtual void WriteBytes(Span<byte> src, int srcIndex, int length)
        {
            this.EnsureWritable(length);
            this.SetBytes(this.writerIndex, src, srcIndex, length);
            this.writerIndex += length;
        }

        public virtual unsafe string GetString(int index, int length, Encoding encoding)
        {
            this.CheckIndex0(index, length);
            if (length == 0) return string.Empty;
            
            if (this.HasMemoryAddress)
            {
                IntPtr ptr = this.AddressOfPinnedMemory();
                if (ptr != IntPtr.Zero)
                {
                    return UnsafeByteBufferUtil.GetString((byte*)(ptr + index), length, encoding);
                }
                else 
                {
                    fixed (byte* p = &this.GetPinnableMemoryAddress())
                    {
                        return UnsafeByteBufferUtil.GetString(p + index, length, encoding);
                    }
                }
            }
            if (this.HasArray)
            {
                return encoding.GetString(this.Array, this.ArrayOffset + index, length);
            }

            return this.ToString(index, length, encoding);
        }

        public virtual void SetString(int index, string value, Encoding encoding)
        {
            this.SetString0(index, value, encoding, false);
        }

        public virtual string ReadString(int length, Encoding encoding)
        {
            var value = this.GetString(this.readerIndex, length, encoding);
            this.readerIndex += length;
            return value;
        }

        public virtual void WriteString(string value, Encoding encoding)
        {
            var length = this.SetString0(this.writerIndex, value, encoding, true);
            this.writerIndex += length;
        }

        private int SetString0(int index, string value, Encoding encoding, bool expand)
        {
            if (ReferenceEquals(encoding, Encoding.UTF8))
            {
                int length = ByteBufferUtil.Utf8MaxBytes(value);
                if (expand)
                {
                    this.EnsureWritable0(length);
                    this.CheckIndex0(index, length);
                }
                else
                {
                    this.CheckIndex(index, length);
                }
                return ByteBufferUtil.WriteUtf8(this, index, value, value.Length);
            }
            if (ReferenceEquals(encoding, Encoding.ASCII))
            {
                int length = value.Length;
                if (expand)
                {
                    this.EnsureWritable0(length);
                    this.CheckIndex0(index, length);
                }
                else
                {
                    this.CheckIndex(index, length);
                }
                return ByteBufferUtil.WriteAscii(this, index, value, length);
            }
            var bytes = encoding.GetBytes(value);
            if (expand)
            {
                this.EnsureWritable0(bytes.Length);
                // setBytes(...) will take care of checking the indices.
            }
            this.SetBytes(index, bytes);
            return bytes.Length;
        }
        
        #endregion
    }
}
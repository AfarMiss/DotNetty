using System;
using System.Text;
using DotNetty.Common;
using DotNetty.Common.Utilities;

namespace DotNetty.Buffers
{
    public interface IByteBuffer : IByteBufferProvider, IReferenceCounted, IComparable<IByteBuffer>, IEquatable<IByteBuffer>
    {
        int Capacity { get; }
        int MaxCapacity { get; }

        int ReaderIndex { get; }
        int WriterIndex { get; }
        
        int ReadableBytes { get; }
        int WritableBytes { get; }
        int MaxWritableBytes { get; }
        
        IByteBufferAllocator Allocator { get; }
        
        IByteBuffer AdjustCapacity(int newCapacity);

        void SetWriterIndex(int writerIndex);
        void SetReaderIndex(int readerIndex);
        void SetIndex(int readerIndex, int writerIndex);

        /// <summary>
        /// <see cref="WriterIndex"/> - <see cref="ReaderIndex"/>大于0则true
        /// </summary>
        bool IsReadable();
        bool IsReadable(int size);

        /// <summary>
        ///  <see cref="Capacity"/> - <see cref="WriterIndex"/>大于0则true
        /// </summary>
        bool IsWritable();
        bool IsWritable(int size);

        /// <summary>
        /// 重置索引为0 不会清楚数据 可重新覆盖数据
        /// </summary>
        void ResetIndex();

        /// <summary>
        ///     Marks the current <see cref="ReaderIndex" /> in this buffer. You can reposition the current
        ///     <see cref="ReaderIndex" />
        ///     to the marked <see cref="ReaderIndex" /> by calling <see cref="ResetReaderIndex" />.
        ///     The initial value of the marked <see cref="ReaderIndex" /> is <c>0</c>.
        /// </summary>
        void MarkReaderIndex();

        /// <summary>
        ///     Repositions the current <see cref="ReaderIndex" /> to the marked <see cref="ReaderIndex" /> in this buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        ///     is thrown if the current <see cref="WriterIndex" /> is less than the
        ///     marked <see cref="ReaderIndex" />
        /// </exception>
        void ResetReaderIndex();

        /// <summary>
        ///     Marks the current <see cref="WriterIndex" /> in this buffer. You can reposition the current
        ///     <see cref="WriterIndex" />
        ///     to the marked <see cref="WriterIndex" /> by calling <see cref="ResetWriterIndex" />.
        ///     The initial value of the marked <see cref="WriterIndex" /> is <c>0</c>.
        /// </summary>
        void MarkWriterIndex();

        /// <summary>
        ///     Repositions the current <see cref="WriterIndex" /> to the marked <see cref="WriterIndex" /> in this buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        ///     is thrown if the current <see cref="ReaderIndex" /> is greater than the
        ///     marked <see cref="WriterIndex" />
        /// </exception>
        void ResetWriterIndex();

        /// <summary>
        ///     Discards the bytes between the 0th index and <see cref="ReaderIndex" />.
        ///     It moves the bytes between <see cref="ReaderIndex" /> and <see cref="WriterIndex" /> to the 0th index,
        ///     and sets <see cref="ReaderIndex" /> and <see cref="WriterIndex" /> to <c>0</c> and
        ///     <c>oldWriterIndex - oldReaderIndex</c> respectively.
        /// </summary>
        IByteBuffer DiscardReadBytes();

        /// <summary>
        ///     Similar to <see cref="DiscardReadBytes" /> except that this method might discard
        ///     some, all, or none of read bytes depending on its internal implementation to reduce
        ///     overall memory bandwidth consumption at the cost of potentially additional memory
        ///     consumption.
        /// </summary>
        IByteBuffer DiscardSomeReadBytes();

        /// <summary>
        /// 确保<see cref="WritableBytes"/>大于<paramref name="minWritableBytes"/>
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"> </exception>
        IByteBuffer EnsureWritable(int minWritableBytes);

        /// <summary>
        ///     Tries to make sure the number of <see cref="WritableBytes" />
        ///     is equal to or greater than the specified value. Unlike <see cref="EnsureWritable(int)" />,
        ///     this method does not raise an exception but returns a code.
        /// </summary>
        /// <param name="minWritableBytes">the expected minimum number of writable bytes</param>
        /// <param name="force">
        ///     When <see cref="WriterIndex" /> + <c>minWritableBytes</c> > <see cref="MaxCapacity" />:
        ///     <ul>
        ///         <li><c>true</c> - the capacity of the buffer is expanded to <see cref="MaxCapacity" /></li>
        ///         <li><c>false</c> - the capacity of the buffer is unchanged</li>
        ///     </ul>
        /// </param>
        /// <returns>
        ///     <c>0</c> if the buffer has enough writable bytes, and its capacity is unchanged.
        ///     <c>1</c> if the buffer does not have enough bytes, and its capacity is unchanged.
        ///     <c>2</c> if the buffer has enough writable bytes, and its capacity has been increased.
        ///     <c>3</c> if the buffer does not have enough bytes, but its capacity has been increased to its maximum.
        /// </returns>
        int EnsureWritable(int minWritableBytes, bool force);

        /// <summary>
        ///     Returns the maximum <see cref="ArraySegment{T}" /> of <see cref="Byte" /> that this buffer holds. Note that
        ///     <see cref="GetIoBuffers()" />
        ///     or <see cref="GetIoBuffers(int,int)" /> might return a less number of <see cref="ArraySegment{T}" />s of
        ///     <see cref="Byte" />.
        /// </summary>
        /// <returns>
        ///     <c>-1</c> if this buffer cannot represent its content as <see cref="ArraySegment{T}" /> of <see cref="Byte" />.
        ///     the number of the underlying <see cref="IByteBuffer"/>s if this buffer has at least one underlying segment.
        ///     Note that this method does not return <c>0</c> to avoid confusion.
        /// </returns>
        /// <seealso cref="GetIoBuffer()" />
        /// <seealso cref="GetIoBuffer(int,int)" />
        /// <seealso cref="GetIoBuffers()" />
        /// <seealso cref="GetIoBuffers(int,int)" />
        int IoBufferCount { get; }

        /// <summary>
        ///     Exposes this buffer's readable bytes as an <see cref="ArraySegment{T}" /> of <see cref="Byte" />. Returned segment
        ///     shares the content with this buffer. This method is identical
        ///     to <c>buf.GetIoBuffer(buf.ReaderIndex, buf.ReadableBytes)</c>. This method does not
        ///     modify <see cref="ReaderIndex" /> or <see cref="WriterIndex" /> of this buffer.  Please note that the
        ///     returned segment will not see the changes of this buffer if this buffer is a dynamic
        ///     buffer and it adjusted its capacity.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///     if this buffer cannot represent its content as <see cref="ArraySegment{T}" />
        ///     of <see cref="Byte" />
        /// </exception>
        /// <seealso cref="IoBufferCount" />
        /// <seealso cref="GetIoBuffers()" />
        /// <seealso cref="GetIoBuffers(int,int)" />
        ArraySegment<byte> GetIoBuffer();

        /// <summary>
        ///     Exposes this buffer's sub-region as an <see cref="ArraySegment{T}" /> of <see cref="Byte" />. Returned segment
        ///     shares the content with this buffer. This method does not
        ///     modify <see cref="ReaderIndex" /> or <see cref="WriterIndex" /> of this buffer. Please note that the
        ///     returned segment will not see the changes of this buffer if this buffer is a dynamic
        ///     buffer and it adjusted its capacity.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///     if this buffer cannot represent its content as <see cref="ArraySegment{T}" />
        ///     of <see cref="Byte" />
        /// </exception>
        /// <seealso cref="IoBufferCount" />
        /// <seealso cref="GetIoBuffers()" />
        /// <seealso cref="GetIoBuffers(int,int)" />
        ArraySegment<byte> GetIoBuffer(int index, int length);

        /// <summary>
        ///     Exposes this buffer's readable bytes as an array of <see cref="ArraySegment{T}" /> of <see cref="Byte" />. Returned
        ///     segments
        ///     share the content with this buffer. This method does not
        ///     modify <see cref="ReaderIndex" /> or <see cref="WriterIndex" /> of this buffer.  Please note that
        ///     returned segments will not see the changes of this buffer if this buffer is a dynamic
        ///     buffer and it adjusted its capacity.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///     if this buffer cannot represent its content with <see cref="ArraySegment{T}" />
        ///     of <see cref="Byte" />
        /// </exception>
        /// <seealso cref="IoBufferCount" />
        /// <seealso cref="GetIoBuffer()" />
        /// <seealso cref="GetIoBuffer(int,int)" />
        ArraySegment<byte>[] GetIoBuffers();

        /// <summary>
        ///     Exposes this buffer's bytes as an array of <see cref="ArraySegment{T}" /> of <see cref="Byte" /> for the specified
        ///     index and length.
        ///     Returned segments share the content with this buffer. This method does
        ///     not modify <see cref="ReaderIndex" /> or <see cref="WriterIndex" /> of this buffer. Please note that
        ///     returned segments will not see the changes of this buffer if this buffer is a dynamic
        ///     buffer and it adjusted its capacity.
        /// </summary>
        /// <exception cref="NotSupportedException">
        ///     if this buffer cannot represent its content with <see cref="ArraySegment{T}" />
        ///     of <see cref="Byte" />
        /// </exception>
        /// <seealso cref="IoBufferCount" />
        /// <seealso cref="GetIoBuffer()" />
        /// <seealso cref="GetIoBuffer(int,int)" />
        ArraySegment<byte>[] GetIoBuffers(int index, int length);

        /// <summary>
        ///     Flag that indicates if this <see cref="IByteBuffer" /> is backed by a byte array or not
        /// </summary>
        bool HasArray { get; }

        /// <summary>
        ///     Grabs the underlying byte array for this buffer
        /// </summary>
        byte[] Array { get; }

        /// <summary>
        /// Returns {@code true} if and only if this buffer has a reference to the low-level memory address that points
        /// to the backing data.
        /// </summary>
        bool HasMemoryAddress { get; }

        /// <summary>
        ///  Returns the low-level memory address that point to the first byte of ths backing data.
        /// </summary>
        /// <returns>The low-level memory address</returns>
        ref byte GetPinnableMemoryAddress();

        /// <summary>
        /// Returns the pointer address of the buffer if the memory is pinned.
        /// </summary>
        /// <returns>IntPtr.Zero if not pinned.</returns>
        IntPtr AddressOfPinnedMemory();

        /// <summary>
        ///     Creates a deep clone of the existing byte array and returns it
        /// </summary>
        IByteBuffer Duplicate();

        IByteBuffer RetainedDuplicate();

        /// <summary>
        ///     Unwraps a nested buffer
        /// </summary>
        IByteBuffer Unwrap();

        /// <summary>
        ///     Returns a copy of this buffer's readable bytes. Modifying the content of the 
        ///     returned buffer or this buffer does not affect each other at all.This method is 
        ///     identical to {@code buf.copy(buf.readerIndex(), buf.readableBytes())}.
        ///     This method does not modify {@code readerIndex} or {@code writerIndex} of this buffer.
        ///</summary>
        IByteBuffer Copy();
        IByteBuffer Copy(int index, int length);

        IByteBuffer Slice();
        IByteBuffer RetainedSlice();
        IByteBuffer Slice(int index, int length);
        IByteBuffer RetainedSlice(int index, int length);

        int ArrayOffset { get; }

        IByteBuffer ReadSlice(int length);
        IByteBuffer ReadRetainedSlice(int length);

        string ToString();
        string ToString(Encoding encoding);
        string ToString(int index, int length, Encoding encoding);
    }
}
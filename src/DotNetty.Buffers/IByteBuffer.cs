// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Buffers
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Common;
    using DotNetty.Common.Utilities;

    /// <summary>
    ///     Inspired by the Netty ByteBuffer implementation
    ///     (https://github.com/netty/netty/blob/master/buffer/src/main/java/io/netty/buffer/ByteBuf.java)
    ///     Provides circular-buffer-esque security around a byte array, allowing reads and writes to occur independently.
    ///     In general, the <see cref="T:DotNetty.Buffers.IByteBuffer" /> guarantees:
    ///     /// <see cref="P:DotNetty.Buffers.IByteBuffer.ReaderIndex" /> LESS THAN OR EQUAL TO <see cref="P:DotNetty.Buffers.IByteBuffer.WriterIndex" /> LESS THAN OR EQUAL TO
    ///     <see cref="P:DotNetty.Buffers.IByteBuffer.Capacity" />.
    /// </summary>
    public interface IByteBuffer : IByteBufferProvider, IReferenceCounted, IComparable<IByteBuffer>, IEquatable<IByteBuffer>
    {
        int Capacity { get; }

        /// <summary>
        ///     Expands the capacity of this buffer so long as it is less than <see cref="MaxCapacity" />.
        /// </summary>
        IByteBuffer AdjustCapacity(int newCapacity);

        int MaxCapacity { get; }

        /// <summary>
        ///     The allocator who created this buffer
        /// </summary>
        IByteBufferAllocator Allocator { get; }

        bool IsDirect { get; }

        int ReaderIndex { get; }

        int WriterIndex { get; }

        /// <summary>
        ///     Sets the <see cref="WriterIndex" /> of this buffer
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">thrown if <see cref="WriterIndex" /> exceeds the length of the buffer</exception>
        IByteBuffer SetWriterIndex(int writerIndex);

        /// <summary>
        ///     Sets the <see cref="ReaderIndex" /> of this buffer
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        ///     thrown if <see cref="ReaderIndex" /> is greater than
        ///     <see cref="WriterIndex" /> or less than <c>0</c>.
        /// </exception>
        IByteBuffer SetReaderIndex(int readerIndex);

        /// <summary>
        ///     Sets both indexes
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        ///     thrown if <see cref="WriterIndex" /> or <see cref="ReaderIndex" /> exceeds
        ///     the length of the buffer
        /// </exception>
        IByteBuffer SetIndex(int readerIndex, int writerIndex);

        int ReadableBytes { get; }

        int WritableBytes { get; }

        int MaxWritableBytes { get; }

        /// <summary>
        ///     Returns true if <see cref="WriterIndex" /> - <see cref="ReaderIndex" /> is greater than <c>0</c>.
        /// </summary>
        bool IsReadable();

        /// <summary>
        ///     Is the buffer readable if and only if the buffer contains equal or more than the specified number of elements
        /// </summary>
        /// <param name="size">The number of elements we would like to read</param>
        bool IsReadable(int size);

        /// <summary>
        ///     Returns true if and only if <see cref="Capacity" /> - <see cref="WriterIndex" /> is greater than zero.
        /// </summary>
        bool IsWritable();

        /// <summary>
        ///     Returns true if and only if the buffer has enough <see cref="Capacity" /> to accomodate <paramref name="size" />
        ///     additional bytes.
        /// </summary>
        /// <param name="size">The number of additional elements we would like to write.</param>
        bool IsWritable(int size);

        /// <summary>
        ///     Sets the <see cref="WriterIndex" /> and <see cref="ReaderIndex" /> to <c>0</c>. Does not erase any of the data
        ///     written into the buffer already,
        ///     but it will overwrite that data.
        /// </summary>
        IByteBuffer Clear();

        /// <summary>
        ///     Marks the current <see cref="ReaderIndex" /> in this buffer. You can reposition the current
        ///     <see cref="ReaderIndex" />
        ///     to the marked <see cref="ReaderIndex" /> by calling <see cref="ResetReaderIndex" />.
        ///     The initial value of the marked <see cref="ReaderIndex" /> is <c>0</c>.
        /// </summary>
        IByteBuffer MarkReaderIndex();

        /// <summary>
        ///     Repositions the current <see cref="ReaderIndex" /> to the marked <see cref="ReaderIndex" /> in this buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        ///     is thrown if the current <see cref="WriterIndex" /> is less than the
        ///     marked <see cref="ReaderIndex" />
        /// </exception>
        IByteBuffer ResetReaderIndex();

        /// <summary>
        ///     Marks the current <see cref="WriterIndex" /> in this buffer. You can reposition the current
        ///     <see cref="WriterIndex" />
        ///     to the marked <see cref="WriterIndex" /> by calling <see cref="ResetWriterIndex" />.
        ///     The initial value of the marked <see cref="WriterIndex" /> is <c>0</c>.
        /// </summary>
        IByteBuffer MarkWriterIndex();

        /// <summary>
        ///     Repositions the current <see cref="WriterIndex" /> to the marked <see cref="WriterIndex" /> in this buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        ///     is thrown if the current <see cref="ReaderIndex" /> is greater than the
        ///     marked <see cref="WriterIndex" />
        /// </exception>
        IByteBuffer ResetWriterIndex();

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
        ///     Makes sure the number of <see cref="WritableBytes" /> is equal to or greater than
        ///     the specified value (<paramref name="minWritableBytes" />.) If there is enough writable bytes in this buffer,
        ///     the method returns with no side effect. Otherwise, it raises an <see cref="ArgumentOutOfRangeException" />.
        /// </summary>
        /// <param name="minWritableBytes">The expected number of minimum writable bytes</param>
        /// <exception cref="IndexOutOfRangeException">
        ///     if <see cref="WriterIndex" /> + <paramref name="minWritableBytes" /> >
        ///     <see cref="MaxCapacity" />.
        /// </exception>
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

        int IndexOf(int fromIndex, int toIndex, byte value);

        int BytesBefore(byte value);

        int BytesBefore(int length, byte value);

        int BytesBefore(int index, int length, byte value);

        string ToString();

        string ToString(Encoding encoding);

        string ToString(int index, int length, Encoding encoding);

        /// <summary>
        ///     Iterates over the readable bytes of this buffer with the specified <c>processor</c> in ascending order.
        /// </summary>
        /// <returns>
        ///     <c>-1</c> if the processor iterated to or beyond the end of the readable bytes.
        ///     The last-visited index If the <see cref="IByteProcessor.Process(byte)" /> returned <c>false</c>.
        /// </returns>
        /// <param name="processor">Processor.</param>
        int ForEachByte(IByteProcessor processor);

        /// <summary>
        ///     Iterates over the specified area of this buffer with the specified <paramref name="processor"/> in ascending order.
        ///     (i.e. <paramref name="index"/>, <c>(index + 1)</c>,  .. <c>(index + length - 1)</c>)
        /// </summary>
        /// <returns>
        ///     <c>-1</c> if the processor iterated to or beyond the end of the specified area.
        ///     The last-visited index If the <see cref="IByteProcessor.Process(byte)"/> returned <c>false</c>.
        /// </returns>
        /// <param name="index">Index.</param>
        /// <param name="length">Length.</param>
        /// <param name="processor">Processor.</param>
        int ForEachByte(int index, int length, IByteProcessor processor);

        /// <summary>
        ///     Iterates over the readable bytes of this buffer with the specified <paramref name="processor"/> in descending order.
        /// </summary>
        /// <returns>
        ///     <c>-1</c> if the processor iterated to or beyond the beginning of the readable bytes.
        ///     The last-visited index If the <see cref="IByteProcessor.Process(byte)"/> returned <c>false</c>.
        /// </returns>
        /// <param name="processor">Processor.</param>
        int ForEachByteDesc(IByteProcessor processor);

        /// <summary>
        ///     Iterates over the specified area of this buffer with the specified <paramref name="processor"/> in descending order.
        ///     (i.e. <c>(index + length - 1)</c>, <c>(index + length - 2)</c>, ... <paramref name="index"/>)
        /// </summary>
        /// <returns>
        ///     <c>-1</c> if the processor iterated to or beyond the beginning of the specified area.
        ///     The last-visited index If the <see cref="IByteProcessor.Process(byte)"/> returned <c>false</c>.
        /// </returns>
        /// <param name="index">Index.</param>
        /// <param name="length">Length.</param>
        /// <param name="processor">Processor.</param>
        int ForEachByteDesc(int index, int length, IByteProcessor processor);
    }
}
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using DotNetty.Common.Internal;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;

namespace DotNetty.Buffers
{
    public static class ByteBufferUtil
    {
        const char WriteUtfUnknown = '?';
        static readonly int MaxBytesPerCharUtf8 = Encoding.UTF8.GetMaxByteCount(1);

        /// <summary>
        ///     Calculates the hash code of the specified buffer.  This method is
        ///     useful when implementing a new buffer type.
        /// </summary>
        public static int HashCode(IByteBuffer buffer)
        {
            int aLen = buffer.ReadableBytes;
            int intCount = (int)((uint)aLen >> 2);
            int byteCount = aLen & 3;
        
            int hashCode = 1;
            int arrayIndex = buffer.ReaderIndex;
            for (int i = intCount; i > 0; i--)
            {
                hashCode = 31 * hashCode + buffer.Get<int>(arrayIndex);
                arrayIndex += 4;
            }
        
            for (int i = byteCount; i > 0; i--)
            {
                hashCode = 31 * hashCode + buffer.Get<byte>(arrayIndex++);
            }
        
            if (hashCode == 0)
            {
                hashCode = 1;
            }
        
            return hashCode;
        }
        
        /// <summary>
        ///     Returns {@code true} if and only if the two specified buffers are
        ///     identical to each other for {@code length} bytes starting at {@code aStartIndex}
        ///     index for the {@code a} buffer and {@code bStartIndex} index for the {@code b} buffer.
        ///     A more compact way to express this is:
        ///     <p />
        ///     {@code a[aStartIndex : aStartIndex + length] == b[bStartIndex : bStartIndex + length]}
        /// </summary>
        public static bool Equals(IByteBuffer a, int aStartIndex, IByteBuffer b, int bStartIndex, int length)
        {
            if (aStartIndex < 0 || bStartIndex < 0 || length < 0)
            {
                throw new ArgumentException("All indexes and lengths must be non-negative");
            }
            if (a.WriterIndex - length < aStartIndex || b.WriterIndex - length < bStartIndex)
            {
                return false;
            }
        
            int longCount = unchecked((int)((uint)length >> 3));
            int byteCount = length & 7;
        
            for (int i = longCount; i > 0; i--)
            {
                if (a.Get<long>(aStartIndex) != b.Get<long>(bStartIndex))
                {
                    return false;
                }
                aStartIndex += 8;
                bStartIndex += 8;
            }
        
            for (int i = byteCount; i > 0; i--)
            {
                if (a.Get<byte>(aStartIndex) != b.Get<byte>(bStartIndex))
                {
                    return false;
                }
                aStartIndex++;
                bStartIndex++;
            }
        
            return true;
        }
        
        /// <summary>
        ///     Compares the two specified buffers as described in {@link ByteBuf#compareTo(ByteBuf)}.
        ///     This method is useful when implementing a new buffer type.
        /// </summary>
        public static int Compare(IByteBuffer bufferA, IByteBuffer bufferB)
        {
            int aLen = bufferA.ReadableBytes;
            int bLen = bufferB.ReadableBytes;
            int minLength = Math.Min(aLen, bLen);
            int uintCount = minLength.RightUShift(2);
            int byteCount = minLength & 3;
        
            int aIndex = bufferA.ReaderIndex;
            int bIndex = bufferB.ReaderIndex;
        
            if (uintCount > 0)
            {
                int uintCountIncrement = uintCount << 2;
                int res = CompareUint(bufferA, bufferB, aIndex, bIndex, uintCountIncrement);
                if (res != 0)
                {
                    return res;
                }
        
                aIndex += uintCountIncrement;
                bIndex += uintCountIncrement;
            }
        
            for (int aEnd = aIndex + byteCount; aIndex < aEnd; ++aIndex, ++bIndex)
            {
                int comp = bufferA.Get<byte>(aIndex) - bufferB.Get<byte>(bIndex);
                if (comp != 0)
                {
                    return comp;
                }
            }
        
            return aLen - bLen;
        }
        
        static int CompareUint(IByteBuffer bufferA, IByteBuffer bufferB, int aIndex, int bIndex, int uintCountIncrement)
        {
            for (int aEnd = aIndex + uintCountIncrement; aIndex < aEnd; aIndex += 4, bIndex += 4)
            {
                long va = bufferA.Get<uint>(aIndex);
                long vb = bufferB.Get<uint>(bIndex);
                if (va > vb)
                {
                    return 1;
                }
                if (va < vb)
                {
                    return -1;
                }
            }
            return 0;
        }
        
        /// <summary>
        /// The default implementation of <see cref="IByteBuffer.IndexOf(int, int, byte)"/>.
        /// This method is useful when implementing a new buffer type.
        /// </summary>
        private static int IndexOf0(IByteBuffer buffer, int fromIndex, int toIndex, byte value)
        {
            if (fromIndex <= toIndex)
            {
                return FirstIndexOf(buffer, fromIndex, toIndex, value);
            }
            else
            {
                return LastIndexOf(buffer, fromIndex, toIndex, value);
            }
        }
        
        static int FirstIndexOf(IByteBuffer buffer, int fromIndex, int toIndex, byte value)
        {
            fromIndex = Math.Max(fromIndex, 0);
            if (fromIndex >= toIndex || buffer.Capacity == 0)
            {
                return -1;
            }
        
            return ForEachByte(buffer, fromIndex, toIndex - fromIndex, new IndexOfProcessor(value));

            return ForEachByte(buffer, fromIndex, toIndex - fromIndex, new IndexOfProcessor(value));
        }
        
        static int LastIndexOf(IByteBuffer buffer, int fromIndex, int toIndex, byte value)
        {
            fromIndex = Math.Min(fromIndex, buffer.Capacity);
            if (fromIndex < 0 || buffer.Capacity == 0)
            {
                return -1;
            }
        
            return ForEachByteDesc(buffer, toIndex, fromIndex - toIndex, new IndexOfProcessor(value));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToCodePoint(char high, char low)
        {
            // See RFC 2781, Section 2.2
            // http://www.faqs.org/rfcs/rfc2781.html
            int h = (high & 0x3FF) << 10;
            int l = low & 0x3FF;
            return (h | l) + 0x10000;
        }
        
        // Fast-Path implementation
        internal static int WriteUtf8(AbstractByteBuffer buffer, int writerIndex, string value, int len)
        {
            int oldWriterIndex = writerIndex;
        
            // We can use the _set methods as these not need to do any index checks and reference checks.
            // This is possible as we called ensureWritable(...) before.
            for (int i = 0; i < len; i++)
            {
                char c = value[i];
                if (c < 0x80)
                {
                    buffer._Set<byte>(writerIndex++, (byte)c);
                }
                else if (c < 0x800)
                {
                    buffer._Set<byte>(writerIndex++, (byte)(0xc0 | (c >> 6)));
                    buffer._Set<byte>(writerIndex++, (byte)(0x80 | (c & 0x3f)));
                }
                else if (char.IsSurrogate(c))
                {
                    if (!char.IsHighSurrogate(c))
                    {
                        buffer._Set<byte>(writerIndex++, (byte)WriteUtfUnknown);
                        continue;
                    }
                    char c2;
                    try
                    {
                        // Surrogate Pair consumes 2 characters. Optimistically try to get the next character to avoid
                        // duplicate bounds checking with charAt. If an IndexOutOfBoundsException is thrown we will
                        // re-throw a more informative exception describing the problem.
                        c2 = value[++i];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        buffer._Set<byte>(writerIndex++, (byte)WriteUtfUnknown);
                        break;
                    }
                    if (!char.IsLowSurrogate(c2))
                    {
                        buffer._Set<byte>(writerIndex++, (byte)WriteUtfUnknown);
                        buffer._Set<byte>(writerIndex++, char.IsHighSurrogate(c2) ? (byte)WriteUtfUnknown : (byte)c2);
                        continue;
                    }
                    int codePoint = ToCodePoint(c, c2);
                    // See http://www.unicode.org/versions/Unicode7.0.0/ch03.pdf#G2630.
                    buffer._Set<byte>(writerIndex++, (byte)(0xf0 | (codePoint >> 18)));
                    buffer._Set<byte>(writerIndex++, (byte)(0x80 | ((codePoint >> 12) & 0x3f)));
                    buffer._Set<byte>(writerIndex++, (byte)(0x80 | ((codePoint >> 6) & 0x3f)));
                    buffer._Set<byte>(writerIndex++, (byte)(0x80 | (codePoint & 0x3f)));
                }
                else
                {
                    buffer._Set<byte>(writerIndex++, (byte)(0xe0 | (c >> 12)));
                    buffer._Set<byte>(writerIndex++, (byte)(0x80 | ((c >> 6) & 0x3f)));
                    buffer._Set<byte>(writerIndex++, (byte)(0x80 | (c & 0x3f)));
                }
            }
            
            return writerIndex - oldWriterIndex;
        }
        
        // internal static int Utf8MaxBytes(ICharSequence seq) => Utf8MaxBytes(seq.Count);
        
        public static int Utf8MaxBytes(string seq) => Utf8MaxBytes(seq.Length);
        
        internal static int Utf8MaxBytes(int seqLength) => seqLength * MaxBytesPerCharUtf8;
        
        internal static int WriteAscii(AbstractByteBuffer buffer, int writerIndex, string value, int len)
        {
            // We can use the _set methods as these not need to do any index checks and reference checks.
            // This is possible as we called ensureWritable(...) before.
            for (int i = 0; i < len; i++)
            {
                buffer._Set<byte>(writerIndex++, (byte)value[i]);
            }
            return len;
        }
        
        /// <summary>
        ///     Encode the given <see cref="string" /> using the given <see cref="Encoding" /> into a new
        ///     <see cref="IByteBuffer" /> which
        ///     is allocated via the <see cref="IByteBufferAllocator" />.
        /// </summary>
        /// <param name="alloc">The <see cref="IByteBufferAllocator" /> to allocate {@link IByteBuffer}.</param>
        /// <param name="src">src The <see cref="string" /> to encode.</param>
        /// <param name="encoding">charset The specified <see cref="Encoding" /></param>
        public static IByteBuffer EncodeString(IByteBufferAllocator alloc, string src, Encoding encoding) => EncodeString0(alloc, false, src, encoding, 0);
        
        // /// <summary>
        // ///     Encode the given <see cref="string" /> using the given <see cref="Encoding" /> into a new
        // ///     <see cref="IByteBuffer" /> which
        // ///     is allocated via the <see cref="IByteBufferAllocator" />.
        // /// </summary>
        // /// <param name="alloc">The <see cref="IByteBufferAllocator" /> to allocate {@link IByteBuffer}.</param>
        // /// <param name="src">src The <see cref="string" /> to encode.</param>
        // /// <param name="encoding">charset The specified <see cref="Encoding" /></param>
        // /// <param name="extraCapacity">the extra capacity to alloc except the space for decoding.</param>
        // public static IByteBuffer EncodeString(IByteBufferAllocator alloc, string src, Encoding encoding, int extraCapacity) => EncodeString0(alloc, false, src, encoding, extraCapacity);
        
        internal static IByteBuffer EncodeString0(IByteBufferAllocator alloc, bool enforceHeap, string src, Encoding encoding, int extraCapacity)
        {
            int length = encoding.GetMaxByteCount(src.Length) + extraCapacity;
            bool release = true;
        
            IByteBuffer dst = enforceHeap ? alloc.Buffer(length) : alloc.Buffer(length);
            Contract.Assert(dst.HasArray, "Operation expects allocator to operate array-based buffers.");
        
            try
            {
                int written = encoding.GetBytes(src, 0, src.Length, dst.Array, dst.ArrayOffset + dst.WriterIndex);
                dst.SetWriterIndex(dst.WriterIndex + written);
                release = false;
        
                return dst;
            }
            finally
            {
                if (release)
                {
                    dst.Release();
                }
            }
        }
        
        public static string DecodeString(IByteBuffer src, int readerIndex, int len, Encoding encoding)
        {
            if (len == 0)
            {
                return string.Empty;
            }
        
            if (src.IoBufferCount == 1)
            {
                ArraySegment<byte> ioBuf = src.GetIoBuffer(readerIndex, len);
                return encoding.GetString(ioBuf.Array, ioBuf.Offset, ioBuf.Count);
            }
            else
            {
                int maxLength = encoding.GetMaxCharCount(len);
                IByteBuffer buffer = ByteBuffer.Allocator.Buffer(maxLength);
                try
                {
                    buffer.WriteBytes(src, readerIndex, len);
                    ArraySegment<byte> ioBuf = buffer.GetIoBuffer();
                    return encoding.GetString(ioBuf.Array, ioBuf.Offset, ioBuf.Count);
                }
                finally
                {
                    // Release the temporary buffer again.
                    buffer.Release();
                }
            }
        }
        
        /// <summary>
        ///     Toggles the endianness of the specified 64-bit long integer.
        /// </summary>
        public static long SwapLong(long value)
            => ((SwapInt((int)value) & 0xFFFFFFFF) << 32)
                | (SwapInt((int)(value >> 32)) & 0xFFFFFFFF);
        
        /// <summary>
        ///     Toggles the endianness of the specified 32-bit integer.
        /// </summary>
        public static int SwapInt(int value)
            => ((SwapShort((short)value) & 0xFFFF) << 16)
                | (SwapShort((short)(value >> 16)) & 0xFFFF);
        
        /// <summary>
        ///     Toggles the endianness of the specified 16-bit integer.
        /// </summary>
        public static short SwapShort(short value) => (short)(((value & 0xFF) << 8) | (value >> 8) & 0xFF);
        
        public static int ForEachByte(IByteBuffer buffer, IByteProcessor processor)
        {
            while (true)
            {
                switch (buffer)
                {
                    case WrappedByteBuffer wrappedByteBuffer:
                        buffer = wrappedByteBuffer.Unwrap();
                        continue;
                    case WrappedCompositeByteBuffer wrappedCompositeByteBuffer:
                        buffer = wrappedCompositeByteBuffer.Unwrap();
                        continue;
                    case EmptyByteBuffer _:
                        return -1;
                    case AbstractByteBuffer abstractByteBuffer:
                    {
                        if (buffer.ReferenceCount == 0) ThrowHelper.ThrowIllegalReferenceCountException(0);
                        return ForEachByteAsc0(abstractByteBuffer, buffer.ReaderIndex, buffer.WriterIndex, processor);
                    }
                }

                throw new InvalidOperationException();
            }
        }
        
        public static int ForEachByte(IByteBuffer buffer, int index, int length, IByteProcessor processor)
        {
            while (true)
            {
                switch (buffer)
                {
                    case WrappedByteBuffer wrappedByteBuffer:
                        buffer = wrappedByteBuffer.Unwrap();
                        continue;
                    case WrappedCompositeByteBuffer wrappedCompositeByteBuffer:
                        buffer = wrappedCompositeByteBuffer.Unwrap();
                        continue;
                    case DuplicateByteBuffer duplicateByteBuffer:
                        buffer = duplicateByteBuffer.Unwrap();
                        continue;
                    case SlicedByteBuffer slicedByteBuffer:
                        slicedByteBuffer.CheckIndex0(index, length);
                        var ret = ForEachByte(slicedByteBuffer.Unwrap(), slicedByteBuffer.Idx(index), length, processor);
                        return ret >= slicedByteBuffer.Adjustment ? ret - slicedByteBuffer.Adjustment : -1;
                    case EmptyByteBuffer emptyByteBuffer:
                        emptyByteBuffer.CheckIndex(index, length);
                        return -1;
                    case AbstractByteBuffer abstractByteBuffer:
                    {
                        abstractByteBuffer.CheckIndex(index, length);
                        return ForEachByteAsc0(abstractByteBuffer, index, index + length, processor);
                    }
                }
                
                throw new InvalidOperationException();
            }
        }
        
        private static int ForEachByteAsc0(AbstractByteBuffer buffer, int start, int end, IByteProcessor processor)
        {
            for (; start < end; ++start)
            {
                if (!processor.Process(buffer._Get<byte>(start)))
                {
                    return start;
                }
            }

            return -1;
        }
        
        public static int ForEachByteDesc(IByteBuffer buffer, IByteProcessor processor)
        {
            while (true)
            {
                switch (buffer)
                {
                    case WrappedByteBuffer wrappedByteBuffer:
                        buffer = wrappedByteBuffer.Unwrap();
                        continue;
                    case WrappedCompositeByteBuffer wrappedCompositeByteBuffer:
                        buffer = wrappedCompositeByteBuffer.Unwrap();
                        continue;
                    case EmptyByteBuffer _:
                        return -1;
                    case AbstractByteBuffer abstractByteBuffer:
                    {
                        if (abstractByteBuffer.ReferenceCount == 0) ThrowHelper.ThrowIllegalReferenceCountException(0);
                        return ForEachByteDesc0(abstractByteBuffer, abstractByteBuffer.WriterIndex - 1, abstractByteBuffer.ReaderIndex, processor);
                    }
                }
                
                throw new InvalidOperationException();
            }
        }
        
        public static int ForEachByteDesc(IByteBuffer buffer, int index, int length, IByteProcessor processor)
        {
            while (true)
            {
                switch (buffer)
                {
                    case WrappedByteBuffer wrappedByteBuffer:
                        buffer = wrappedByteBuffer.Unwrap();
                        continue;
                    case WrappedCompositeByteBuffer wrappedCompositeByteBuffer:
                        buffer = wrappedCompositeByteBuffer.Unwrap();
                        continue;
                    case DuplicateByteBuffer duplicateByteBuffer:
                        buffer = duplicateByteBuffer.Unwrap();
                        continue;
                    case SlicedByteBuffer slicedByteBuffer:
                        slicedByteBuffer.CheckIndex0(index, length);
                        var ret = ForEachByteDesc(slicedByteBuffer.Unwrap(), slicedByteBuffer.Idx(index), length, processor);
                        return ret >= slicedByteBuffer.Adjustment ? ret - slicedByteBuffer.Adjustment : -1;
                    case EmptyByteBuffer emptyByteBuffer:
                        emptyByteBuffer.CheckIndex(index, length);
                        return -1;
                    case AbstractByteBuffer abstractByteBuffer:
                    {
                        abstractByteBuffer.CheckIndex(index, length);
                        return ForEachByteDesc0(abstractByteBuffer, index + length - 1, index, processor);
                    }
                }

                throw new InvalidOperationException();
            }
        }
        
        private static int ForEachByteDesc0(AbstractByteBuffer buffer, int rStart, int rEnd, IByteProcessor processor)
        {
            for (; rStart >= rEnd; --rStart)
            {
                if (!processor.Process(buffer._Get<byte>(rStart)))
                {
                    return rStart;
                }
            }

            return -1;
        }
        
        public static int IndexOf(IByteBuffer buffer, int fromIndex, int toIndex, byte value)
        {
            while (true)
            {
                switch (buffer)
                {
                    case WrappedByteBuffer wrappedByteBuffer:
                        buffer = wrappedByteBuffer.Unwrap();
                        continue;
                    case WrappedCompositeByteBuffer wrappedCompositeByteBuffer:
                        buffer = wrappedCompositeByteBuffer.Unwrap();
                        continue;
                    case EmptyByteBuffer emptyByteBuffer:
                        emptyByteBuffer.CheckIndex(fromIndex);
                        emptyByteBuffer.CheckIndex(toIndex);
                        return -1;
                    case AbstractByteBuffer _:
                        return ByteBufferUtil.IndexOf(buffer, fromIndex, toIndex, value);
                }

                throw new InvalidOperationException();
            }
        }
        
        public static int BytesBefore(IByteBuffer buffer, byte value)
        {
            while (true)
            {
                switch (buffer)
                {
                    case WrappedByteBuffer wrappedByteBuffer:
                        buffer = wrappedByteBuffer.Unwrap();
                        continue;
                    case EmptyByteBuffer _:
                        return -1;
                    case AbstractByteBuffer abstractByteBuffer:
                        return BytesBefore(abstractByteBuffer, buffer.ReaderIndex, buffer.ReadableBytes, value);
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
        
        public static int BytesBefore(IByteBuffer buffer, int length, byte value)
        {
            switch (buffer)
            {
                case EmptyByteBuffer emptyByteBuffer:
                    emptyByteBuffer.CheckLength(length);
                    return -1;
                case AbstractByteBuffer abstractByteBuffer:
                    abstractByteBuffer.CheckReadableBytes(length);
                    return BytesBefore(abstractByteBuffer, buffer.ReaderIndex, length, value);
            }

            throw new InvalidOperationException();
        }
        
        public static int BytesBefore(IByteBuffer buffer, int index, int length, byte value)
        {
            while (true)
            {
                switch (buffer)
                {
                    case EmptyByteBuffer emptyByteBuffer:
                        emptyByteBuffer.CheckIndex(index, length);
                        return -1;
                    case WrappedCompositeByteBuffer wrappedCompositeByteBuffer:
                        var index1 = index;
                        buffer = wrappedCompositeByteBuffer.Unwrap();
                        length = index1 + length;
                        continue;
                    case AbstractByteBuffer _:
                    {
                        int endIndex = IndexOf0(buffer, index, index + length, value);
                        if (endIndex < 0)
                        {
                            return -1;
                        }

                        return endIndex - index;
                    }
                    default:
                        throw new InvalidOperationException();
                }
            }
        }
    }
}
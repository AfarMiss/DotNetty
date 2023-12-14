using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace DotNetty.Buffers
{
    public static partial class ByteBuffer
    {
        public static readonly ByteBufferAllocator Allocator = ByteBufferAllocator.Default;
        
        public static readonly IByteBuffer Empty = Allocator.Buffer(0, 0);
        
        public static IByteBuffer Buffer() => Allocator.Buffer();
        public static IByteBuffer Buffer(int initialCapacity) => Allocator.Buffer(initialCapacity);
        public static IByteBuffer Buffer(int initialCapacity, int maxCapacity) => Allocator.Buffer(initialCapacity, maxCapacity);
        
        /// <summary>
        ///     Creates a new big-endian buffer which wraps the specified array.
        ///     A modification on the specified array's content will be visible to the returned buffer.
        /// </summary>
        public static IByteBuffer WrappedBuffer(byte[] array) =>
            array.Length == 0 ? Empty  : new HeapByteBuffer(Allocator, array, array.Length);
        
        /// <summary>
        ///     Creates a new big-endian buffer which wraps the sub-region of the
        ///     specified array. A modification on the specified array's content 
        ///     will be visible to the returned buffer.
        /// </summary>
        public static IByteBuffer WrappedBuffer(byte[] array, int offset, int length)
        {
            if (length == 0)
            {
                return Empty;
            }
        
            if (offset == 0 && length == array.Length)
            {
                return WrappedBuffer(array);
            }
        
            return WrappedBuffer(array).Slice(offset, length);
        }
        
        /// <summary>
        ///     Creates a new buffer which wraps the specified buffer's readable bytes.
        ///     A modification on the specified buffer's content will be visible to the returned buffer.
        /// </summary>
        /// <param name="buffer">The buffer to wrap. Reference count ownership of this variable is transfered to this method.</param>
        /// <returns>The readable portion of the buffer, or an empty buffer if there is no readable portion.</returns>
        public static IByteBuffer WrappedBuffer(IByteBuffer buffer)
        {
            if (buffer.IsReadable())
            {
                return buffer.Slice();
            }
            else
            {
                buffer.Release();
                return Empty;
            }
        }
        
        /// <summary>
        ///     Creates a new big-endian composite buffer which wraps the specified arrays without copying them.
        ///     A modification on the specified arrays' content will be visible to the returned buffer.
        /// </summary>
        public static IByteBuffer WrappedBuffer(params byte[][] arrays) => WrappedBuffer(AbstractByteBufferAllocator.DefaultMaxComponents, arrays);
        
        /// <summary>
        ///     Creates a new big-endian composite buffer which wraps the readable bytes of the specified buffers without copying them. 
        ///     A modification on the content of the specified buffers will be visible to the returned buffer.
        /// </summary>
        /// <param name="buffers">The buffers to wrap. Reference count ownership of all variables is transfered to this method.</param>
        /// <returns>The readable portion of the buffers. The caller is responsible for releasing this buffer.</returns>
        public static IByteBuffer WrappedBuffer(params IByteBuffer[] buffers) => WrappedBuffer(AbstractByteBufferAllocator.DefaultMaxComponents, buffers);
        
        /// <summary>
        ///     Creates a new big-endian composite buffer which wraps the specified arrays without copying them.
        ///     A modification on the specified arrays' content will be visible to the returned buffer.
        /// </summary>
        public static IByteBuffer WrappedBuffer(int maxNumComponents, params byte[][] arrays)
        {
            switch (arrays.Length)
            {
                case 0:
                    break;
                case 1:
                    if (arrays[0].Length != 0)
                    {
                        return WrappedBuffer(arrays[0]);
                    }
                    break;
                default:
                    // Get the list of the component, while guessing the byte order.
                    var components = new List<IByteBuffer>(arrays.Length);
                    foreach (byte[] array in arrays)
                    {
                        if (array == null)
                        {
                            break;
                        }
                        if (array.Length > 0)
                        {
                            components.Add(WrappedBuffer(array));
                        }
                    }
        
                    if (components.Count > 0)
                    {
                        return new CompositeByteBuffer(Allocator, maxNumComponents, components);
                    }
                    break;
            }
        
            return Empty;
        }
        
        /// <summary>
        ///     Creates a new big-endian composite buffer which wraps the readable bytes of the specified buffers without copying them.
        ///     A modification on the content of the specified buffers will be visible to the returned buffer.
        /// </summary>
        /// <param name="maxNumComponents">Advisement as to how many independent buffers are allowed to exist before consolidation occurs.</param>
        /// <param name="buffers">The buffers to wrap. Reference count ownership of all variables is transfered to this method.</param>
        /// <returns>The readable portion of the buffers. The caller is responsible for releasing this buffer.</returns>
        public static IByteBuffer WrappedBuffer(int maxNumComponents, params IByteBuffer[] buffers)
        {
            switch (buffers.Length)
            {
                case 0:
                    break;
                case 1:
                    IByteBuffer buffer = buffers[0];
                    if (buffer.IsReadable())
                        return WrappedBuffer(buffer);
                    else
                        buffer.Release();
                    break;
                default:
                    for (int i = 0; i < buffers.Length; i++)
                    {
                        IByteBuffer buf = buffers[i];
                        if (buf.IsReadable())
                            return new CompositeByteBuffer(Allocator, maxNumComponents, buffers, i, buffers.Length);
                        else
                            buf.Release();
                    }
                    break;
            }
        
            return Empty;
        }
        
        public static CompositeByteBuffer CompositeBuffer() => CompositeBuffer(AbstractByteBufferAllocator.DefaultMaxComponents);
        
        public static CompositeByteBuffer CompositeBuffer(int maxNumComponents) => new CompositeByteBuffer(Allocator, maxNumComponents);
        
        /// <summary>
        ///     Creates a new big-endian buffer whose content is a copy of the specified array
        ///     The new buffer's <see cref="IByteBuffer.ReaderIndex" /> and <see cref="IByteBuffer.WriterIndex" />
        ///     are <c>0</c> and <see cref="Array.Length" /> respectively.
        /// </summary>
        /// <param name="array">A buffer we're going to copy.</param>
        /// <returns>The new buffer that copies the contents of array.</returns>
        public static IByteBuffer CopiedBuffer(byte[] array)
        {
            if (array.Length == 0)
            {
                return Empty;
            }
        
            var newArray = new byte[array.Length];
            ByteBuffer.CopyMemory(array, 0, newArray, 0, array.Length);
        
            return WrappedBuffer(newArray);
        }
        
        /// <summary>
        ///     Creates a new big-endian buffer whose content is a copy of the specified array.
        ///     The new buffer's <see cref="IByteBuffer.ReaderIndex" /> and <see cref="IByteBuffer.WriterIndex" />
        ///     are <c>0</c> and <see cref="Array.Length" /> respectively.
        /// </summary>
        /// <param name="array">A buffer we're going to copy.</param>
        /// <param name="offset">The index offset from which we're going to read array.</param>
        /// <param name="length">
        ///     The number of bytes we're going to read from array beginning from position offset.
        /// </param>
        /// <returns>The new buffer that copies the contents of array.</returns>
        public static IByteBuffer CopiedBuffer(byte[] array, int offset, int length)
        {
            if (length == 0)
            {
                return Empty;
            }
        
            var copy = new byte[length];
            ByteBuffer.CopyMemory(array, offset, copy, 0, length);
            return WrappedBuffer(copy);
        }
        
        /// <summary>
        ///     Creates a new big-endian buffer whose content is a copy of the specified <see cref="Array" />.
        ///     The new buffer's <see cref="IByteBuffer.ReaderIndex" /> and <see cref="IByteBuffer.WriterIndex" />
        ///     are <c>0</c> and <see cref="IByteBuffer.Capacity" /> respectively.
        /// </summary>
        /// <param name="buffer">A buffer we're going to copy.</param>
        /// <returns>The new buffer that copies the contents of buffer.</returns>
        public static IByteBuffer CopiedBuffer(IByteBuffer buffer)
        {
            int readable = buffer.ReadableBytes;
            if (readable > 0)
            {
                IByteBuffer copy = Buffer(readable);
                copy.WriteBytes(buffer, buffer.ReaderIndex, readable);
                return copy;
            }
            else
            {
                return Empty;
            }
        }
        
        /// <summary>
        ///     Creates a new big-endian buffer whose content is a merged copy of of the specified arrays.
        ///     The new buffer's <see cref="IByteBuffer.ReaderIndex" /> and <see cref="IByteBuffer.WriterIndex" />
        ///     are <c>0</c> and <see cref="Array.Length" /> respectively.
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static IByteBuffer CopiedBuffer(params byte[][] arrays)
        {
            switch (arrays.Length)
            {
                case 0:
                    return Empty;
                case 1:
                    return arrays[0].Length == 0 ? Empty : CopiedBuffer(arrays[0]);
            }
        
            // Merge the specified arrays into one array.
            int length = 0;
            foreach (byte[] a in arrays)
            {
                if (int.MaxValue - length < a.Length)
                {
                    throw new ArgumentException("The total length of the specified arrays is too big.");
                }
                length += a.Length;
            }
        
            if (length == 0)
            {
                return Empty;
            }
        
            var mergedArray = new byte[length];
            for (int i = 0, j = 0; i < arrays.Length; i++)
            {
                byte[] a = arrays[i];
                ByteBuffer.CopyMemory(a, 0, mergedArray, j, a.Length);
                j += a.Length;
            }
        
            return WrappedBuffer(mergedArray);
        }
        
        /// <summary>
        ///     Creates a new big-endian buffer whose content  is a merged copy of the specified <see cref="Array" />.
        ///     The new buffer's <see cref="IByteBuffer.ReaderIndex" /> and <see cref="IByteBuffer.WriterIndex" />
        ///     are <c>0</c> and <see cref="IByteBuffer.Capacity" /> respectively.
        /// </summary>
        /// <param name="buffers">Buffers we're going to copy.</param>
        /// <returns>The new buffer that copies the contents of buffers.</returns>
        public static IByteBuffer CopiedBuffer(params IByteBuffer[] buffers)
        {
            switch (buffers.Length)
            {
                case 0:
                    return Empty;
                case 1:
                    return CopiedBuffer(buffers[0]);
            }
        
            // Merge the specified buffers into one buffer.
            int length = 0;
            foreach (IByteBuffer b in buffers)
            {
                int bLen = b.ReadableBytes;
                if (bLen <= 0)
                {
                    continue;
                }
                if (int.MaxValue - length < bLen)
                {
                    throw new ArgumentException("The total length of the specified buffers is too big.");
                }
        
                length += bLen;
            }
        
            if (length == 0)
            {
                return Empty;
            }
        
            var mergedArray = new byte[length];
            for (int i = 0, j = 0; i < buffers.Length; i++)
            {
                IByteBuffer b = buffers[i];
                int bLen = b.ReadableBytes;
                b.GetBytes(b.ReaderIndex, mergedArray, j, bLen);
                j += bLen;
            }
        
            return WrappedBuffer(mergedArray);
        }
        
        public static IByteBuffer CopiedBuffer(char[] array, int offset, int length, Encoding encoding)
        {
            Contract.Requires(array != null);
            return length == 0 ? Empty : CopiedBuffer(new string(array, offset, length), encoding);
        }
        
        public static IByteBuffer CopiedBuffer(string value, Encoding encoding) => ByteBufferUtil.EncodeString0(Allocator, true, value, encoding, 0);
        
        /// <summary>
        ///     Return a unreleasable view on the given {@link ByteBuf} which will just ignore release and retain calls.
        /// </summary>
        public static IByteBuffer KeepByteBuffer(IByteBuffer buffer) => new KeepByteBuffer(buffer);
    }
}
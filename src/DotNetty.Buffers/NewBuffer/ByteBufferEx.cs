using System;
using System.Runtime.CompilerServices;
using DotNetty.Common.Utilities;

namespace DotNetty.Buffers
{
    public static class ByteBufferEx
    {
        public static readonly ByteOrder DefaultByteOrder = BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

        public static unsafe T Read<T>(byte[] bytes, int index) where T : unmanaged
        {
            if (index+ sizeof(T) > bytes.Length) throw new IndexOutOfRangeException();
            return Unsafe.ReadUnaligned<T>(ref bytes[index]);
        }
        
        public static T Read<T>(IByteBuffer buffer, int index) where T : unmanaged
        {
            return Unsafe.ReadUnaligned<T>(ref buffer.Array[index]);
        }
        
        public static void Write<T>(byte[] bytes, int index, T value) where T : unmanaged
        {
            Unsafe.WriteUnaligned<T>(ref bytes[index], value);
        }
        
        public static void Write<T>(IByteBuffer buffer, int index, T value) where T : unmanaged
        {
            Unsafe.WriteUnaligned(ref buffer.Array[index], value);
        }

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
                        return ForEachByteDesc0(abstractByteBuffer, index, index + length, processor);
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
    }
}
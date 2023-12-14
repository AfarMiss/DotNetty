using System;
using System.Runtime.CompilerServices;

namespace DotNetty.Buffers
{
    public static partial class ByteBuffer
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
    }
}
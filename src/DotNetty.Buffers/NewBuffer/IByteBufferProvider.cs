using System;
using System.Text;

namespace DotNetty.Buffers
{
    public interface IByteBufferProvider
    {
        public T Get<T>(int index) where T : unmanaged;
        public void Set<T>(int index, T value) where T : unmanaged;
        public T Read<T>() where T : unmanaged;
        public void Write<T>(T value) where T : unmanaged;
        
        public void Set<T>(int index, int value) where T : unmanaged;
        public void Write<T>(int value) where T : unmanaged;
        
        public void GetBytes(int index, IByteBuffer dst, int? length = null);
        public void GetBytes(int index, IByteBuffer dst, int dstIndex, int length);
        public void GetBytes(int index, Span<byte> dst, int? length = null);
        public void GetBytes(int index, Span<byte> dst, int dstIndex, int length);

        public void SetBytes(int index, IByteBuffer src, int? length = null);
        public void SetBytes(int index, IByteBuffer src, int srcIndex, int length);
        public void SetBytes(int index, Span<byte> src, int? length = null);
        public void SetBytes(int index, Span<byte> src, int srcIndex, int length);
        
        void SkipBytes(int length);
        public void ReadBytes(IByteBuffer dst, int? length = null);
        public void ReadBytes(IByteBuffer dst, int dstIndex, int length);
        public void ReadBytes(Span<byte> dst, int? length = null);
        public void ReadBytes(Span<byte> dst, int dstIndex, int length);
        public void WriteBytes(IByteBuffer src, int? length = null);
        public void WriteBytes(IByteBuffer src, int srcIndex, int length);
        public void WriteBytes(Span<byte> src, int? length = null);
        public void WriteBytes(Span<byte> src, int srcIndex, int length);
        
        string GetString(int index, int length, Encoding encoding);
        void SetString(int index, string value, Encoding encoding);
        string ReadString(int length, Encoding encoding);
        void WriteString(string value, Encoding encoding);
    }
}
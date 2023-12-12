namespace DotNetty.Buffers
{
    /// <summary>
    /// 线程安全分配<see cref="IByteBuffer"/>
    /// </summary>
    public interface IByteBufferAllocator
    {
        IByteBuffer Buffer();
        IByteBuffer Buffer(int initialCapacity);
        IByteBuffer Buffer(int initialCapacity, int maxCapacity);

        CompositeByteBuffer CompositeBuffer();
        CompositeByteBuffer CompositeBuffer(int maxComponents);
        CompositeByteBuffer CompositeHeapBuffer();
        CompositeByteBuffer CompositeHeapBuffer(int maxComponents);

        int CalculateNewCapacity(int minNewCapacity, int maxCapacity);
    }
}
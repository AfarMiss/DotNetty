namespace DotNetty.Buffers
{
    public sealed class ByteBufferAllocator : AbstractByteBufferAllocator
    {
        public static readonly ByteBufferAllocator Default = new ByteBufferAllocator();

        protected override IByteBuffer NewBuffer(int initialCapacity, int maxCapacity)
        {
            return new HeapByteBuffer(this, initialCapacity, maxCapacity);
        }

        public override CompositeByteBuffer CompositeHeapBuffer(int maxNumComponents)
        {
            return new CompositeByteBuffer(this, maxNumComponents);
        }
    }
}
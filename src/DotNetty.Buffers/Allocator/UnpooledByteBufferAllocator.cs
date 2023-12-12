namespace DotNetty.Buffers
{
    public sealed class UnpooledByteBufferAllocator : AbstractByteBufferAllocator
    {
        public static readonly UnpooledByteBufferAllocator Default = new UnpooledByteBufferAllocator();

        protected override IByteBuffer NewBuffer(int initialCapacity, int maxCapacity) =>
            new HeapByteBuffer(this, initialCapacity, maxCapacity);

        public override CompositeByteBuffer CompositeHeapBuffer(int maxNumComponents)
        {
            return new CompositeByteBuffer(this, maxNumComponents);
        }
    }
}
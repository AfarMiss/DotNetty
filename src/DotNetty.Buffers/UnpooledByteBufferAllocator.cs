namespace DotNetty.Buffers
{
    public sealed class UnpooledByteBufferAllocator : AbstractByteBufferAllocator
    {
        public static readonly UnpooledByteBufferAllocator Default = new UnpooledByteBufferAllocator();

        protected override IByteBuffer NewHeapBuffer(int initialCapacity, int maxCapacity) =>
            new UnpooledHeapByteBuffer(this, initialCapacity, maxCapacity);

        public override CompositeByteBuffer CompositeHeapBuffer(int maxNumComponents)
        {
            var buf = new CompositeByteBuffer(this, false, maxNumComponents);
            return buf;
        }
    }
}
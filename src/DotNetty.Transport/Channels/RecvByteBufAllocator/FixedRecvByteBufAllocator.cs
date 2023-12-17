namespace DotNetty.Transport.Channels
{
    public sealed class FixedRecvByteBufAllocator : AbstractRecvByteBufAllocator
    {
        public static readonly FixedRecvByteBufAllocator Default = new FixedRecvByteBufAllocator(4 * 1024);
        private readonly IRecvByteBufAllocatorHandle handle;

        public FixedRecvByteBufAllocator(int bufferSize) => this.handle = new HandleImpl(this, bufferSize);

        public override IRecvByteBufAllocatorHandle NewHandle() => this.handle;
        
        private sealed class HandleImpl : MaxMessageHandle<FixedRecvByteBufAllocator>
        {
            private readonly int bufferSize;

            public HandleImpl(FixedRecvByteBufAllocator owner, int bufferSize) : base(owner)
            {
                this.bufferSize = bufferSize;
            }

            public override int Guess() => this.bufferSize;
        }
    }
}
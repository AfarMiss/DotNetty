using System.Diagnostics.Contracts;

namespace DotNetty.Transport.Channels
{
    public sealed class FixedRecvByteBufAllocator : DefaultMaxMessagesRecvByteBufAllocator
    {
        public static readonly FixedRecvByteBufAllocator Default = new FixedRecvByteBufAllocator(4 * 1024);
        private readonly IRecvByteBufAllocatorHandle handle;

        private sealed class HandleImpl : MaxMessageHandle<FixedRecvByteBufAllocator>
        {
            readonly int bufferSize;

            public HandleImpl(FixedRecvByteBufAllocator owner, int bufferSize)
                : base(owner)
            {
                this.bufferSize = bufferSize;
            }

            public override int Guess() => this.bufferSize;
        }

        public FixedRecvByteBufAllocator(int bufferSize)
        {
            Contract.Requires(bufferSize > 0);

            this.handle = new HandleImpl(this, bufferSize);
        }

        public override IRecvByteBufAllocatorHandle NewHandle() => this.handle;
    }
}
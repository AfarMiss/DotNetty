using System.Threading;

namespace DotNetty.Transport.Channels.Pool
{
    public sealed class CountingChannelPoolHandler : IChannelPoolHandler
    {
        private int channelCount;
        private int acquiredCount;
        private int releasedCount;
        
        public int ChannelCount => this.channelCount;

        public int AcquiredCount => this.acquiredCount;

        public int ReleasedCount => this.releasedCount;

        public void ChannelCreated(IChannel ch) => Interlocked.Increment(ref this.channelCount);

        public void ChannelReleased(IChannel ch) => Interlocked.Increment(ref this.releasedCount);

        public void ChannelAcquired(IChannel ch) => Interlocked.Increment(ref this.acquiredCount);
    }
}
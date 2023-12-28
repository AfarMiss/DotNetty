namespace DotNetty.Transport.Channels
{
    internal sealed class DefaultChannelId : IChannelId
    {
        public static DefaultChannelId NewInstance() => new DefaultChannelId();

        public int CompareTo(IChannelId other) => 0;
    }
}

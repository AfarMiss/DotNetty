namespace DotNetty.Transport.Channels.Pool
{
    public interface IChannelPoolHandler
    {
        void ChannelReleased(IChannel channel);
        void ChannelAcquired(IChannel channel);
        void ChannelCreated(IChannel channel);
    }
}
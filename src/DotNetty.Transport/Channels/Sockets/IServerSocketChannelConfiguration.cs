namespace DotNetty.Transport.Channels.Sockets
{
    public interface IServerSocketChannelConfiguration : IChannelConfiguration
    {
        int Backlog { get; set; }
    }
}
using DotNetty.Transport.Channels.Sockets;

namespace DotNetty.Transport.Channels
{
    /// <summary>
    /// A <see cref="IChannel"/> that accepts an incoming connection attempt and creates its child
    /// <see cref="IChannel"/>s by accepting them. <see cref="IServerSocketChannel"/> is a good example.
    /// </summary>
    public interface IServerChannel : IChannel
    {
    }
}
namespace DotNetty.Transport.Channels.Sockets
{
    public interface ISocketChannelConfiguration : IChannelConfiguration
    {
        bool AllowHalfClosure { get; set; }

        int ReceiveBufferSize { get; set; }

        int SendBufferSize { get; set; }

        int Linger { get; set; }

        bool KeepAlive { get; set; }

        bool ReuseAddress { get; set; }

        bool TcpNoDelay { get; set; }
    }
}
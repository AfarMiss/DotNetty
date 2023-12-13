using System.Net;
using System.Threading.Tasks;

namespace DotNetty.Transport.Channels
{
    public interface IChannelUnsafe
    {
        IRecvByteBufAllocatorHandle RecvBufAllocHandle { get; }

        Task RegisterAsync(IEventLoop eventLoop);
        Task DeregisterAsync();
        Task BindAsync(EndPoint localAddress);
        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);
        Task DisconnectAsync();
        Task CloseAsync();
        void CloseForcibly();
        void BeginRead();
        Task WriteAsync(object message);
        void Flush();

        ChannelOutboundBuffer OutboundBuffer { get; }
    }
}
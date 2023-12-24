using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public interface IChannel : IAttributeMap, IConstantTransfer, IComparable<IChannel>
    {
        IChannelId Id { get; }
        IEventLoop EventLoop { get; }
        IChannel Parent { get; }
        bool Open { get; }
        bool Active { get; }
        bool Registered { get; }

        ChannelMetadata Metadata { get; }
        EndPoint LocalAddress { get; }
        EndPoint RemoteAddress { get; }
        bool IsWritable { get; }
        IChannelUnsafe Unsafe { get; }
        IChannelPipeline Pipeline { get; }
        IChannelConfiguration Configuration { get; }
        Task CloseCompletion { get; }

        Task DeregisterAsync();
        Task BindAsync(EndPoint localAddress);
        Task ConnectAsync(EndPoint remoteAddress);
        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);
        Task DisconnectAsync();
        Task CloseAsync();
        // todo: make these available through separate interface to hide them from public API on channel
        void Read();
        Task WriteAsync(object message);
        void Flush();
        Task WriteAndFlushAsync(object message);
    }
}
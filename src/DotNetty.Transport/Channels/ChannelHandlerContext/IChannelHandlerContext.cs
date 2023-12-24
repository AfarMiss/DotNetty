using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    /// <summary>
    /// ChannelHandler上下文
    /// ChannelHandler添加到ChannelPipeline时候分配一个上下文与之绑定 上下文可以与自身处理器或其他的处理器进行交互
    /// </summary>
    public interface IChannelHandlerContext : IAttributeMap
    {
        IChannel Channel { get; }
        IByteBufferAllocator Allocator { get; }
        IEventExecutor Executor { get; }

        string Name { get; }

        IChannelHandler Handler { get; }

        bool Removed { get; }

        void FireChannelRegistered();

        void FireChannelUnregistered();

        void FireChannelActive();

        void FireChannelInactive();

        void FireChannelRead(object message);

        void FireChannelReadComplete();

        void FireChannelWritabilityChanged();

        void FireExceptionCaught(Exception ex);

        void FireUserEventTriggered(object evt);

        void Read();

        Task WriteAsync(object message); // todo: optimize: add flag saying if handler is interested in task, do not produce task if it isn't needed

        void Flush();

        Task WriteAndFlushAsync(object message);

        Task BindAsync(EndPoint localAddress);

        Task ConnectAsync(EndPoint remoteAddress);

        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

        Task DisconnectAsync();

        Task CloseAsync();

        Task DeregisterAsync();
    }
}
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
        /// <summary>
        /// Returns the <see cref="IEventExecutor"/> which is used to execute an arbitrary task.
        /// </summary>
        IEventExecutor Executor { get; }

        /// <summary>
        /// The unique name of the <see cref="IChannelHandlerContext"/>.
        /// </summary>
        /// <remarks>
        /// The name was used when the <see cref="IChannelHandler"/> was added to the <see cref="IChannelPipeline"/>.
        /// This name can also be used to access the registered <see cref="IChannelHandler"/> from the
        /// <see cref="IChannelPipeline"/>.
        /// </remarks>
        string Name { get; }

        IChannelHandler Handler { get; }

        bool Removed { get; }

        /// <summary>
        /// A <see cref="IChannel"/> was registered to its <see cref="IEventLoop"/>. This will result in having the
        /// <see cref="IChannelHandler.ChannelRegistered"/> method called of the next <see cref="IChannelHandler"/>
        /// contained in the <see cref="IChannelPipeline"/> of the <see cref="IChannel"/>.
        /// </summary>
        /// <returns>The current <see cref="IChannelHandlerContext"/>.</returns>
        void FireChannelRegistered();

        /// <summary>
        /// A <see cref="IChannel"/> was unregistered from its <see cref="IEventLoop"/>. This will result in having the
        /// <see cref="IChannelHandler.ChannelUnregistered"/> method called of the next <see cref="IChannelHandler"/>
        /// contained in the <see cref="IChannelPipeline"/> of the <see cref="IChannel"/>.
        /// </summary>
        /// <returns>The current <see cref="IChannelHandlerContext"/>.</returns>
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

        /// <summary>
        /// Request to bind to the given <see cref="EndPoint"/>.
        /// <para>
        /// This will result in having the <see cref="IChannelHandler.BindAsync"/> method called of the next
        /// <see cref="IChannelHandler"/> contained in the <see cref="IChannelPipeline"/> of the
        /// <see cref="IChannel"/>.
        /// </para>
        /// </summary>
        /// <param name="localAddress">The <see cref="EndPoint"/> to bind to.</param>
        /// <returns>An await-able task.</returns>
        Task BindAsync(EndPoint localAddress);

        /// <summary>
        /// Request to connect to the given <see cref="EndPoint"/>.
        /// <para>
        /// This will result in having the <see cref="IChannelHandler.ConnectAsync"/> method called of the next
        /// <see cref="IChannelHandler"/> contained in the <see cref="IChannelPipeline"/> of the
        /// <see cref="IChannel"/>.
        /// </para>
        /// </summary>
        /// <param name="remoteAddress">The <see cref="EndPoint"/> to connect to.</param>
        /// <returns>An await-able task.</returns>
        Task ConnectAsync(EndPoint remoteAddress);

        /// <summary>
        /// Request to connect to the given <see cref="EndPoint"/> while also binding to the localAddress.
        /// <para>
        /// This will result in having the <see cref="IChannelHandler.ConnectAsync"/> method called of the next
        /// <see cref="IChannelHandler"/> contained in the <see cref="IChannelPipeline"/> of the
        /// <see cref="IChannel"/>.
        /// </para>
        /// </summary>
        /// <param name="remoteAddress">The <see cref="EndPoint"/> to connect to.</param>
        /// <param name="localAddress">The <see cref="EndPoint"/> to bind to.</param>
        /// <returns>An await-able task.</returns>
        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

        /// <summary>
        /// Request to disconnect from the remote peer.
        /// <para>
        /// This will result in having the <see cref="IChannelHandler.DisconnectAsync"/> method called of the next
        /// <see cref="IChannelHandler"/> contained in the <see cref="IChannelPipeline"/> of the
        /// <see cref="IChannel"/>.
        /// </para>
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task DisconnectAsync();

        Task CloseAsync();

        /// <summary>
        /// Request to deregister from the previous assigned <see cref="IEventExecutor"/>.
        /// <para>
        /// This will result in having the <see cref="IChannelHandler.DeregisterAsync"/> method called of the next
        /// <see cref="IChannelHandler"/> contained in the <see cref="IChannelPipeline"/> of the
        /// <see cref="IChannel"/>.
        /// </para>
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task DeregisterAsync();
    }
}
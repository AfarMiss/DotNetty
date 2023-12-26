using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace DotNetty.Transport.Channels
{
    /// <para>
    ///     <pre>
    ///         +---------------------------------------------------+---------------+
    ///         |                           ChannelPipeline         |               |
    ///         |                                                  \|/              |
    ///         |    +----------------------------------------------+----------+    |
    ///         |    |                   ChannelHandler  N                     |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  |               |
    ///         |               |                                  \|/              |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |    |                   ChannelHandler N-1                    |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  .               |
    ///         |               .                                   .               |
    ///         | ChannelHandlerContext.fireIN_EVT() ChannelHandlerContext.OUT_EVT()|
    ///         |          [method call]                      [method call]         |
    ///         |               .                                   .               |
    ///         |               .                                  \|/              |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |    |                   ChannelHandler  2                     |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  |               |
    ///         |               |                                  \|/              |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |    |                   ChannelHandler  1                     |    |
    ///         |    +----------+-----------------------------------+----------+    |
    ///         |              /|\                                  |               |
    ///         +---------------+-----------------------------------+---------------+
    ///         |                                  \|/
    ///         +---------------+-----------------------------------+---------------+
    ///         |               |                                   |               |
    ///         |       [ Socket.read() ]                    [ Socket.write() ]     |
    ///         |                                                                   |
    ///         |  Netty Internal I/O Threads (Transport Implementation)            |
    ///         +-------------------------------------------------------------------+
    ///     </pre>
    /// </para>

    /// <summary>
    /// <para>
    ///     <ul>
    ///         <li>
    ///             Inbound event propagation methods:
    ///             <ul>
    ///                 <li><see cref="IChannelHandlerContext.FireChannelRegistered"/></li>
    ///                 <li><see cref="IChannelHandlerContext.FireChannelActive"/></li>
    ///                 <li><see cref="IChannelHandlerContext.FireChannelRead"/></li>
    ///                 <li><see cref="IChannelHandlerContext.FireChannelReadComplete"/></li>
    ///                 <li><see cref="IChannelHandlerContext.FireExceptionCaught"/></li>
    ///                 <li><see cref="IChannelHandlerContext.FireUserEventTriggered"/></li>
    ///                 <li><see cref="IChannelHandlerContext.FireChannelWritabilityChanged"/></li>
    ///                 <li><see cref="IChannelHandlerContext.FireChannelInactive"/></li>
    ///             </ul>
    ///         </li>
    ///         <li>
    ///             Outbound event propagation methods:
    ///             <ul>
    ///                 <li><see cref="IChannelHandlerContext.BindAsync"/></li>
    ///                 <li><see cref="IChannelHandlerContext.ConnectAsync(EndPoint)"/></li>
    ///                 <li><see cref="IChannelHandlerContext.ConnectAsync(EndPoint, EndPoint)"/></li>
    ///                 <li><see cref="IChannelHandlerContext.WriteAsync"/></li>
    ///                 <li><see cref="IChannelHandlerContext.Flush"/></li>
    ///                 <li><see cref="IChannelHandlerContext.Read"/></li>
    ///                 <li><see cref="IChannelHandlerContext.DisconnectAsync"/></li>
    ///                 <li><see cref="IChannelHandlerContext.CloseAsync"/></li>
    ///             </ul>
    ///         </li>
    ///     </ul>
    /// </para>
    /// </summary>
    public interface IChannelPipeline : IChannelPipelineCollection, IEnumerable<IChannelHandler>
    {
        IChannel Channel { get; }

        /// <summary>
        /// <see cref="IChannelHandler.ChannelRegistered"/>
        /// </summary>
        void FireChannelRegistered();

        /// <summary>
        /// <see cref="IChannelHandler.ChannelUnregistered"/>
        /// </summary>
        void FireChannelUnregistered();

        /// <summary>
        /// <see cref="IChannelHandler.ChannelActive"/>
        /// </summary>
        void FireChannelActive();

        /// <summary>
        /// <see cref="IChannelHandler.ChannelInactive"/>
        /// </summary>
        void FireChannelInactive();

        /// <summary>
        /// <see cref="IChannelHandler.ExceptionCaught"/>
        /// </summary>
        void FireExceptionCaught(Exception cause);

        /// <summary>
        /// <see cref="IChannelHandler.UserEventTriggered"/>
        /// </summary>
        void FireUserEventTriggered(object evt);

        /// <summary>
        /// <see cref="IChannelHandler.ChannelRead"/>
        /// </summary>
        void FireChannelRead(object msg);

        /// <summary>
        /// <see cref="IChannelHandler.ChannelReadComplete"/>
        /// </summary>
        void FireChannelReadComplete();

        /// <summary>
        /// <see cref="IChannelHandler.ChannelWritabilityChanged"/>
        /// </summary>
        void FireChannelWritabilityChanged();

        /// <summary>
        /// <see cref="IChannelHandler.BindAsync"/>
        /// </summary>
        Task BindAsync(EndPoint localAddress);

        /// <summary>
        /// <see cref="IChannelHandler.ConnectAsync"/>
        /// </summary>
        Task ConnectAsync(EndPoint remoteAddress);

        /// <summary>
        /// <see cref="IChannelHandler.ConnectAsync"/>
        /// </summary>
        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

        /// <summary>
        /// <see cref="IChannelHandler.DisconnectAsync"/>
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// <see cref="IChannelHandler.CloseAsync"/>
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// <see cref="IChannelHandler.DeregisterAsync"/>
        /// </summary>
        Task DeregisterAsync();

        /// <summary>
        /// <see cref="IChannelHandler.ChannelRead"/>
        /// <see cref="IChannelHandler.ChannelReadComplete"/>
        /// <see cref="IChannelHandler.Read"/>
        /// </summary>
        void Read();

        /// <summary>
        /// 写入消息 此方法不会Flush数据,如需要将消息Flush调用<see cref="Flush"/>
        /// </summary>
        Task WriteAsync(object msg);

        /// <summary>
        /// Request to flush all pending messages.
        /// </summary>
        /// <returns>This <see cref="IChannelPipeline"/>.</returns>
        void Flush();

        /// <summary>
        /// <see cref="WriteAsync"/>和<see cref="Flush"/>.
        /// </summary>
        Task WriteAndFlushAsync(object msg);
    }
}
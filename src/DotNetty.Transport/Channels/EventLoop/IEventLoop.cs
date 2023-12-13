using DotNetty.Common.Concurrency;

namespace DotNetty.Transport.Channels
{
    /// <summary>
    /// <see cref="IEventExecutor"/> 专门处理分配的I/O操作 <see cref="IChannel"/>s.
    /// </summary>
    public interface IEventLoop : IEventLoopGroup, IEventExecutor
    {
        new IEventLoopGroup Parent { get; }
    }
}
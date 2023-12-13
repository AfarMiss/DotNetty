using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;

namespace DotNetty.Transport.Channels
{
    /// <inheritdoc />
    /// <summary>
    /// <see cref="IEventExecutorGroup" /> specialized for handling <see cref="IEventLoop" />s.
    /// </summary>
    public interface IEventLoopGroup : IEventExecutorGroup
    {
        /// <inheritdoc cref="IEventExecutorGroup.Items"/>
        new IEnumerable<IEventLoop> Items { get; }

        /// <summary> 获取持有的IEventLoop </summary>
        new IEventLoop GetNext();

        /// <summary> 注册<see cref="IChannel"/>  </summary>
        Task RegisterAsync(IChannel channel);
    }
}
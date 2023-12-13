using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetty.Common.Concurrency
{
    /// <summary>
    /// 管理和调度EventExecutor组
    /// </summary>
    public interface IEventExecutorGroup : IScheduledExecutorService
    {
        /// <summary>
        /// Returns list of owned event executors.
        /// </summary>
        IEnumerable<IEventExecutor> Items { get; }

        /// <summary> 仅当通过调用<see cref="ShutdownGracefullyAsync()" />关闭时返回true </summary>
        bool IsShuttingDown { get; }
        /// <summary> 终止回调 </summary>
        Task TerminationCompletion { get; }
        
        /// <summary> 终止当前<see cref="IEventExecutorGroup"/>及其所有<see cref="IEventExecutor"/> </summary>
        Task ShutdownGracefullyAsync();
        /// <inheritdoc cref="ShutdownGracefullyAsync()"/>
        Task ShutdownGracefullyAsync(TimeSpan quietPeriod, TimeSpan timeout);

        IEventExecutor GetNext();
    }
}
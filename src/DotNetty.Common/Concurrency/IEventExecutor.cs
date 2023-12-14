using System.Threading;

namespace DotNetty.Common.Concurrency
{
    /// 任务执行器
    public interface IEventExecutor : IEventExecutorGroup
    {
        /// <see cref="IEventExecutorGroup"/>
        IEventExecutorGroup Parent { get; }
        /// 当前<see cref="Thread"/>是否属于此事件循环
        bool InEventLoop { get; }

        /// <see cref="InEventLoop"/>
        bool IsInEventLoop(Thread thread);
    }
}
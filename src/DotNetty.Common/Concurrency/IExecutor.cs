using System;

namespace DotNetty.Common.Concurrency
{
    /// <summary>
    /// 执行线程任务 线程细节由IEventExecutor实现决定
    /// </summary>
    public interface IExecutor
    {
        /// <summary> 执行线程任务</summary>
        void Execute(IRunnable task);
        void Execute(Action<object> action, object state);
        void Execute(Action action);
        void Execute(Action<object, object> action, object context, object state);
    }
}
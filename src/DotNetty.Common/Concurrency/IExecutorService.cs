using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetty.Common.Concurrency
{
    /// <summary>
    /// 异步线程任务 线程细节由<see cref="IEventExecutor"/>实现决定
    /// </summary>
    public interface IExecutorService : IExecutor
    {
        /// <summary> 是否关闭 </summary>
        bool IsShutdown { get; }
        /// <summary> 如果关闭后所有任务都已完成且调用<see cref="IEventExecutorGroup.ShutdownGracefullyAsync()"/>则返回true </summary>
        bool IsTerminated { get; }

        /// <summary> 封装执行函数为Task </summary>
        Task<T> SubmitAsync<T>(Func<T> func);
        Task<T> SubmitAsync<T>(Func<T> func, CancellationToken cancellationToken);
        Task<T> SubmitAsync<T>(Func<object, T> func, object state);
        Task<T> SubmitAsync<T>(Func<object, T> func, object state, CancellationToken cancellationToken);
        Task<T> SubmitAsync<T>(Func<object, object, T> func, object context, object state);
        Task<T> SubmitAsync<T>(Func<object, object, T> func, object context, object state, CancellationToken cancellationToken);
    }
}
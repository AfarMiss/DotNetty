using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetty.Common.Concurrency
{
    /// <summary>
    /// 计划调度异步线程任务 线程细节由IEventExecutor实现决定
    /// </summary>
    public interface IScheduledExecutorService : IExecutorService
    {
        /// <summary> 计划任务 在给定延迟后执 带参则可用作重复执行 </summary>
        IScheduledTask Schedule(IRunnable action, TimeSpan delay);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        IScheduledTask Schedule(Action action, TimeSpan delay);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        IScheduledTask Schedule(Action<object> action, object state, TimeSpan delay);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        IScheduledTask Schedule(Action<object, object> action, object context, object state, TimeSpan delay);

        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        Task ScheduleAsync(Action action, TimeSpan delay);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        Task ScheduleAsync(Action action, TimeSpan delay, CancellationToken cancellationToken);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        Task ScheduleAsync(Action<object> action, object state, TimeSpan delay);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        Task ScheduleAsync(Action<object> action, object state, TimeSpan delay, CancellationToken cancellationToken);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        Task ScheduleAsync(Action<object, object> action, object context, object state, TimeSpan delay);
        /// <inheritdoc cref="Schedule(IRunnable, TimeSpan)"/>
        Task ScheduleAsync(Action<object, object> action, object context, object state, TimeSpan delay, CancellationToken cancellationToken);
    }
}
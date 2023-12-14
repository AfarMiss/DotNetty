using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common.Internal.Logging;

namespace DotNetty.Common.Concurrency
{
    public abstract class AbstractEventExecutor : AbstractExecutorService, IEventExecutor
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AbstractEventExecutor>();

        private static readonly TimeSpan DefaultShutdownQuietPeriod = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(15);

        public abstract bool IsShuttingDown { get; }
        public abstract Task TerminationCompletion { get; }
        public IEnumerable<IEventExecutor> Items => this.GetItems();

        public IEventExecutorGroup Parent { get; }
        public bool InEventLoop => this.IsInEventLoop(Thread.CurrentThread);
        
        protected abstract IEnumerable<IEventExecutor> GetItems();
        public abstract bool IsInEventLoop(Thread thread);
        
        protected AbstractEventExecutor(IEventExecutorGroup parent = null) => this.Parent = parent;
        
        public IEventExecutor GetNext() => this;

        public virtual IScheduledTask Schedule(IRunnable action, TimeSpan delay)
        {
            throw new NotSupportedException();
        }

        public virtual IScheduledTask Schedule(Action action, TimeSpan delay)
        {
            throw new NotSupportedException();
        }

        public virtual IScheduledTask Schedule(Action<object> action, object state, TimeSpan delay)
        {
            throw new NotSupportedException();
        }

        public virtual IScheduledTask Schedule(Action<object, object> action, object context, object state, TimeSpan delay)
        {
            throw new NotSupportedException();
        }

        public virtual Task ScheduleAsync(Action action, TimeSpan delay)
        {
            return this.ScheduleAsync(action, delay, CancellationToken.None);
        }

        public virtual Task ScheduleAsync(Action<object> action, object state, TimeSpan delay, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public virtual Task ScheduleAsync(Action<object> action, object state, TimeSpan delay)
        {
            return this.ScheduleAsync(action, state, delay, CancellationToken.None);
        }

        public virtual Task ScheduleAsync(Action action, TimeSpan delay, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public virtual Task ScheduleAsync(Action<object, object> action, object context, object state, TimeSpan delay)
        {
            return this.ScheduleAsync(action, context, state, delay, CancellationToken.None);
        }

        public virtual Task ScheduleAsync(Action<object, object> action, object context, object state, TimeSpan delay, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ShutdownGracefullyAsync() => this.ShutdownGracefullyAsync(DefaultShutdownQuietPeriod, DefaultShutdownTimeout);

        public abstract Task ShutdownGracefullyAsync(TimeSpan quietPeriod, TimeSpan timeout);

        protected static void SetCurrentExecutor(IEventExecutor executor) => ExecutionEnvironment.SetCurrentExecutor(executor);

        protected static void SafeExecute(IRunnable task)
        {
            try
            {
                task.Run();
            }
            catch (Exception ex)
            {
                Logger.Warn("A task raised an exception. Task: {}", task, ex);
            }
        }
    }
}
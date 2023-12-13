using System;
using System.Threading;

namespace DotNetty.Common.Concurrency
{
    internal sealed class StateActionScheduledAsyncTask : ScheduledAsyncTask
    {
        private readonly Action<object> action;

        public StateActionScheduledAsyncTask(AbstractScheduledEventExecutor executor, Action<object> action, object state, PreciseTimeSpan deadline,
            CancellationToken cancellationToken)
            : base(executor, deadline, new TaskCompletionSource(state), cancellationToken)
        {
            this.action = action;
        }

        protected override void Execute() => this.action(this.Completion.AsyncState);
    }
}
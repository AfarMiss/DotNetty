using System;
using System.Threading;

namespace DotNetty.Common.Concurrency
{
    sealed class StateActionWithContextScheduledAsyncTask : ScheduledAsyncTask
    {
        private readonly Action<object, object> action;
        private readonly object context;

        public StateActionWithContextScheduledAsyncTask(AbstractScheduledEventExecutor executor, Action<object, object> action, object context, object state,
            PreciseTimeSpan deadline, CancellationToken cancellationToken)
            : base(executor, deadline, new TaskCompletionSource(state), cancellationToken)
        {
            this.action = action;
            this.context = context;
        }

        protected override void Execute() => this.action(this.context, this.Completion.AsyncState);
    }
}
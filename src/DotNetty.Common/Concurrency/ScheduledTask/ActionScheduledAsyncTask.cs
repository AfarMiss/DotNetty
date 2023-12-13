using System;
using System.Threading;

namespace DotNetty.Common.Concurrency
{
    internal sealed class ActionScheduledAsyncTask : ScheduledAsyncTask
    {
        private readonly Action action;

        public ActionScheduledAsyncTask(AbstractScheduledEventExecutor executor, Action action, PreciseTimeSpan deadline, CancellationToken cancellationToken)
            : base(executor, deadline, new TaskCompletionSource(), cancellationToken)
        {
            this.action = action;
        }

        protected override void Execute() => this.action();
    }
}
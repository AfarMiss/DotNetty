using System;

namespace DotNetty.Common.Concurrency
{
    internal sealed class ActionScheduledTask : ScheduledTask
    {
        private readonly Action action;

        public ActionScheduledTask(AbstractScheduledEventExecutor executor, Action action, PreciseTimeSpan deadline)
            : base(executor, deadline, new TaskCompletionSource())
        {
            this.action = action;
        }

        protected override void Execute() => this.action();
    }
}
using System;

namespace DotNetty.Common.Concurrency
{
    internal sealed class StateActionWithContextScheduledTask : ScheduledTask
    {
        private readonly Action<object, object> action;
        private readonly object context;

        public StateActionWithContextScheduledTask(AbstractScheduledEventExecutor executor, Action<object, object> action, object context, object state,
            PreciseTimeSpan deadline)
            : base(executor, deadline, new TaskCompletionSource(state))
        {
            this.action = action;
            this.context = context;
        }

        protected override void Execute() => this.action(this.context, this.Completion.AsyncState);
    }
}
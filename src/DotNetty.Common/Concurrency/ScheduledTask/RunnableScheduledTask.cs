namespace DotNetty.Common.Concurrency
{
    internal sealed class RunnableScheduledTask : ScheduledTask
    {
        private readonly IRunnable action;

        public RunnableScheduledTask(AbstractScheduledEventExecutor executor, IRunnable action, PreciseTimeSpan deadline)
            : base(executor, deadline, new TaskCompletionSource())
        {
            this.action = action;
        }

        protected override void Execute() => this.action.Run();
    }
}
using System.Threading;

namespace DotNetty.Common.Concurrency
{

    abstract class ScheduledAsyncTask : ScheduledTask
    {
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenRegistration cancellationTokenRegistration;

        protected ScheduledAsyncTask(AbstractScheduledEventExecutor executor, PreciseTimeSpan deadline, TaskCompletionSource promise, CancellationToken cancellationToken)
            : base(executor, deadline, promise)
        {
            this.cancellationToken = cancellationToken;
            this.cancellationTokenRegistration = cancellationToken.Register(s => ((ScheduledAsyncTask)s).Cancel(), this);
        }

        public override void Run()
        {
            this.cancellationTokenRegistration.Dispose();
            if (this.cancellationToken.IsCancellationRequested)
            {
                this.Promise.TrySetCanceled();
            }
            else
            {
                base.Run();
            }
        }
    }
}
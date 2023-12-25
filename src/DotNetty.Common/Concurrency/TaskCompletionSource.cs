using System.Threading.Tasks;

namespace DotNetty.Common.Concurrency
{
    public sealed class TaskCompletionSource : TaskCompletionSource<TaskCompletionSource.VoidResult>
    {
        public readonly struct VoidResult { }

        /// <summary>
        /// Completed
        /// </summary>
        public static readonly TaskCompletionSource Void = CreateVoidTcs();

        public TaskCompletionSource(object state) : base(state)
        {
        }

        public TaskCompletionSource()
        {
        }

        public bool TryComplete() => this.TrySetResult(default);

        public void Complete() => this.SetResult(default);

        // todo: support cancellation token where used
        public bool SetUncancellable() => true;

        private static TaskCompletionSource CreateVoidTcs()
        {
            var tcs = new TaskCompletionSource();
            tcs.TryComplete();
            return tcs;
        }
    }
}
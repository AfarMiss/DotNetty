using System.Threading.Tasks;

namespace DotNetty.Common.Concurrency
{
    public sealed class TaskCompletionSource : TaskCompletionSource<int>
    {
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

        public bool TryComplete() => this.TrySetResult(0);

        public void Complete() => this.SetResult(0);

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
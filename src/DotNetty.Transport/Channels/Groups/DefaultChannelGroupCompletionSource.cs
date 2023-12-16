using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels.Groups
{
    public class DefaultChannelGroupCompletionSource : TaskCompletionSource<int>, IEnumerator<Task>
    {
        private readonly List<Task> futures;
        public IChannelGroup Group { get; }

        public DefaultChannelGroupCompletionSource(IChannelGroup group, List<Task> futures, object state = null) : base(state)
        {
            Contract.Requires(group != null);
            Contract.Requires(futures != null);

            this.Group = group;
            this.futures = futures;
            System.Threading.Tasks.Task.WhenAll(futures).LinkOutcome(this);
        }

        public bool IsSuccess() => this.Task.IsCompleted && !this.Task.IsFaulted && !this.Task.IsCanceled;

        public Task Current => this.futures.GetEnumerator().Current;

        public void Dispose() => this.futures.GetEnumerator().Dispose();

        object IEnumerator.Current => this.futures.GetEnumerator().Current;

        public bool MoveNext() => this.futures.GetEnumerator().MoveNext();

        public void Reset() => ((IEnumerator)this.futures.GetEnumerator()).Reset();
    }
}
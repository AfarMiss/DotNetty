using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace DotNetty.Transport.Channels.Groups
{
    public sealed class CombinedEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> e1;
        private readonly IEnumerator<T> e2;
        private IEnumerator<T> currentEnumerator;

        public CombinedEnumerator(IEnumerator<T> e1, IEnumerator<T> e2)
        {
            Contract.Requires(e1 != null);
            Contract.Requires(e2 != null);
            this.e1 = e1;
            this.e2 = e2;
            this.currentEnumerator = e1;
        }

        public T Current => this.currentEnumerator.Current;

        public void Dispose() => this.currentEnumerator.Dispose();

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            for (;;)
            {
                if (this.currentEnumerator.MoveNext())
                {
                    return true;
                }
                if (this.currentEnumerator == this.e1)
                {
                    this.currentEnumerator = this.e2;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Reset() => this.currentEnumerator.Reset();
    }
}
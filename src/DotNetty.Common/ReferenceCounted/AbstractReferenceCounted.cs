using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DotNetty.Common.Utilities
{
    public abstract class AbstractReferenceCounted : IReferenceCounted
    {
        private int referenceCount = 1;

        public int ReferenceCount => this.referenceCount;

        public IReferenceCounted Retain(int increment = 1)
        {
            Contract.Requires(increment > 0);

            return this.RetainCore(increment);
        }

        protected virtual IReferenceCounted RetainCore(int increment)
        {
            while (true)
            {
                int count = this.referenceCount;
                int nextCount = count + increment;

                // Ensure we not resurrect (which means the refCnt was 0) and also that we encountered an overflow.
                if (nextCount <= increment)
                {
                    ThrowIllegalReferenceCountException(count, increment);
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, nextCount, count) == count)
                {
                    break;
                }
            }

            return this;
        }

        public bool Release(int decrement = 1)
        {
            Contract.Requires(decrement > 0);

            return this.ReleaseCore(decrement);
        }

        private bool ReleaseCore(int decrement)
        {
            while (true)
            {
                int count = this.referenceCount;
                if (count < decrement)
                {
                    ThrowIllegalReferenceCountException(count, decrement);
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, count - decrement, count) == decrement)
                {
                    this.Deallocate();
                    return  true;
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowIllegalReferenceCountException(int count, int increment)
        {
            throw GetIllegalReferenceCountException();

            IllegalReferenceCountException GetIllegalReferenceCountException()
            {
                return new IllegalReferenceCountException(count, increment);
            }
        }

        protected abstract void Deallocate();
    }
}

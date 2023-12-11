using System.Diagnostics.Contracts;
using System.Threading;
using DotNetty.Common;
using DotNetty.Common.Utilities;

namespace DotNetty.Buffers
{
    public abstract class AbstractRefByteBuffer : AbstractByteBuffer
    {
        private volatile int referenceCount = 1;
        public override int ReferenceCount => this.referenceCount;

        protected internal abstract void Deallocate();

        protected AbstractRefByteBuffer(int maxCapacity) : base(maxCapacity)
        {
        }

        public override IReferenceCounted Retain() => this.Retain0(1);

        public override IReferenceCounted Retain(int increment)
        {
            Contract.Requires(increment > 0);

            return this.Retain0(increment);
        }

        private IReferenceCounted Retain0(int increment)
        {
            while (true)
            {
                int refCnt = this.referenceCount;
                int nextCnt = refCnt + increment;

                // Ensure we not resurrect (which means the refCnt was 0) and also that we encountered an overflow.
                if (nextCnt <= increment)
                {
                    throw new IllegalReferenceCountException(refCnt, increment);
                }
                if (Interlocked.CompareExchange(ref this.referenceCount, refCnt + increment, refCnt) == refCnt)
                {
                    break;
                }
            }

            return this;
        }

        public override bool Release() => this.Release0(1);

        public override bool Release(int decrement)
        {
            Contract.Requires(decrement > 0);

            return this.Release0(decrement);
        }

        bool Release0(int decrement)
        {
            while (true)
            {
                int refCnt = this.ReferenceCount;
                if (refCnt < decrement)
                {
                    throw new IllegalReferenceCountException(refCnt, -decrement);
                }

                if (Interlocked.CompareExchange(ref this.referenceCount, refCnt - decrement, refCnt) == refCnt)
                {
                    if (refCnt == decrement)
                    {
                        this.Deallocate();
                        return true;
                    }

                    return false;
                }
            }
        }

    }
}
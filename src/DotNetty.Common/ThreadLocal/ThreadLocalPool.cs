using System;

namespace DotNetty.Common
{
    public class ThreadLocalPool<T> where T : IRecycle, new()
    {
        private readonly FastThreadLocalPool threadLocal;
        private readonly Func<T> valueFactory;
        private readonly int interval;
        private int intervalCounter;
        
        public ThreadLocalPool(Func<T> valueFactory = null, int maxCapacity = 1024, int interval = 8, int chunkSize = 32)
        {
            valueFactory ??= () => new T();
            this.valueFactory = valueFactory;
            this.threadLocal = new FastThreadLocalPool(maxCapacity, chunkSize);
            this.interval = interval;
            this.intervalCounter = interval;
        }

        private sealed class FastThreadLocalPool : FastThreadLocal<ThreadLocalQueue<T>>
        {
            private readonly int chunkSize;
            private readonly int maxCapacity;

            public FastThreadLocalPool(int maxCapacity, int chunkSize)
            {
                this.maxCapacity = maxCapacity;
                this.chunkSize = chunkSize;
            }

            protected override ThreadLocalQueue<T> GetInitialValue() => new ThreadLocalQueue<T>(this.chunkSize, this.maxCapacity);

            protected override void OnRemove(ThreadLocalQueue<T> value)
            {
                var handles = value.PooledHandles;
                value.PooledHandles = null;
                value.Owner = null;
                handles.Clear();
            }
        }
        
        public T Acquire(out IRecycleHandle<T> handle)
        {
            var localPool = this.threadLocal.Value;
            handle = localPool.Dequeue();
            if (handle == null)
            {
                if (++this.intervalCounter >= this.interval)
                {
                    this.intervalCounter = 0;
                    handle = new RecycleHandle<T>(localPool);
                }
                else
                {
                    handle = new NormalHandle<T>();
                }
                var obj = this.valueFactory();
                handle.SetValue(obj);
                return obj;
            }

            return ((RecycleHandle<T>)handle).GetValue();
        }
        
        public void Recycle(IRecycleHandle<T> handle) => handle.Recycle();
    }
}
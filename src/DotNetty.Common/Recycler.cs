using System;

namespace DotNetty.Common
{
    public class RecyclerThreadLocalPool<T> where T : IRecycle
    {
        private readonly FastThreadLocalPool threadLocal;
        private readonly Func<T> valueFactory;

        public RecyclerThreadLocalPool(Func<T> valueFactory, int capacity = 0)
        {
            this.valueFactory = valueFactory;
            this.threadLocal = new FastThreadLocalPool(capacity);
        }

        private sealed class FastThreadLocalPool : FastThreadLocal<ThreadLocalQueue<T>>
        {
            private readonly int chunkSize;
            
            public FastThreadLocalPool(int capacity) => this.chunkSize = capacity;
            
            protected override ThreadLocalQueue<T> GetInitialValue() => new ThreadLocalQueue<T>(this.chunkSize);

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
            T obj;
            var localPool = threadLocal.Value;
            handle = localPool.Dequeue();
            if (handle == null)
            {
                handle = new RecycleHandle<T>(localPool);
                obj = this.valueFactory();
                ((RecycleHandle<T>)handle).SetValue(obj);
            }
            else
            {
                obj = ((RecycleHandle<T>)handle).GetValue();
            }

            return obj;
        }
        
        public void Recycle(IRecycleHandle<T> handle) => handle.Recycle();
    }
}
namespace DotNetty.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    public interface IRecycle
    {
        protected internal void Recycle();
    }

    public interface IRecycleHandle<T> where T : IRecycle
    {
        protected internal void Recycle();
    }

    internal sealed class RecycleHandle<T> : IRecycleHandle<T> where T : IRecycle
    {
        private const int STATE_CLAIMED = 0;
        private const int STATE_AVAILABLE = 1;
        private int STATE_UPDATER = STATE_CLAIMED;

        private readonly ThreadLocalQueue<T> queue;
        private IRecycle value;

        public RecycleHandle(ThreadLocalQueue<T> queue) => this.queue = queue;

        public void SetValue(T value) => this.value = value;
        public T GetValue() => (T)this.value;

        public void ToClaimed()
        {
            Interlocked.Exchange(ref STATE_UPDATER, STATE_CLAIMED);
        }

        public void ToAvailable()
        {
            if (Interlocked.Exchange(ref STATE_UPDATER, STATE_AVAILABLE) == STATE_AVAILABLE)
            {
                throw new Exception("重复回收");
            }
        }

        void IRecycleHandle<T>.Recycle()
        {
            this.value.Recycle();
            this.queue.Enqueue(this);
        }
    }

    internal class ThreadLocalQueue<T> where T : IRecycle
    {
        private readonly int chunkSize;
        internal volatile Thread Owner;
        private readonly Queue<RecycleHandle<T>> batch;
        internal volatile ConcurrentQueue<RecycleHandle<T>> PooledHandles;

        internal ThreadLocalQueue(int chunkSize) 
        {
            this.chunkSize = chunkSize;
            this.batch = new Queue<RecycleHandle<T>>(chunkSize);
            this.Owner = Thread.CurrentThread;
            this.PooledHandles = new ConcurrentQueue<RecycleHandle<T>>();
        }
        
        /// <summary>
        /// 线程安全
        /// </summary>
        public RecycleHandle<T> Dequeue() 
        {
            if (this.batch.Count <= 0)
            {
                var size = Math.Min(this.chunkSize, this.PooledHandles.Count);
                for (var i = 0; i < size; i++)
                {
                    if (this.PooledHandles.TryDequeue(out var swapHandle))
                    {
                        this.batch.Enqueue(swapHandle);
                    }
                }
            }
            
            if (this.batch.TryDequeue(out var handle))
            {
                handle.ToClaimed();
            }

            return handle;
        }

        public void Enqueue(RecycleHandle<T> handle) 
        {
            handle.ToAvailable();
            if (this.Owner != null && Thread.CurrentThread == this.Owner && this.batch.Count < this.chunkSize) 
            {
                this.batch.Enqueue(handle);
            } 
            else if (this.Owner != null && !this.Owner.IsAlive)
            {
                this.Owner = null;
                this.PooledHandles = null;
            }
            else
            {
                this.PooledHandles.Enqueue(handle);
            }
        }
    }
}
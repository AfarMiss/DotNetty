namespace DotNetty.Common
{
    using System.Collections.Generic;

    public class PoolList : List<object>, IRecycle
    {
        internal IRecycleHandle<PoolList> Handle;
        void IRecycle.Recycle() => this.Clear();
    }

    public static class ThreadLocalListPool
    {
        private const int DefaultCapacity = 8;
        private static readonly ThreadLocalPool<PoolList> Pool = new ThreadLocalPool<PoolList>();
        
        public static PoolList Acquire(int capacity = DefaultCapacity)
        {
            var recycleList = Pool.Acquire(out var handle);
            recycleList.Handle = handle;
            if (recycleList.Capacity < capacity)
            {
                recycleList.Capacity = capacity;
            }
            return recycleList;
        } 
        
        public static void Recycle(PoolList reference)
        {
            Pool.Recycle(reference.Handle);
        }
    }
}
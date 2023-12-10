namespace DotNetty.Common
{
    using System.Collections.Generic;

    public class RecycleList : List<object>, IRecycle
    {
        internal IRecycleHandle<RecycleList> Handle;
        void IRecycle.Recycle() => this.Clear();
    }

    public static class ThreadLocalListPool
    {
        const int DefaultCapacity = 8;
        private static readonly ThreadLocalPool<RecycleList> Pool = new ThreadLocalPool<RecycleList>();
        
        public static RecycleList Acquire(int capacity = DefaultCapacity)
        {
            var recycleList = Pool.Acquire(out var handle);
            recycleList.Handle = handle;
            if (recycleList.Capacity < capacity)
            {
                recycleList.Capacity = capacity;
            }
            return recycleList;
        } 
        
        public static void Recycle(RecycleList reference)
        {
            Pool.Recycle(reference.Handle);
        }
    }
}
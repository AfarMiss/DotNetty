using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DotNetty.Common
{
    public sealed class ThreadLocalMap
    {
        /// <summary>
        /// 线程回收时候自动释放
        /// </summary>
        [ThreadStatic]
        private static ThreadLocalMap threadLocalMap;
        private static int nextIndex;

        public static readonly object UNSET = new object();
        private object[] slotArray = CreateNewTable();

        // TODO ~ThreadLocalMap时Slot为ThreadLocalQueue<T>的对象标记回收 ~RecycleHandle时判断Queue标记可判定释放泄露 
        internal static int NextIndex()
        {
            int index = Interlocked.Increment(ref nextIndex);
            if (index < 0)
            {
                Interlocked.Decrement(ref nextIndex);
                throw new InvalidOperationException("too many thread-local indexed variables");
            }
            return index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThreadLocalMap GetIfSet() => threadLocalMap;

        /// <summary>
        /// 获取当前线程的映射值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ThreadLocalMap Get()
        {
            var ret = threadLocalMap;
            if (ret == null)
            {
                ret = new ThreadLocalMap();
                threadLocalMap = ret;
            }
            return ret;
        }

        private static object[] CreateNewTable()
        {
            var array = new object[32];
            Array.Fill(array, UNSET);
            return array;
        }

        private void GrowTable(int index)
        {
            object[] oldArray = this.slotArray;
            int oldCapacity = oldArray.Length;
            int newCapacity = index;
            newCapacity |= newCapacity >> 1;
            newCapacity |= newCapacity >> 2;
            newCapacity |= newCapacity >> 4;
            newCapacity |= newCapacity >> 8;
            newCapacity |= newCapacity >> 16;

            newCapacity++;

            var newArray = new object[newCapacity];
            oldArray.CopyTo(newArray, 0);
            Array.Fill(newArray, UNSET, oldCapacity, newArray.Length - oldCapacity);
            this.slotArray = newArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetSlot(int index)
        {
            var lookup = this.slotArray;
            return index < lookup.Length ? lookup[index] : UNSET;
        }

        public bool SetSlot(int index, object value)
        {
            object[] lookup = this.slotArray;
            if (index < lookup.Length)
            {
                object oldValue = lookup[index];
                lookup[index] = value;
                return oldValue == UNSET;
            }

            this.GrowTable(index);
            this.slotArray[index] = value;
            return true;
        }

        public object Remove(int index)
        {
            object[] lookup = this.slotArray;
            if (index < lookup.Length)
            {
                object v = lookup[index];
                lookup[index] = UNSET;
                return v;
            }

            return UNSET;
        }

        public bool Contains(int index)
        {
            object[] lookup = this.slotArray;
            return index < lookup.Length && lookup[index] != UNSET;
        }
        
        public static void Remove() => threadLocalMap = null;

        public static void Destroy() => threadLocalMap = null;
    }
}
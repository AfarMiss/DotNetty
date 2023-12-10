namespace DotNetty.Common
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public abstract class FastThreadLocal
    {
        /// <summary>
        /// 全局管理 映射<see cref="FastThreadLocal"/>
        /// </summary>
        public static readonly int RemoveIndex = ThreadLocalMap.NextIndex();

        /// <summary>
        /// 当前线程移除所有
        /// </summary>
        public static void RemoveAll()
        {
            ThreadLocalMap threadLocalMap = ThreadLocalMap.GetIfSet();
            if (threadLocalMap == null) return;

            try
            {
                var v = threadLocalMap.GetSlot(RemoveIndex);
                if (v != null && v != ThreadLocalMap.UNSET)
                {
                    var variablesToRemove = (HashSet<FastThreadLocal>)v;
                    foreach (var tlv in variablesToRemove)
                    {
                        tlv.Remove(threadLocalMap);
                    }
                }
            }
            finally
            {
                ThreadLocalMap.Remove();
            }
        }

        /// <summary>
        /// Destroys the data structure that keeps all <see cref="FastThreadLocal"/> variables accessed from
        /// non-<see cref="FastThreadLocal"/>s.  This operation is useful when you are in a container environment, and
        /// you do not want to leave the thread local variables in the threads you do not manage.  Call this method when
        /// your application is being unloaded from the container.
        /// </summary>
        public static void Destroy() => ThreadLocalMap.Destroy();

        protected static void Add(ThreadLocalMap threadLocalMap, FastThreadLocal variable)
        {
            object v = threadLocalMap.GetSlot(RemoveIndex);
            HashSet<FastThreadLocal> variablesToRemove;
            if (v == ThreadLocalMap.UNSET || v == null)
            {
                variablesToRemove = new HashSet<FastThreadLocal>(); // Collections.newSetFromMap(new IdentityHashMap<FastThreadLocal<?>, Boolean>());
                threadLocalMap.SetSlot(RemoveIndex, variablesToRemove);
            }
            else
            {
                variablesToRemove = (HashSet<FastThreadLocal>)v;
            }

            variablesToRemove.Add(variable);
        }

        protected static void Remove(ThreadLocalMap threadLocalMap, FastThreadLocal variable)
        {
            object v = threadLocalMap.GetSlot(RemoveIndex);

            if (v == ThreadLocalMap.UNSET || v == null)
            {
                return;
            }

            var variablesToRemove = (HashSet<FastThreadLocal>)v;
            variablesToRemove.Remove(variable);
        }

        /// <summary>
        ///     Sets the value to uninitialized; a proceeding call to get() will trigger a call to GetInitialValue().
        /// </summary>
        /// <param name="threadLocalMap"></param>
        protected abstract void Remove(ThreadLocalMap threadLocalMap);
    }

    public class FastThreadLocal<T> : FastThreadLocal where T : class
    {
        private readonly int index = ThreadLocalMap.NextIndex();

        /// <inheritdoc cref="ThreadLocalMap.Get()"/>
        public T Value
        {
            get => this.Get(ThreadLocalMap.Get());
            set => this.Set(ThreadLocalMap.Get(), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Get(ThreadLocalMap threadLocalMap)
        {
            object v = threadLocalMap.GetSlot(this.index);
            if (v != ThreadLocalMap.UNSET)
            {
                return (T)v;
            }

            return this.Initialize(threadLocalMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Initialize(ThreadLocalMap threadLocalMap)
        {
            T v = this.GetInitialValue();

            threadLocalMap.SetSlot(this.index, v);
            Add(threadLocalMap, this);
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(ThreadLocalMap threadLocalMap, T value)
        {
            if (threadLocalMap.SetSlot(this.index, value))
            {
                Add(threadLocalMap, this);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if and only if this thread-local variable is set.
        /// </summary>
        public bool IsSet() => this.IsSet(ThreadLocalMap.GetIfSet());

        /// <summary>
        /// Returns <c>true</c> if and only if this thread-local variable is set.
        /// The specified thread local map must be for the current thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSet(ThreadLocalMap threadLocalMap) => threadLocalMap != null && threadLocalMap.Contains(this.index);

        /// <summary>
        /// Returns the initial value for this thread-local variable.
        /// </summary>
        protected virtual T GetInitialValue() => null;

        public void Remove() => this.Remove(ThreadLocalMap.GetIfSet());

        /// <summary>
        /// Sets the value to uninitialized for the specified thread local map;
        /// a proceeding call to <see cref="Get"/> will trigger a call to <see cref="GetInitialValue"/>.
        /// The specified thread local map must be for the current thread.
        /// </summary>
        /// <param name="threadLocalMap">
        /// The <see cref="ThreadLocalMap"/> from which this <see cref="FastThreadLocal"/> should be removed.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected sealed override void Remove(ThreadLocalMap threadLocalMap)
        {
            if (threadLocalMap == null) return;

            object v = threadLocalMap.Remove(this.index);
            Remove(threadLocalMap, this);

            if (v != ThreadLocalMap.UNSET)
            {
                this.OnRemove((T)v);
            }
        }

        protected virtual void OnRemove(T value) { }
    }
}
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetty.Common
{
    public abstract class FastThreadLocal
    {
        /// <summary>
        /// 全局管理 映射<see cref="FastThreadLocal"/>
        /// </summary>
        private static readonly int RemoveIndex = ThreadLocalMap.NextIndex();

        /// <summary>
        /// 当前线程移除所有
        /// </summary>
        public static void RemoveAll()
        {
            var threadLocalMap = ThreadLocalMap.GetIfSet();
            if (threadLocalMap == null) return;

            try
            {
                var value = threadLocalMap.GetValue(RemoveIndex);
                if (value != null && value != ThreadLocalMap.UNSET)
                {
                    var variablesToRemove = (HashSet<FastThreadLocal>)value;
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
            var value = threadLocalMap.GetValue(RemoveIndex);
            HashSet<FastThreadLocal> removeSet;
            if (value == ThreadLocalMap.UNSET || value == null)
            {
                removeSet = new HashSet<FastThreadLocal>(); // Collections.newSetFromMap(new IdentityHashMap<FastThreadLocal<?>, Boolean>());
                threadLocalMap.SetValue(RemoveIndex, removeSet);
            }
            else
            {
                removeSet = (HashSet<FastThreadLocal>)value;
            }

            removeSet.Add(variable);
        }

        protected static void Remove(ThreadLocalMap threadLocalMap, FastThreadLocal variable)
        {
            var value = threadLocalMap.GetValue(RemoveIndex);
            if (value == ThreadLocalMap.UNSET || value == null) return;

            var variablesToRemove = (HashSet<FastThreadLocal>)value;
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
            var value = threadLocalMap.GetValue(this.index);
            if (value != ThreadLocalMap.UNSET)
            {
                return (T)value;
            }

            return this.Initialize(threadLocalMap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T Initialize(ThreadLocalMap threadLocalMap)
        {
            var value = this.GetInitialValue();
            threadLocalMap.SetValue(this.index, value);
            Add(threadLocalMap, this);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set(ThreadLocalMap threadLocalMap, T value)
        {
            if (threadLocalMap.SetValue(this.index, value))
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

            var remove = threadLocalMap.Remove(this.index);
            Remove(threadLocalMap, this);

            if (remove != ThreadLocalMap.UNSET)
            {
                this.OnRemove((T)remove);
            }
        }

        protected virtual void OnRemove(T value) { }
    }
}
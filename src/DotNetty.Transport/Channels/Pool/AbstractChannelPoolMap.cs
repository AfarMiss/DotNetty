using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace DotNetty.Transport.Channels.Pool
{
    public abstract class AbstractChannelPoolMap<TKey, TPool> : IChannelPoolMap<TKey, TPool> where TPool : IChannelPool
    {
        private readonly ConcurrentDictionary<TKey, TPool> map = new ConcurrentDictionary<TKey, TPool>();

        public TPool Get(TKey key)
        {
            Contract.Requires(key != null);

            if (!this.map.TryGetValue(key, out var pool))
            {
                pool = this.NewPool(key);
                var old = this.map.GetOrAdd(key, pool);
                if (!ReferenceEquals(old, pool))
                {
                    pool.Dispose();
                    pool = old;
                }
            }

            return pool;
        }

        public bool Remove(TKey key)
        {
            Contract.Requires(key != null);
            if (this.map.TryRemove(key, out var pool))
            {
                pool.Dispose();
                return true;
            }
            return false;
        }

        public int Count => this.map.Count;

        public bool IsEmpty => this.map.Count == 0;

        public bool Contains(TKey key)
        {
            Contract.Requires(key != null);
            return this.map.ContainsKey(key);
        } 
        
        protected abstract TPool NewPool(TKey key);

        public void Dispose()
        {
            foreach (var key in this.map.Keys)
            {
                this.Remove(key);
            }
        }
    }
}
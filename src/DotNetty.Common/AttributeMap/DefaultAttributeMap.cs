using System.Collections.Concurrent;
using System.Threading;

namespace DotNetty.Common.Utilities
{
    public class DefaultAttributeMap : IAttributeMap
    {
        private readonly ConcurrentDictionary<IConstant, object> attributes = new ConcurrentDictionary<IConstant, object>();

        public IAttribute<T> GetAttribute<T>(AttributeKey<T> key) where T : class
        {
            if (!this.attributes.TryGetValue(key, out var attribute))
            {
                attribute = new DefaultAttribute<T>(key);
                this.attributes.TryAdd(key, attribute);
            }

            return (IAttribute<T>)attribute;
        }

        public bool HasAttribute<T>(AttributeKey<T> key) where T : class
        {
            return this.attributes.ContainsKey(key);
        }

        private sealed class DefaultAttribute<T> : IAttribute<T> where T : class
        {
            public AttributeKey<T> Key { get; }
            private T value;

            public DefaultAttribute(AttributeKey<T> key) => this.Key = key;

            public T Get() => Volatile.Read(ref this.value);
            public void Set(T value) => Volatile.Write(ref this.value, value);

            public T GetAndSet(T value) => Interlocked.Exchange(ref this.value, value);
        }
    }
}
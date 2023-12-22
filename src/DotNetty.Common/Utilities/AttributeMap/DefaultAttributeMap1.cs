using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DotNetty.Common.Utilities
{
    public interface IConstantValue
    {
        public bool TransferSet(IConstantAccessor accessor);
    }
        
    public interface IConstantAccessor
    {
        public bool ConstantSet<T>(IConstant constant, T value);
    }
    
    public class DefaultAttributeMap
    {
        public readonly ConcurrentDictionary<IConstant, IConstantValue> attributes;

        public DefaultAttributeMap()
        {
            attributes = new ConcurrentDictionary<IConstant, IConstantValue>();
        }
        public DefaultAttributeMap(DefaultAttributeMap attributeMap)
        {
            attributes = new ConcurrentDictionary<IConstant, IConstantValue>(attributeMap.attributes);
        }
        
        public IConstantValue<T> GetAttribute<T>(IConstant<T> key)
        {
            if (!this.attributes.TryGetValue(key, out var attribute))
            {
                attribute = new DefaultAttribute<T>(this, key);
                this.attributes.TryAdd(key, attribute);
            }

            return (IConstantValue<T>)attribute;
        }

        public bool HasAttribute<T>(AttributeKey<T> key) where T : class
        {
            return this.attributes.ContainsKey(key);
        }
        
        public bool DelAttribute(IConstant key)
        {
            return this.attributes.TryRemove(key, out _);
        }
        
        public ICollection<IConstantValue> Values => attributes.Values;

        public interface IConstantValue<T> : IConstantValue
        {
            T Get();
            void Set(T value);
            T GetAndSet(T value);
        }
        
        private sealed class DefaultAttribute<T> : IConstantValue<T>
        {
            private readonly DefaultAttributeMap attributeMap;
            public IConstant<T> Key;
            private Data data;
            private class Data
            {
                public T Value;
            }
            
            public DefaultAttribute(DefaultAttributeMap attributeMap, IConstant<T> key)
            {
                this.attributeMap = attributeMap;
                this.Key = key;
            }

            public bool TransferSet(IConstantAccessor accessor) => accessor.ConstantSet(this.Key, this.data.Value);
            public T Get()
            {
                return Volatile.Read(ref this.data).Value;
            }

            public void Set(T value)
            {
                var data1 = this.data;
                data1.Value = value;
                Volatile.Write(ref this.data, data1);
            }

            public T GetAndSet(T value)
            {
                var data1 = this.data;
                data1.Value = value;
                return Interlocked.Exchange(ref this.data, data1).Value;
            }
        }
    }
}
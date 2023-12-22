using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DotNetty.Common.Utilities
{
    public interface IConstantTransfer
    {
        protected internal bool TransferSet<T>(IConstant<T> constant, T value);
    }

    public interface IConstantAccessor
    {
        public bool TransferSet(IConstantTransfer transfer);
    }

    internal sealed class ConstantAccessor<T> : IConstantAccessor
    {
        private readonly IConstant<T> key;
        internal T Value;

        public ConstantAccessor(IConstant key, T value = default)
        {
            this.key = (IConstant<T>)key;
            this.Value = value;
        }

        public bool TransferSet(IConstantTransfer transfer) => transfer.TransferSet(this.key, this.Value);
    }

    public class ConstantMap : IEnumerable<KeyValuePair<IConstant, IConstantAccessor>>
    {
        private readonly ConcurrentDictionary<IConstant, IConstantAccessor> attributes;

        public ConstantMap()
        {
            this.attributes = new ConcurrentDictionary<IConstant, IConstantAccessor>();
        }

        public ConstantMap(ConstantMap attributeMap)
        {
            this.attributes = new ConcurrentDictionary<IConstant, IConstantAccessor>(attributeMap.attributes);
        }

        public T GetConstant<T>(IConstant<T> key)
        {
            var accessor = (ConstantAccessor<T>)this.attributes.GetOrAdd(key, constant => new ConstantAccessor<T>(key));
            return accessor.Value;
        }

        public bool HasConstant<T>(IConstant<T> key)
        {
            return this.attributes.ContainsKey(key);
        }

        public bool DelConstant<T>(IConstant<T> key)
        {
            return this.attributes.TryRemove(key, out _);
        }

        public void SetConstant<T>(IConstant<T> key, T value)
        {
            this.attributes.AddOrUpdate(key, (constant, arg) => new ConstantAccessor<T>(constant, arg),
                (constant, accessor, arg) =>
                {
                    ((ConstantAccessor<T>)accessor).Value = arg;
                    return accessor;
                }, value);
        }

        public IEnumerator<KeyValuePair<IConstant, IConstantAccessor>> GetEnumerator() => this.attributes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public ICollection<IConstantAccessor> Values => this.attributes.Values;
    }
}
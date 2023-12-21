using System;
using System.Collections.Concurrent;

namespace DotNetty.Common.Utilities
{
    internal sealed class ConstantPool
    {
        private static readonly ConcurrentDictionary<string, ConstantPool> Pools = new ConcurrentDictionary<string, ConstantPool>();
        public static ConstantPool GetSharedPool<T>() => Pools.GetOrAdd(typeof(T).Name, new ConstantPool());
        
        private readonly ConcurrentDictionary<string, object> constants = new ConcurrentDictionary<string, object>();

        internal T ValueOf<T>(string name) where T : new()
        {
            return (T)this.constants.GetOrAdd(name, this.NewInstance0<T>(name));
        }

        internal bool Exists(string name) => this.constants.ContainsKey(name);

        internal T NewInstance<T>(string name) where T : new()
        {
            if (this.constants.ContainsKey(name)) throw new ArgumentException($"'{name}' is already in use");
            return this.NewInstance0<T>(name);
        }

        private T NewInstance0<T>(string name) where T : new()
        {
            var constant = new T();
            this.constants.TryAdd(name, constant);
            return constant;
        }
    }
}
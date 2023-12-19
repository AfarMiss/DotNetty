using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DotNetty.Common.Utilities
{
    internal interface IConstantPool
    {
        internal T ValueOf<T>(string name) where T : Constant, new();
        internal bool Exists(string name);
        internal T NewInstance<T>(string name) where T : Constant, new();
    }

    public abstract class ConstantPool : IConstantPool
    {
        private readonly ConcurrentDictionary<string, object> constants = new ConcurrentDictionary<string, object>();
        private int nextId;

        T IConstantPool.ValueOf<T>(string name)
        {
            return this.constants.TryGetValue(name, out var constant) ? (T)constant : this.NewInstance0<T>(name);
        }

        bool IConstantPool.Exists(string name) => this.constants.ContainsKey(name);

        T IConstantPool.NewInstance<T>(string name)
        {
            if (this.constants.ContainsKey(name)) throw new ArgumentException($"'{name}' is already in use");
            return this.NewInstance0<T>(name);
        }

        private T NewInstance0<T>(string name) where T : Constant, new()
        {
            var constant = new T();
            constant.Initialize(this.nextId, name);
            this.constants.TryAdd(name, constant);
            Interlocked.Increment(ref this.nextId);
            return constant;
        }
    }

    internal static class ConstantPool<T> where T : ConstantPool, new()
    {
        public static readonly T Pool = new T();
    }
}
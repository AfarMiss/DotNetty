using System;
using System.Collections.Concurrent;
using System.Threading;

namespace DotNetty.Common.Utilities
{
    public abstract class ConstantPool<T> where T : IConstant
    {
        private readonly ConcurrentDictionary<string, IConstant> constants = new ConcurrentDictionary<string, IConstant>();
        private int nextId;

        protected virtual T GetInitialValue(in int id, string name) => default;

        public IConstant ValueOf(string name)
        {
            return this.constants.TryGetValue(name, out var constant) ? constant : this.NewInstance0(name);
        }

        public bool Exists(string name) => this.constants.ContainsKey(name);

        public IConstant NewInstance(string name)
        {
            if (this.Exists(name)) throw new ArgumentException($"'{name}' is already in use");
            return this.NewInstance0(name);
        }

        private IConstant NewInstance0(string name)
        {
            IConstant constant = this.GetInitialValue(this.nextId, name);
            this.constants.TryAdd(name, constant);
            Interlocked.Increment(ref this.nextId);
            return constant;
        }
    }
}
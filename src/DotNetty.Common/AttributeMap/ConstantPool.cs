using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace DotNetty.Common.Utilities
{
    public abstract class ConstantPool
    {
        private readonly ConcurrentDictionary<string, IConstant> constants = new ConcurrentDictionary<string, IConstant>();
        private int nextId;

        public IConstant ValueOf<T>(string name)
        {
            return this.constants.TryGetValue(name, out var constant) ? constant : this.NewInstance0<T>(name);
        }

        public bool Exists(string name)
        {
            lock (this.constants)
            {
                return this.constants.ContainsKey(name);
            }
        }

        public IConstant NewInstance<T>(string name)
        {
            if (this.Exists(name)) throw new ArgumentException($"'{name}' is already in use");
            return this.NewInstance0<T>(name);
        }

        private IConstant NewInstance0<T>(string name)
        {
            var constant = this.NewConstant<T>(this.nextId, name);
            this.constants.TryAdd(name, constant);
            Interlocked.Increment(ref this.nextId);
            return constant;
        }

        protected abstract IConstant NewConstant<T>(int id, string name);
    }
}
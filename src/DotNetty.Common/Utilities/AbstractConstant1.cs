using System;

namespace DotNetty.Common.Utilities
{
    public abstract class AbstractConstant<T> : IConstant, IComparable<T>, IEquatable<T> where T : AbstractConstant<T>, new()
    {
        private static readonly ConstantPool<T> Pool = new ConstantPool();
        
        public int Id { get; private set; }
        public string Name { get; private set; }

        private sealed class ConstantPool : ConstantPool<T>
        {
            protected override T GetInitialValue(in int id, string name)
            {
                var constant = new T();
                constant.Id = id;
                constant.Name = name;
                constant.Initialize();
                return constant;
            }
        }

        public static T ValueOf(string name) => (T)Pool.ValueOf(name);
        public static bool Exists(string name) => Pool.Exists(name);
        public static T NewInstance(string name) => (T)Pool.NewInstance(name);
        
        protected virtual void Initialize() { }
        
        public sealed override string ToString() => this.Name;

        public bool Equals(T other) => ReferenceEquals(this, other);

        public int CompareTo(T other)
        {
            if (ReferenceEquals(this, other)) return 0;

            var returnCode = this.GetHashCode() - other.GetHashCode();
            if (returnCode != 0)
                return returnCode;
            if (this.Id < other.Id)
                return -1;
            if (this.Id > other.Id)
                return 1;

            throw new Exception("failed to compare two different constants");
        }
    }
}
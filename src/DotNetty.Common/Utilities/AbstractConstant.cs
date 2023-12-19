using System;

namespace DotNetty.Common.Utilities
{
    public abstract class Constant : IConstant, IComparable<Constant>, IEquatable<Constant>
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public virtual void Initialize(int id, string name)
        {
            this.Id = id;
            this.Name = name;
        }
        
        public sealed override string ToString() => this.Name;

        public bool Equals(Constant other) => ReferenceEquals(this, other);

        public int CompareTo(Constant other)
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
    
    public abstract class AbstractConstant<TPool, TKey> : Constant where TPool : ConstantPool, new() where TKey : AbstractConstant<TPool, TKey>, new()
    {
        private static readonly IConstantPool Pool = ConstantPool<TPool>.Pool;

        public static TKey ValueOf(string name) => Pool.ValueOf<TKey>(name);
        public static bool Exists(string name) => Pool.Exists(name);
        public static TKey NewInstance(string name) => Pool.NewInstance<TKey>(name);
    }
}
using System.Collections.Generic;

namespace DotNetty.Common.Utilities
{
    public interface IHashingStrategy<in T> : IEqualityComparer<T>
    {
        int HashCode(T obj);
    }

    public sealed class DefaultHashingStrategy<T> : IHashingStrategy<T>
    {
        public int GetHashCode(T obj) => obj.GetHashCode();

        public int HashCode(T obj) => obj != null ? this.GetHashCode(obj) : 0;

        public bool Equals(T a, T b) => ReferenceEquals(a, b) || (!ReferenceEquals(a, null) && a.Equals(b));
    }
}

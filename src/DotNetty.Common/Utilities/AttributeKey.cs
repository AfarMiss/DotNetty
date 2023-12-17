using System;

namespace DotNetty.Common.Utilities
{
    internal static class AttributeKey
    {
        public static readonly ConstantPool Pool = new AttributeConstantPool();

        private sealed class AttributeConstantPool : ConstantPool
        {
            protected override IConstant NewConstant<TValue>(in int id, string name) => new AttributeKey<TValue>(id, name);
        }
    }

    public sealed class AttributeKey<T> : AbstractConstant<AttributeKey<T>>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ConstantPool Pool = AttributeKey.Pool;

        internal AttributeKey(int id, string name) : base(id, name)
        {
        }
        
        public static AttributeKey<T> ValueOf(string name) => (AttributeKey<T>)Pool.ValueOf<T>(name);
        public static bool Exists(string name) => Pool.Exists(name);

        public static AttributeKey<T> NewInstance(string name) => (AttributeKey<T>)Pool.NewInstance<T>(name);
    }
}
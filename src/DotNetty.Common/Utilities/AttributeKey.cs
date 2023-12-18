using System;

namespace DotNetty.Common.Utilities
{
    // internal static class AttributeKey
    // {
    //     public static readonly ConstantPool Pool = new AttributeConstantPool();
    //
    //     private sealed class AttributeConstantPool : ConstantPool
    //     {
    //         protected override IConstant NewConstant<TValue>(in int id, string name) => new AttributeKey<TValue>(id, name);
    //     }
    // }

    public sealed class AttributeKey : AbstractConstant<AttributeKey>
    {
        private void fff()
        {
            var attributeKey = ValueOf("111");
        }
    }
    
    public sealed class AttributeKey<T> : AbstractConstant<AttributeKey<T>>
    {
    }
}
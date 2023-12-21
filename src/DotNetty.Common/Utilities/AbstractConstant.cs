namespace DotNetty.Common.Utilities
{
    public interface IConstant
    {

    }
    
    public abstract class AbstractConstant<TPool, TKey> : IConstant where TPool : ConstantPool, new() where TKey : AbstractConstant<TPool, TKey>, new()
    {
        private static readonly IConstantPool Pool = ConstantPool<TPool>.Pool;

        public static TKey ValueOf(string name) => Pool.ValueOf<TKey>(name);
        public static bool Exists(string name) => Pool.Exists(name);
        public static TKey NewInstance(string name) => Pool.NewInstance<TKey>(name);
    }
}
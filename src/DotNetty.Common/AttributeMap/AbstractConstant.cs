namespace DotNetty.Common.Utilities
{
    public interface IConstant
    {
        
    }
    
    public abstract class AbstractConstant<TKey> : IConstant where TKey : AbstractConstant<TKey>, new()
    {
        private static readonly ConstantPool Pool = ConstantPool.GetSharedPool<TKey>();

        public static TKey ValueOf(string name) => Pool.ValueOf<TKey>(name);
        public static bool Exists(string name) => Pool.Exists(name);
        public static TKey NewInstance(string name) => Pool.NewInstance<TKey>(name);
    }
}
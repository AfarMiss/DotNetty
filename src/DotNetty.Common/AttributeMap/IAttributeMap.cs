namespace DotNetty.Common.Utilities
{
    /// <summary>持有 <see cref="IAttribute{T}"/> 容器</summary>
    /// <remarks>实现必须是线程安全</remarks>
    public interface IAttributeMap
    {
        IAttribute<T> GetAttribute<T>(AttributeKey<T> key) where T : class;
        bool HasAttribute<T>(AttributeKey<T> key) where T : class;
    }
}
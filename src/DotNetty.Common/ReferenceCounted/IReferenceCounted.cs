namespace DotNetty.Common
{
    public interface IReferenceCounted
    {
        int ReferenceCount { get; }
        /// 增加引用计数
        IReferenceCounted Retain(int increment = 1);
        ///  减少引用计数 归0则释放对象
        bool Release(int decrement = 1);
    }
}
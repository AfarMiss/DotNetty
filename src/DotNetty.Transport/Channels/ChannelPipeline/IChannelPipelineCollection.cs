using DotNetty.Common.Concurrency;

namespace DotNetty.Transport.Channels
{
    public interface IChannelPipelineCollection
    {
        IChannelHandlerContext FirstContext();
        IChannelHandlerContext LastContext();

        IChannelHandlerContext Context(IChannelHandler handler);
        IChannelHandlerContext Context(string name);
        IChannelHandlerContext Context<T>() where T : class, IChannelHandler;
        
        void AddFirst(string name, IChannelHandler handler);
        /// <summary>
        /// group为null则默认为通道执行组
        /// </summary>
        void AddFirst(IEventExecutorGroup group, string name, IChannelHandler handler);
        void AddLast(string name, IChannelHandler handler);
        void AddLast(IEventExecutorGroup group, string name, IChannelHandler handler);
        void AddBefore(string baseName, string name, IChannelHandler handler);
        void AddBefore(IEventExecutorGroup group, string baseName, string name, IChannelHandler handler);
        void AddAfter(string baseName, string name, IChannelHandler handler);
        void AddAfter(IEventExecutorGroup group, string baseName, string name, IChannelHandler handler);

        void AddFirst(params IChannelHandler[] handlers);
        void AddFirst(IEventExecutorGroup group, params IChannelHandler[] handlers);
        void AddLast(params IChannelHandler[] handlers);
        void AddLast(IEventExecutorGroup group, params IChannelHandler[] handlers);

        IChannelPipeline Remove(IChannelHandler handler);
        IChannelHandler Remove(string name);
        T Remove<T>() where T : class, IChannelHandler;
        IChannelHandler RemoveFirst();
        IChannelHandler RemoveLast();

        void Replace(IChannelHandler oldHandler, string newName, IChannelHandler newHandler);
        void Replace(string oldName, string newName, IChannelHandler newHandler);
        T Replace<T>(string newName, IChannelHandler newHandler) where T : class, IChannelHandler;
    
        IChannelHandler First();
        IChannelHandler Last();
        
        IChannelHandler Get(string name);
        T Get<T>() where T : class, IChannelHandler;
    }
}
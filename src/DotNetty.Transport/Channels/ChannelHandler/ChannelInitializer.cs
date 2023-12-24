using System;
using System.Collections.Concurrent;
using DotNetty.Common.Internal.Logging;

namespace DotNetty.Transport.Channels
{
    /// <summary>
    /// 特殊<see cref="IChannelHandler"/> 辅助<see cref="IChannel"/>注册到<see cref="IEventLoop"/>后初始化
    /// </summary>
    public abstract class ChannelInitializer<T> : ChannelHandlerAdapter where T : IChannel
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ChannelInitializer<T>>();
        private readonly ConcurrentDictionary<IChannelHandlerContext, bool> initMap = new ConcurrentDictionary<IChannelHandlerContext, bool>();
        
        public override bool IsSharable => true;
        
        /// <summary>
        /// 注册<see cref="IChannel"/>成功调用,方法执行后从<see cref="IChannelPipeline"/>移除
        /// </summary>
        protected abstract void InitChannel(T channel);

        public sealed override void ChannelRegistered(IChannelHandlerContext ctx)
        {
            if (this.InitChannel(ctx)) 
            {
                ctx.Channel.Pipeline.FireChannelRegistered();
            } 
            else 
            {
                ctx.FireChannelRegistered();
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
        {
            Logger.Warn("Failed to initialize a channel. Closing: " + ctx.Channel, cause);
            ctx.CloseAsync();
        }

        public override void HandlerAdded(IChannelHandlerContext ctx)
        {
            if (ctx.Channel.Registered)
            {
                this.InitChannel(ctx);
            }
        }

        private bool InitChannel(IChannelHandlerContext ctx)
        {
            if (this.initMap.TryAdd(ctx, true))
            {
                try
                {
                    this.InitChannel((T) ctx.Channel);
                }
                catch (Exception cause)
                {
                    this.ExceptionCaught(ctx, cause);
                }
                finally
                {
                    this.Remove(ctx);
                }
                return true;
            }
            return false;
        }

        private void Remove(IChannelHandlerContext ctx)
        {
            try
            {
                var pipeline = ctx.Channel.Pipeline;
                if (pipeline.Context(this) != null)
                {
                    pipeline.Remove(this);
                }
            }
            finally
            {
                this.initMap.TryRemove(ctx, out _);
            }
        }
    }
}
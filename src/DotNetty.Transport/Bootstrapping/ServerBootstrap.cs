using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace DotNetty.Transport.Bootstrapping
{
    /// <summary> 服务端引导 </summary>
    public class ServerBootstrap : AbstractBootstrap<ServerBootstrap, IServerChannel>
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ServerBootstrap>();

        private readonly ConstantMap childOptions = new ConstantMap();
        private readonly ConstantMap childAttrs = new ConstantMap();
        private volatile IEventLoopGroup childGroup;
        private volatile IChannelHandler childHandler;

        public override ServerBootstrap SetGroup(IEventLoopGroup group) => this.SetGroup(group, group);

        public ServerBootstrap SetGroup(IEventLoopGroup parentGroup, IEventLoopGroup childGroup)
        {
            Contract.Requires(childGroup != null);

            base.SetGroup(parentGroup);
            if (this.childGroup != null)
            {
                throw new InvalidOperationException("childGroup set already");
            }
            this.childGroup = childGroup;
            return this;
        }

        public ServerBootstrap ChildOption<T>(ChannelOption<T> childOption, T value)
        {
            Contract.Requires(childOption != null);

            if (value == null)
            {
                this.childOptions.Remove(childOption);
            }
            else
            {
                this.childOptions.Set(childOption, value);
            }
            return this;
        }

        public ServerBootstrap ChildAttribute<T>(AttributeKey<T> childKey, T value) where T : class
        {
            Contract.Requires(childKey != null);

            if (value == null)
            {
                this.childAttrs.Remove(childKey);
            }
            else
            {
                this.childAttrs.Set(childKey, value);
            }
            return this;
        }

        public ServerBootstrap ChildHandler(IChannelHandler childHandler)
        {
            Contract.Requires(childHandler != null);

            this.childHandler = childHandler;
            return this;
        }

        public IEventLoopGroup ChildGroup() => this.childGroup;

        protected override void Init(IChannel channel)
        {
            SetChannelOptions(channel, this.Options, Logger);

            foreach (var (_, accessor) in this.Attrs)
            {
                accessor.TransferSet(channel);
            }

            var pipeline = channel.Pipeline;
            var channelHandler = this.Handler;
            if (channelHandler != null)
            {
                pipeline.AddLast((string)null, channelHandler);
            }

            var currentChildGroup = this.childGroup;
            var currentChildHandler = this.childHandler;
            var currentChildOptions = this.childOptions.Values.ToArray();
            var currentChildAttrs = this.childAttrs.Values.ToArray();

            pipeline.AddLast(new ActionChannelInitializer<IChannel>(ch =>
            {
                ch.Pipeline.AddLast(new ServerBootstrapAcceptor(currentChildGroup, currentChildHandler, currentChildOptions, currentChildAttrs));
            }));
        }

        protected override void Validate()
        {
            base.Validate();
            if (this.childHandler == null)
            {
                throw new InvalidOperationException("childHandler not set");
            }
            if (this.childGroup == null)
            {
                Logger.Warn("childGroup is not set. Using parentGroup instead.");
                this.childGroup = this.Group;
            }
        }

        private sealed class ServerBootstrapAcceptor : ChannelHandlerAdapter
        {
            private readonly IEventLoopGroup childGroup;
            private readonly IChannelHandler childHandler;
            private readonly IConstantAccessor[] childOptions;
            private readonly IConstantAccessor[] childAttrs;

            public ServerBootstrapAcceptor(IEventLoopGroup childGroup, IChannelHandler childHandler,IConstantAccessor[] childOptions, IConstantAccessor[] childAttrs)
            {
                this.childGroup = childGroup;
                this.childHandler = childHandler;
                this.childOptions = childOptions;
                this.childAttrs = childAttrs;
            }

            public override void ChannelRead(IChannelHandlerContext ctx, object msg)
            {
                var child = (IChannel)msg;

                child.Pipeline.AddLast((string)null, this.childHandler);

                SetChannelOptions(child, this.childOptions, Logger);

                foreach (var accessor in this.childAttrs)
                {
                    accessor.TransferSet(child);
                }

                // todo: async/await instead?
                try
                {
                    this.childGroup.RegisterAsync(child).ContinueWith(
                        (future, state) => ForceClose((IChannel)state, future.Exception), child,
                        TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }
                catch (Exception ex)
                {
                    ForceClose(child, ex);
                }
            }

            private static void ForceClose(IChannel child, Exception ex)
            {
                child.Unsafe.CloseForcibly();
                Logger.Warn("Failed to register an accepted channel: " + child, ex);
            }

            public override void ExceptionCaught(IChannelHandlerContext ctx, Exception cause)
            {
                IChannelConfiguration config = ctx.Channel.Configuration;
                if (config.AutoRead)
                {
                    // 停止接受新连接1秒以允许通道恢复
                    // See https://github.com/netty/netty/issues/1328
                    config.AutoRead = false;
                    ctx.Channel.EventLoop.ScheduleAsync(c => ((IChannelConfiguration)c).AutoRead = true, config, TimeSpan.FromSeconds(1));
                }

                ctx.FireExceptionCaught(cause);
            }
        }
    }
}
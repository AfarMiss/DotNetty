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

        private readonly ConstantMap childOptions;
        private readonly ConstantMap childAttrs;
        private volatile IEventLoopGroup childGroup;
        private volatile IChannelHandler childHandler;

        public IEventLoopGroup ChildGroup() => this.childGroup;

        public ServerBootstrap()
        {
           this.childOptions = new ConstantMap();
            this.childAttrs = new ConstantMap();
        }

        private ServerBootstrap(ServerBootstrap bootstrap) : base(bootstrap)
        {
            this.childGroup = bootstrap.childGroup;
            this.childHandler = bootstrap.childHandler;
            this.childOptions = new ConstantMap(bootstrap.childOptions);
            this.childAttrs = new ConstantMap(bootstrap.childAttrs);
        }

        public override ServerBootstrap SetGroup(IEventLoopGroup group) => this.SetGroup(group, group);

        public ServerBootstrap SetGroup(IEventLoopGroup parentGroup, IEventLoopGroup childGroup)
        {
            Contract.Requires(childGroup != null);

            base.SetGroup(parentGroup);
            if (this.childGroup != null)
            {
                throw new InvalidOperationException($"{nameof(this.childGroup)}已设置!");
            }
            this.childGroup = childGroup;
            return this;
        }

        public ServerBootstrap ChildOption<T>(ChannelOption<T> childOption, T value)
        {
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

        public ServerBootstrap ChildAttribute<T>(AttributeKey<T> childKey, T value)
        {
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
            this.childHandler = childHandler;
            return this;
        }
        
        protected override void Init(IChannel channel)
        {
            foreach (var (_, accessor) in this.Options)
            {
                accessor.TransferSet(channel.Configuration);
            }
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

            pipeline.AddLast(new ActionChannelInitializer<IChannel>(ch =>
            {
                ch.Pipeline.AddLast(new ServerBootstrapAcceptor(this.childGroup, this.childHandler, this.childOptions, this.childAttrs));
            }));
        }

        protected override void Validate()
        {
            base.Validate();
            if (this.childHandler == null)
            {
                throw new InvalidOperationException($"{nameof(this.childHandler)}未设置");
            }
            if (this.childGroup == null)
            {
                Logger.Warn($"{nameof(this.childGroup)}未设置. 使用基类{nameof(this.Group)}");
                this.childGroup = this.Group;
            }
        }

        private sealed class ServerBootstrapAcceptor : ChannelHandlerAdapter
        {
            private readonly IEventLoopGroup childGroup;
            private readonly IChannelHandler childHandler;
            private readonly ConstantMap childOptions;
            private readonly ConstantMap childAttrs;

            public ServerBootstrapAcceptor(IEventLoopGroup childGroup, IChannelHandler childHandler, ConstantMap childOptions, ConstantMap childAttrs)
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

                foreach (var (_, accessor) in this.childOptions)
                {
                    accessor.TransferSet(child.Configuration);
                }
                foreach (var (_, accessor) in this.childAttrs)
                {
                    accessor.TransferSet(child);
                }

                try
                {
                    this.childGroup.RegisterAsync(child).ContinueWith(
                        (task, state) => ForceClose((IChannel)state, task.Exception), child,
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

        public override ServerBootstrap Clone() => new ServerBootstrap(this);
    }
}
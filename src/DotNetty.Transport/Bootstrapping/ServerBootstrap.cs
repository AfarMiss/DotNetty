﻿using System;
using System.Collections.Concurrent;
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

        // private readonly ConcurrentDictionary<IConstant, ChannelOptionValue> childOptions;
        // private readonly ConcurrentDictionary<IConstant, AttributeValue> childAttrs;
        private readonly DefaultAttributeMap childOptions;
        private readonly DefaultAttributeMap childAttrs;
        private volatile IEventLoopGroup childGroup;
        private volatile IChannelHandler childHandler;

        public ServerBootstrap()
        {
            // this.childOptions = new ConcurrentDictionary<IConstant, ChannelOptionValue>();
            // this.childAttrs = new ConcurrentDictionary<IConstant, AttributeValue>();
            this.childOptions = new DefaultAttributeMap();
            this.childAttrs = new DefaultAttributeMap();
        }

        private ServerBootstrap(ServerBootstrap bootstrap) : base(bootstrap)
        {
            this.childGroup = bootstrap.childGroup;
            this.childHandler = bootstrap.childHandler;
            this.childOptions = new DefaultAttributeMap(bootstrap.childOptions);
            this.childAttrs = new DefaultAttributeMap(bootstrap.childAttrs);
        }

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
                // this.childOptions.TryRemove(childOption, out _);
                this.childOptions.DelAttribute(childOption);
            }
            else
            {
                // this.childOptions[childOption] = new ChannelOptionValue<T>(childOption, value);
                this.childOptions.SetAttribute(childOption, value);
            }
            return this;
        }

        public ServerBootstrap ChildAttribute<T>(AttributeKey<T> childKey, T value) where T : class
        {
            Contract.Requires(childKey != null);

            if (value == null)
            {
                // this.childAttrs.TryRemove(childKey, out _);
                this.childAttrs.DelAttribute(childKey);
            }
            else
            {
                this.childAttrs.SetAttribute(childKey, value);
                // this.childAttrs[childKey] = new AttributeValue<T>(childKey, value);
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

            foreach (var e in this.Attributes)
            {
                // e.Set(channel);
                e.TransferSet(channel);
            }

            IChannelPipeline p = channel.Pipeline;
            IChannelHandler channelHandler = this.Handler;
            if (channelHandler != null)
            {
                p.AddLast((string)null, channelHandler);
            }

            var currentChildGroup = this.childGroup;
            var currentChildHandler = this.childHandler;
            var currentChildOptions = this.childOptions.Values.ToArray();
            var currentChildAttrs = this.childAttrs.Values.ToArray();

            p.AddLast(new ActionChannelInitializer<IChannel>(ch =>
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

        private class ServerBootstrapAcceptor : ChannelHandlerAdapter
        {
            private readonly IEventLoopGroup childGroup;
            private readonly IChannelHandler childHandler;
            private readonly IConstantValue[] childOptions;
            private readonly IConstantValue[] childAttrs;

            public ServerBootstrapAcceptor(IEventLoopGroup childGroup, IChannelHandler childHandler,IConstantValue[] childOptions, IConstantValue[] childAttrs)
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

                foreach (var attr in this.childAttrs)
                {
                    attr.TransferSet(child);
                    // attr.Set(child);
                }

                // todo: async/await instead?
                try
                {
                    this.childGroup.RegisterAsync(child).ContinueWith(
                        (future, state) => ForceClose((IChannel)state, future.Exception),
                        child,
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
                    // stop accept new connections for 1 second to allow the channel to recover
                    // See https://github.com/netty/netty/issues/1328
                    config.AutoRead = false;
                    ctx.Channel.EventLoop.ScheduleAsync(c => ((IChannelConfiguration)c).AutoRead = true, config, TimeSpan.FromSeconds(1));
                }
                // still let the ExceptionCaught event flow through the pipeline to give the user
                // a chance to do something with it
                ctx.FireExceptionCaught(cause);
            }
        }

        public override ServerBootstrap Clone() => new ServerBootstrap(this);
    }
}
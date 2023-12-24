using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public class CombinedChannelDuplexHandler<TIn, TOut> : ChannelDuplexHandler where TIn : IChannelHandler where TOut : IChannelHandler
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<CombinedChannelDuplexHandler<TIn, TOut>>();

        private DelegatingChannelHandlerContext inboundCtx;
        private DelegatingChannelHandlerContext outboundCtx;
        private volatile bool handlerAdded;

        protected TIn InboundHandler { get; private set; }
        protected TOut OutboundHandler { get; private set; }
        
        protected CombinedChannelDuplexHandler()
        {
            this.EnsureNotSharable();
        }

        public CombinedChannelDuplexHandler(TIn inboundHandler, TOut outboundHandler)
        {
            Contract.Requires(inboundHandler != null);
            Contract.Requires(outboundHandler != null);

            this.EnsureNotSharable();
            this.Init(inboundHandler, outboundHandler);
        }

        protected void Init(TIn inbound, TOut outbound)
        {
            this.Validate(inbound, outbound);

            this.InboundHandler = inbound;
            this.OutboundHandler = outbound;
        }

        private void Validate(TIn inbound, TOut outbound)
        {
            if (this.InboundHandler != null)
            {
                throw new InvalidOperationException($"init() can not be invoked if {this.GetType().Name} was constructed with non-default constructor.");
            }

            if (inbound == null)
            {
                throw new ArgumentNullException(nameof(inbound));
            }

            if (outbound == null)
            {
                throw new ArgumentNullException(nameof(outbound));
            }
        }

        private void CheckAdded()
        {
            if (!this.handlerAdded)
            {
                throw new InvalidOperationException("handler not added to pipeline yet");
            }
        }

        public void RemoveInboundHandler()
        {
            this.CheckAdded();
            this.inboundCtx.Remove();
        }

        public void RemoveOutboundHandler()
        {
            this.CheckAdded();
            this.outboundCtx.Remove();
        }

        public override void HandlerAdded(IChannelHandlerContext context)
        {
            if (this.InboundHandler == null)
            {
                throw new InvalidOperationException($"Init() must be invoked before being added to a {nameof(IChannelPipeline)} if {this.GetType().Name} was constructed with the default constructor.");
            }

            this.outboundCtx = new DelegatingChannelHandlerContext(context, this.OutboundHandler);
            this.inboundCtx = new DelegatingChannelHandlerContext(context, this.InboundHandler,
                cause =>
                {
                    try
                    {
                        this.OutboundHandler.ExceptionCaught(this.outboundCtx, cause);
                    }
                    catch (Exception error)
                    {
                        if (Logger.DebugEnabled)
                        {
                            Logger.Debug("An exception {}"
                                + "was thrown by a user handler's exceptionCaught() "
                                + "method while handling the following exception:", error, cause);
                        }
                        else if (Logger.WarnEnabled)
                        {
                            Logger.Warn("An exception '{}' [enable DEBUG level for full stacktrace] "
                                + "was thrown by a user handler's exceptionCaught() "
                                + "method while handling the following exception:", error, cause);
                        }
                    }
                });

            this.handlerAdded = true;

            try
            {
                this.InboundHandler.HandlerAdded(this.inboundCtx);
            }
            finally
            {
                this.OutboundHandler.HandlerAdded(this.outboundCtx);
            }
        }

        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            try
            {
                this.inboundCtx.Remove();
            }
            finally
            {
                this.outboundCtx.Remove();
            }
        }

        public override void ChannelRegistered(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ChannelRegistered(this.inboundCtx);
            }
            else
            {
                this.inboundCtx.FireChannelRegistered();
            }
        }

        public override void ChannelUnregistered(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ChannelUnregistered(this.inboundCtx);
            }
            else
            {
                this.inboundCtx.FireChannelUnregistered();
            }
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ChannelActive(this.inboundCtx);
            }
            else
            {
                this.inboundCtx.FireChannelActive();
            }
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ChannelInactive(this.inboundCtx);
            }
            else
            {
                this.inboundCtx.FireChannelInactive();
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ExceptionCaught(this.inboundCtx, exception);
            }
            else
            {
                this.inboundCtx.FireExceptionCaught(exception);
            }
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.UserEventTriggered(this.inboundCtx, evt);
            }
            else
            {
                this.inboundCtx.FireUserEventTriggered(evt);
            }
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ChannelRead(this.inboundCtx, message);
            }
            else
            {
                this.inboundCtx.FireChannelRead(message);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ChannelReadComplete(this.inboundCtx);
            }
            else
            {
                this.inboundCtx.FireChannelReadComplete();
            }
        }

        public override void ChannelWritabilityChanged(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.inboundCtx.InnerContext);

            if (!this.inboundCtx.Removed)
            {
                this.InboundHandler.ChannelWritabilityChanged(this.inboundCtx);
            }
            else
            {
                this.inboundCtx.FireChannelWritabilityChanged();
            }
        }

        public override Task BindAsync(IChannelHandlerContext context, EndPoint localAddress)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                return this.OutboundHandler.BindAsync(this.outboundCtx, localAddress);
            }
            else
            {
                return this.outboundCtx.BindAsync(localAddress);
            }
        }

        public override Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                return this.OutboundHandler.ConnectAsync(this.outboundCtx, remoteAddress, localAddress);
            }
            else
            {
                return this.outboundCtx.ConnectAsync(localAddress);
            }
        }

        public override Task DisconnectAsync(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                return this.OutboundHandler.DisconnectAsync(this.outboundCtx);
            }
            else
            {
                return this.outboundCtx.DisconnectAsync();
            }
        }

        public override Task CloseAsync(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                return this.OutboundHandler.CloseAsync(this.outboundCtx);
            }
            else
            {
                return this.outboundCtx.CloseAsync();
            }
        }

        public override Task DeregisterAsync(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                return this.OutboundHandler.DeregisterAsync(this.outboundCtx);
            }
            else
            {
                return this.outboundCtx.DeregisterAsync();
            }
        }

        public override void Read(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                this.OutboundHandler.Read(this.outboundCtx);
            }
            else
            {
                this.outboundCtx.Read();
            }
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                return this.OutboundHandler.WriteAsync(this.outboundCtx, message);
            }
            else
            {
                return this.outboundCtx.WriteAsync(message);
            }
        }

        public override void Flush(IChannelHandlerContext context)
        {
            Contract.Assert(context == this.outboundCtx.InnerContext);

            if (!this.outboundCtx.Removed)
            {
                this.OutboundHandler.Flush(this.outboundCtx);
            }
            else
            {
                this.outboundCtx.Flush();
            }
        }

        private sealed class DelegatingChannelHandlerContext : IChannelHandlerContext
        {
            private readonly IChannelHandlerContext ctx;
            private readonly IChannelHandler handler;
            private readonly Action<Exception> onError;
            private bool removed;

            public DelegatingChannelHandlerContext(IChannelHandlerContext ctx, IChannelHandler handler, Action<Exception> onError = null)
            {
                this.ctx = ctx;
                this.handler = handler;
                this.onError = onError;
            }

            public IChannelHandlerContext InnerContext => this.ctx;

            public IChannel Channel => this.ctx.Channel;

            public IByteBufferAllocator Allocator => this.ctx.Allocator;

            public IEventExecutor Executor => this.ctx.Executor;

            public string Name => this.ctx.Name;

            public IChannelHandler Handler => this.ctx.Handler;

            public bool Removed => this.removed || this.ctx.Removed;

            public void FireChannelRegistered() => this.ctx.FireChannelRegistered();

            public void FireChannelUnregistered() => this.ctx.FireChannelUnregistered();

            public void FireChannelActive() => this.ctx.FireChannelActive();

            public void FireChannelInactive() => this.ctx.FireChannelInactive();

            public void FireExceptionCaught(Exception ex)
            {
                if (this.onError != null)
                {
                    this.onError(ex);
                }
                else
                {
                    this.ctx.FireExceptionCaught(ex);
                }
            }

            public void FireUserEventTriggered(object evt) => this.ctx.FireUserEventTriggered(evt);

            public void FireChannelRead(object message) => this.ctx.FireChannelRead(message);

            public void FireChannelReadComplete() => this.ctx.FireChannelReadComplete();

            public void FireChannelWritabilityChanged() => this.ctx.FireChannelWritabilityChanged();

            public Task BindAsync(EndPoint localAddress) => this.ctx.BindAsync(localAddress);

            public Task ConnectAsync(EndPoint remoteAddress) => this.ctx.ConnectAsync(remoteAddress);

            public Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress) => this.ctx.ConnectAsync(remoteAddress, localAddress);

            public Task DisconnectAsync() => this.ctx.DisconnectAsync();

            public Task CloseAsync() => this.ctx.CloseAsync();

            public Task DeregisterAsync() => this.ctx.DeregisterAsync();

            public void Read() => this.ctx.Read();

            public Task WriteAsync(object message) => this.ctx.WriteAsync(message);

            public void Flush() => this.ctx.Flush();

            public Task WriteAndFlushAsync(object message) => this.ctx.WriteAndFlushAsync(message);

            public ConstantMap ConstantMap => this.ctx.ConstantMap;

            internal void Remove()
            {
                var executor = this.Executor;
                if (executor.InEventLoop)
                {
                    this.Remove0();
                }
                else
                {
                    executor.Execute(this.Remove0);
                }
            }

            private void Remove0()
            {
                if (this.removed)
                {
                    return;
                }

                this.removed = true;
                try
                {
                    this.handler.HandlerRemoved(this);
                }
                catch (Exception cause)
                {
                    this.FireExceptionCaught(new ChannelPipelineException($"{this.handler.GetType().Name}.handlerRemoved() has thrown an exception.", cause));
                }
            }
        }
    }
}

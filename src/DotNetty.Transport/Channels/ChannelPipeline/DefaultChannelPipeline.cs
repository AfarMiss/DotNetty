using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using ReferenceEqualityComparer = DotNetty.Common.Utilities.ReferenceEqualityComparer;

namespace DotNetty.Transport.Channels
{
    public partial class DefaultChannelPipeline : IChannelPipeline
    {
        internal static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<DefaultChannelPipeline>();
        private static readonly NameCachesLocal NameCaches = new NameCachesLocal();
        private static readonly Action<object, object> CallHandlerAddedAction = (self, ctx) => ((DefaultChannelPipeline)self).CallHandlerAdded0((AbstractChannelHandlerContext)ctx);

        private class NameCachesLocal : FastThreadLocal<ConditionalWeakTable<Type, string>>
        {
            protected override ConditionalWeakTable<Type, string> GetInitialValue() => new ConditionalWeakTable<Type, string>();
        }

        private readonly IChannel channel;
        private readonly AbstractChannelHandlerContext head;
        private readonly AbstractChannelHandlerContext tail;
        private Dictionary<IEventExecutorGroup, IEventExecutor> childExecutors;
        private IMessageSizeEstimatorHandle estimatorHandle;

        private PendingHandlerCallback pendingHandlerCallbackHead;
        private bool registered;

        public DefaultChannelPipeline(IChannel channel)
        {
            Contract.Requires(channel != null);

            this.channel = channel;

            this.tail = new TailContext(this);
            this.head = new HeadContext(this);

            this.head.Next = this.tail;
            this.tail.Prev = this.head;
        }

        internal IMessageSizeEstimatorHandle EstimatorHandle => this.estimatorHandle ??= this.channel.Configuration.MessageSizeEstimator.NewHandle();

        public IChannel Channel => this.channel;

        private IEventExecutor GetChildExecutor(IEventExecutorGroup group)
        {
            if (group == null) return null;
            
            var executorMap = this.childExecutors ??= new Dictionary<IEventExecutorGroup, IEventExecutor>(4, ReferenceEqualityComparer.Default);

            if (!executorMap.TryGetValue(group, out var childExecutor))
            {
                childExecutor = group.GetNext();
                executorMap[group] = childExecutor;
            }
            return childExecutor;
        }

        private string GenerateName(IChannelHandler handler)
        {
            var nameCaches = NameCaches.Value;
            var handlerType = handler.GetType();
            var name = nameCaches.GetValue(handlerType, GenerateName0);

            if (this.Context0(name) != null)
            {
                var baseName = name!.Substring(0, name.Length - 1); // Strip the trailing '0'.
                for (int i = 1; ; i++)
                {
                    var newName = baseName + i;
                    if (this.Context0(newName) == null)
                    {
                        name = newName;
                        break;
                    }
                }
            }
            return name;
        }

        private static string GenerateName0(Type handlerType) => StringUtil.SimpleClassName(handlerType) + "#0";

        private void CallHandlerAdded0(AbstractChannelHandlerContext ctx)
        {
            try
            {
                ctx.Handler.HandlerAdded(ctx);
                ctx.SetAdded();
            }
            catch (Exception ex)
            {
                bool removed = false;
                try
                {
                    Remove0(ctx);
                    try
                    {
                        ctx.Handler.HandlerRemoved(ctx);
                    }
                    finally
                    {
                        ctx.SetRemoved();
                    }
                    removed = true;
                }
                catch (Exception ex2)
                {
                    if (Logger.WarnEnabled)
                    {
                        Logger.Warn($"Failed to remove a handler: {ctx.Name}", ex2);
                    }
                }

                if (removed)
                {
                    this.FireExceptionCaught(new ChannelPipelineException($"{ctx.Handler.GetType().Name}.HandlerAdded() has thrown an exception; removed.", ex));
                }
                else
                {
                    this.FireExceptionCaught(new ChannelPipelineException($"{ctx.Handler.GetType().Name}.HandlerAdded() has thrown an exception; also failed to remove.", ex));
                }
            }
        }

        private void CallHandlerRemoved0(AbstractChannelHandlerContext ctx)
        {
            // Notify the complete removal.
            try
            {
                try
                {
                    ctx.Handler.HandlerRemoved(ctx);
                }
                finally
                {
                    ctx.SetRemoved();
                }
            }
            catch (Exception ex)
            {
                this.FireExceptionCaught(new ChannelPipelineException($"{ctx.Handler.GetType().Name}.HandlerRemoved() has thrown an exception.", ex));
            }
        }

        public void FireChannelRegistered()
        {
            AbstractChannelHandlerContext.InvokeChannelRegistered(this.head);
        }

        public void FireChannelUnregistered()
        {
            AbstractChannelHandlerContext.InvokeChannelUnregistered(this.head);
        }

        private void Destroy()
        {
            lock (this)
            {
                this.DestroyUp(this.head.Next, false);
            }
        }

        private void DestroyUp(AbstractChannelHandlerContext ctx, bool inEventLoop)
        {
            var currentThread = Thread.CurrentThread;
            while (true)
            {
                if (ctx == this.tail)
                {
                    this.DestroyDown(currentThread, this.tail.Prev, inEventLoop);
                    break;
                }

                var executor = ctx.Executor;
                if (!inEventLoop && !executor.IsInEventLoop(currentThread))
                {
                    executor.Execute((self, c) => ((DefaultChannelPipeline)self).DestroyUp((AbstractChannelHandlerContext)c, true), this, ctx);
                    break;
                }

                ctx = ctx.Next;
                inEventLoop = false;
            }
        }

        private void DestroyDown(Thread currentThread, AbstractChannelHandlerContext ctx, bool inEventLoop)
        {
            // We have reached at tail; now traverse backwards.
            while (true)
            {
                if (ctx == this.head) break;

                var executor = ctx.Executor;
                if (inEventLoop || executor.IsInEventLoop(currentThread))
                {
                    lock (this)
                    {
                        Remove0(ctx);
                        this.CallHandlerRemoved0(ctx);
                    }
                }
                else
                {
                    executor.Execute((self, c) => ((DefaultChannelPipeline)self).DestroyDown(Thread.CurrentThread, (AbstractChannelHandlerContext)c, true), this, ctx);
                    break;
                }

                ctx = ctx.Prev;
                inEventLoop = false;
            }
        }

        public void FireChannelActive()
        {
            this.head.FireChannelActive();

            if (this.channel.Configuration.AutoRead)
            {
                this.channel.Read();
            }
        }

        public void FireChannelInactive()
        {
            this.head.FireChannelInactive();
        }

        public void FireExceptionCaught(Exception cause)
        {
            this.head.FireExceptionCaught(cause);
        }

        public void FireUserEventTriggered(object evt)
        {
            this.head.FireUserEventTriggered(evt);
        }

        public void FireChannelRead(object msg)
        {
            this.head.FireChannelRead(msg);
        }

        public void FireChannelReadComplete()
        {
            this.head.FireChannelReadComplete();
            if (this.channel.Configuration.AutoRead)
            {
                this.Read();
            }
        }

        public void FireChannelWritabilityChanged()
        {
            this.head.FireChannelWritabilityChanged();
        }

        public Task BindAsync(EndPoint localAddress) => this.tail.BindAsync(localAddress);

        public Task ConnectAsync(EndPoint remoteAddress) => this.tail.ConnectAsync(remoteAddress);

        public Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress) => this.tail.ConnectAsync(remoteAddress, localAddress);

        public Task DisconnectAsync() => this.tail.DisconnectAsync();

        public Task CloseAsync() => this.tail.CloseAsync();

        public Task DeregisterAsync() => this.tail.DeregisterAsync();

        public void Read() => this.tail.Read();

        public Task WriteAsync(object msg) => this.tail.WriteAsync(msg);

        public void Flush() => this.tail.Flush();

        public Task WriteAndFlushAsync(object msg) => this.tail.WriteAndFlushAsync(msg);

        private void CallHandlerAddedForAllHandlers()
        {
            PendingHandlerCallback pendingHandlerCallbackHead;
            lock (this)
            {
                Contract.Assert(!this.registered);

                this.registered = true;
                pendingHandlerCallbackHead = this.pendingHandlerCallbackHead;
                this.pendingHandlerCallbackHead = null;
            }

            // This must happen outside of the synchronized(...) block as otherwise handlerAdded(...) may be called while
            // holding the lock and so produce a deadlock if handlerAdded(...) will try to add another handler from outside
            // the EventLoop.
            var task = pendingHandlerCallbackHead;
            while (task != null)
            {
                task.Execute();
                task = task.Next;
            }
        }

        private void CallHandlerCallbackLater(AbstractChannelHandlerContext ctx, bool added)
        {
            Contract.Assert(!this.registered);

            var task = added ? (PendingHandlerCallback)new PendingHandlerAddedTask(this, ctx) : new PendingHandlerRemovedTask(this, ctx);
            var pending = this.pendingHandlerCallbackHead;
            if (pending == null)
            {
                this.pendingHandlerCallbackHead = task;
            }
            else
            {
                while (pending.Next != null)
                {
                    pending = pending.Next;
                }
                pending.Next = task;
            }
        }

        private IEventExecutor ExecutorSafe(IEventExecutor eventExecutor) => eventExecutor ?? (this.channel.Registered || this.registered ? this.channel.EventLoop : null);

        protected virtual void OnUnhandledInboundException(Exception cause)
        {
            try
            {
                Logger.Warn("Inbound Tail Exception未处理!\n异常{}", cause);
            }
            finally
            {
                ReferenceCountUtil.Release(cause);
            }
        }

        protected virtual void OnUnhandledInboundMessage(object msg)
        {
            try
            {
                Logger.Debug("Inbound Tail Message未处理!\n丢弃{}", msg);
            }
            finally
            {
                ReferenceCountUtil.Release(msg);
            }
        }

        private sealed class TailContext : AbstractChannelHandlerContext, IChannelHandler
        {
            private static readonly string TailName = GenerateName0(typeof(TailContext));
            private static readonly SkipFlags SkipFlags = SkipFlagHelper.GetSkipFlag(typeof(TailContext));

            public TailContext(DefaultChannelPipeline pipeline) : base(pipeline, null, TailName, SkipFlags)
            {
                this.SetAdded();
            }

            public override IChannelHandler Handler => this;

            public void ChannelRegistered(IChannelHandlerContext context)
            {
            }

            public void ChannelUnregistered(IChannelHandlerContext context)
            {
            }

            public void ChannelActive(IChannelHandlerContext context)
            {
            }

            public void ChannelInactive(IChannelHandlerContext context)
            {
            }

            public void ExceptionCaught(IChannelHandlerContext context, Exception exception) => this.pipeline.OnUnhandledInboundException(exception);

            public void ChannelRead(IChannelHandlerContext context, object message) => this.pipeline.OnUnhandledInboundMessage(message);

            public void ChannelReadComplete(IChannelHandlerContext context)
            {
            }

            public void ChannelWritabilityChanged(IChannelHandlerContext context)
            {
            }

            [Skip]
            public void HandlerAdded(IChannelHandlerContext context)
            {
            }

            [Skip]
            public void HandlerRemoved(IChannelHandlerContext context)
            {
            }

            [Skip]
            public Task DeregisterAsync(IChannelHandlerContext context) => context.DeregisterAsync();

            [Skip]
            public Task DisconnectAsync(IChannelHandlerContext context) => context.DisconnectAsync();

            [Skip]
            public Task CloseAsync(IChannelHandlerContext context) => context.CloseAsync();

            [Skip]
            public void Read(IChannelHandlerContext context) => context.Read();

            public void UserEventTriggered(IChannelHandlerContext context, object evt) => ReferenceCountUtil.Release(evt);

            [Skip]
            public Task WriteAsync(IChannelHandlerContext ctx, object message) => ctx.WriteAsync(message);

            [Skip]
            public void Flush(IChannelHandlerContext context) => context.Flush();

            [Skip]
            public Task BindAsync(IChannelHandlerContext context, EndPoint localAddress) => context.BindAsync(localAddress);

            [Skip]
            public Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress) => context.ConnectAsync(remoteAddress, localAddress);
        }

        private sealed class HeadContext : AbstractChannelHandlerContext, IChannelHandler
        {
            private static readonly string HeadName = GenerateName0(typeof(HeadContext));
            private static readonly SkipFlags SkipFlags = SkipFlagHelper.GetSkipFlag(typeof(HeadContext));

            private readonly IChannelUnsafe channelUnsafe;
            private bool firstRegistration = true;

            public HeadContext(DefaultChannelPipeline pipeline) : base(pipeline, null, HeadName, SkipFlags)
            {
                this.channelUnsafe = pipeline.Channel.Unsafe;
                this.SetAdded();
            }

            public override IChannelHandler Handler => this;

            public void Flush(IChannelHandlerContext context) => this.channelUnsafe.Flush();

            public Task BindAsync(IChannelHandlerContext context, EndPoint localAddress) => this.channelUnsafe.BindAsync(localAddress);

            public Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress) => this.channelUnsafe.ConnectAsync(remoteAddress, localAddress);

            public Task DisconnectAsync(IChannelHandlerContext context) => this.channelUnsafe.DisconnectAsync();

            public Task CloseAsync(IChannelHandlerContext context) => this.channelUnsafe.CloseAsync();

            public Task DeregisterAsync(IChannelHandlerContext context) => this.channelUnsafe.DeregisterAsync();

            public void Read(IChannelHandlerContext context) => this.channelUnsafe.BeginRead();

            public Task WriteAsync(IChannelHandlerContext context, object message) => this.channelUnsafe.WriteAsync(message);

            [Skip]
            public void HandlerAdded(IChannelHandlerContext context)
            {
            }

            [Skip]
            public void HandlerRemoved(IChannelHandlerContext context)
            {
            }

            [Skip]
            public void ExceptionCaught(IChannelHandlerContext ctx, Exception exception) => ctx.FireExceptionCaught(exception);

            public void ChannelRegistered(IChannelHandlerContext context)
            {
                if (this.firstRegistration)
                {
                    this.firstRegistration = false;
                    this.pipeline.CallHandlerAddedForAllHandlers();
                }

                context.FireChannelRegistered();
            }

            public void ChannelUnregistered(IChannelHandlerContext context)
            {
                context.FireChannelUnregistered();

                if (!this.pipeline.channel.Open)
                {
                    this.pipeline.Destroy();
                }
            }

            public void ChannelActive(IChannelHandlerContext context)
            {
                context.FireChannelActive();

                this.ReadIfIsAutoRead();
            }

            [Skip]
            public void ChannelInactive(IChannelHandlerContext context) => context.FireChannelInactive();

            [Skip]
            public void ChannelRead(IChannelHandlerContext ctx, object msg) => ctx.FireChannelRead(msg);

            public void ChannelReadComplete(IChannelHandlerContext ctx)
            {
                ctx.FireChannelReadComplete();

                this.ReadIfIsAutoRead();
            }

            private void ReadIfIsAutoRead()
            {
                if (this.pipeline.channel.Configuration.AutoRead)
                {
                    this.pipeline.channel.Read();
                }
            }

            [Skip]
            public void UserEventTriggered(IChannelHandlerContext context, object evt) => this.FireUserEventTriggered(evt);

            [Skip]
            public void ChannelWritabilityChanged(IChannelHandlerContext context) => context.FireChannelWritabilityChanged();
        }

        private abstract class PendingHandlerCallback : IRunnable
        {
            protected readonly DefaultChannelPipeline Pipeline;
            protected readonly AbstractChannelHandlerContext Ctx;
            internal PendingHandlerCallback Next;

            protected PendingHandlerCallback(DefaultChannelPipeline pipeline, AbstractChannelHandlerContext ctx)
            {
                this.Pipeline = pipeline;
                this.Ctx = ctx;
            }

            public abstract void Run();

            internal abstract void Execute();
        }

        private sealed class PendingHandlerAddedTask : PendingHandlerCallback
        {
            public PendingHandlerAddedTask(DefaultChannelPipeline pipeline, AbstractChannelHandlerContext ctx)
                : base(pipeline, ctx)
            {
            }

            public override void Run() => this.Pipeline.CallHandlerAdded0(this.Ctx);

            internal override void Execute()
            {
                var executor = this.Ctx.Executor;
                if (executor.InEventLoop)
                {
                    this.Pipeline.CallHandlerAdded0(this.Ctx);
                }
                else
                {
                    try
                    {
                        executor.Execute(this);
                    }
                    catch (RejectedExecutionException e)
                    {
                        if (Logger.WarnEnabled)
                        {
                            Logger.Warn(
                                "Can't invoke HandlerAdded() as the IEventExecutor {} rejected it, removing handler {}.",
                                executor, this.Ctx.Name, e);
                        }
                        Remove0(this.Ctx);
                        this.Ctx.SetRemoved();
                    }
                }
            }
        }

        private sealed class PendingHandlerRemovedTask : PendingHandlerCallback
        {
            public PendingHandlerRemovedTask(DefaultChannelPipeline pipeline, AbstractChannelHandlerContext ctx)
                : base(pipeline, ctx)
            {
            }

            public override void Run() => this.Pipeline.CallHandlerRemoved0(this.Ctx);

            internal override void Execute()
            {
                var executor = this.Ctx.Executor;
                if (executor.InEventLoop)
                {
                    this.Pipeline.CallHandlerRemoved0(this.Ctx);
                }
                else
                {
                    try
                    {
                        executor.Execute(this);
                    }
                    catch (RejectedExecutionException e)
                    {
                        if (Logger.WarnEnabled)
                        {
                            Logger.Warn(
                                "Can't invoke HandlerRemoved() as the IEventExecutor {} rejected it," +
                                    " removing handler {}.", executor, this.Ctx.Name, e);
                        }
                        // remove0(...) was call before so just call AbstractChannelHandlerContext.setRemoved().
                        this.Ctx.SetRemoved();
                    }
                }
            }
        }
    }
}
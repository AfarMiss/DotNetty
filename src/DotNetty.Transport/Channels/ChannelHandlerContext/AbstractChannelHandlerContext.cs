using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal;
using DotNetty.Common.Utilities;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels
{
    internal abstract class AbstractChannelHandlerContext : IChannelHandlerContext
    {
        internal volatile AbstractChannelHandlerContext Next;
        internal volatile AbstractChannelHandlerContext Prev;
        internal readonly SkipFlags SkipPropagationFlags;

        private enum HandlerState
        {
            Init = 0,
            Added,
            Removed
        }

        internal readonly DefaultChannelPipeline pipeline;
        internal readonly IEventExecutor executor;
        private HandlerState handlerState = HandlerState.Init;

        protected AbstractChannelHandlerContext(DefaultChannelPipeline pipeline, IEventExecutor executor, string name, SkipFlags skipPropagationDirections)
        {
            Contract.Requires(pipeline != null);
            Contract.Requires(name != null);

            this.pipeline = pipeline;
            this.Name = name;
            this.executor = executor;
            this.SkipPropagationFlags = skipPropagationDirections;
        }

        public IChannel Channel => this.pipeline.Channel;

        public IByteBufferAllocator Allocator => this.Channel.Configuration.Allocator;

        public abstract IChannelHandler Handler { get; }

        public bool Added => handlerState == HandlerState.Added;

        public bool Removed => handlerState == HandlerState.Removed;

        internal void SetAdded() => handlerState = HandlerState.Added;

        internal void SetRemoved() => handlerState = HandlerState.Removed;

        public IEventExecutor Executor => this.executor ?? this.Channel.EventLoop;

        public string Name { get; }

        public ConstantMap ConstantMap => this.Channel.ConstantMap;

        private static void InvokeIfInEventLoop<T, T1>(Action<T, T1> executorAction, T next, T1 arg) where T : IChannelHandlerContext
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                executorAction(next, arg);
            }
            else
            {
                nextExecutor.Execute(executorAction, next, arg);
            }
        }

        private void FireIfException<T>(Action<AbstractChannelHandlerContext, T> handlerFireAction, Action<IChannelHandlerContext, T> fireAction, T arg)
        {
            if (this.Added)
            {
                try
                {
                    handlerFireAction(this, arg);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                fireAction(this, arg);
            }
        }

        private static Task InvokeIfInEventLoop<T, T1>(Func<T, T1, Task> executorAction, T next, T1 arg) where T : IChannelHandlerContext
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                return executorAction(next, arg);
            }
            else
            {
                return SafeExecuteOutboundAsync(nextExecutor, () => executorAction(next, arg));
            }
        }

        private Task FireIfException<T>(Func<AbstractChannelHandlerContext, T, Task> handlerFireAction, Func<IChannelHandlerContext, T, Task> fireAction, T arg)
        {
            if (this.Added)
            {
                try
                {
                    return handlerFireAction(this, arg);
                }
                catch (Exception ex)
                {
                    return TaskEx.FromException(ex);
                }
            }
            else
            {
                return fireAction(this, arg);
            }
        }

        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        private sealed class CacheLambda<T>
        {
            private readonly Action<IChannelHandlerContext, T> action1;
            private readonly Action<IChannelHandlerContext, T> action2;
            internal readonly Action<AbstractChannelHandlerContext, T> InvokeAction;

            public CacheLambda(Action<IChannelHandlerContext, T> action1, Action<IChannelHandlerContext, T> action2)
            {
                this.action1 = action1;
                this.action2 = action2;
                this.InvokeAction = (context, arg) => context.FireIfException(this.action1, this.action2, arg);
            }
        }

        [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
        private sealed class CacheTaskLambda<T>
        {
            private readonly Func<AbstractChannelHandlerContext, T, Task> action1;
            private readonly Func<IChannelHandlerContext, T, Task> action2;
            internal readonly Func<AbstractChannelHandlerContext, T, Task> InvokeAction;

            public CacheTaskLambda(Func<AbstractChannelHandlerContext, T, Task> action1, Func<IChannelHandlerContext, T, Task> action2, Action<AbstractChannelHandlerContext, T> action3 = null)
            {
                this.action1 = action1;
                this.action2 = action2;
                if (action3 == null)
                {
                    this.InvokeAction = (context, arg) => context.FireIfException(this.action1, this.action2, arg);
                }
                else
                {
                    this.InvokeAction = (context, arg) =>
                    {
                        action3(context, arg);
                        return context.FireIfException(this.action1, this.action2, arg);
                    };
                }
            }
        }

        public void FireChannelRegistered() => InvokeChannelRegistered(this.FindContextInbound());

        private static readonly CacheLambda<object> ChannelRegistered = new CacheLambda<object>
        (
            (context, _) => context.Handler.ChannelRegistered(context),
            (context, _) => context.FireChannelRegistered()
        );
        internal static void InvokeChannelRegistered(AbstractChannelHandlerContext next)
        {
            InvokeIfInEventLoop(ChannelRegistered.InvokeAction, next, default);
        }

        public void FireChannelUnregistered() => InvokeChannelUnregistered(this.FindContextInbound());

        private static readonly CacheLambda<object> ChannelUnregistered = new CacheLambda<object>
        (
            (context, _) => context.Handler.ChannelUnregistered(context),
            (context, _) => context.FireChannelUnregistered()
        );
        internal static void InvokeChannelUnregistered(AbstractChannelHandlerContext next)
        {
            InvokeIfInEventLoop(ChannelUnregistered.InvokeAction, next, default);
        }

        public void FireChannelActive() => InvokeChannelActive(this.FindContextInbound());

        private static readonly CacheLambda<object> ChannelActive = new CacheLambda<object>
        (
            (context, _) => context.Handler.ChannelActive(context),
            (context, _) => context.FireChannelActive()
        );
        internal static void InvokeChannelActive(AbstractChannelHandlerContext next)
        {
            InvokeIfInEventLoop(ChannelActive.InvokeAction, next, default);
        }

        public void FireChannelInactive() => InvokeChannelInactive(this.FindContextInbound());

        private static readonly CacheLambda<object> ChannelInactive = new CacheLambda<object>
        (
            (context, _) => context.Handler.ChannelInactive(context),
            (context, _) => context.FireChannelInactive()
        );
        internal static void InvokeChannelInactive(AbstractChannelHandlerContext next)
        {
            InvokeIfInEventLoop(ChannelInactive.InvokeAction, next, default);
        }

        public virtual void FireExceptionCaught(Exception cause)
        {
            InvokeExceptionCaught(this.FindContextInbound(), cause);
        }

        private static readonly CacheLambda<Exception> ExceptionCaught = new CacheLambda<Exception>
        (
            (context, arg) => context.Handler.ExceptionCaught(context, arg),
            (context, arg) => context.FireExceptionCaught(arg)
        );
        internal static void InvokeExceptionCaught(AbstractChannelHandlerContext next, Exception cause)
        {
            InvokeIfInEventLoop(ExceptionCaught.InvokeAction, next, cause);
        }

        public void FireUserEventTriggered(object evt) => InvokeUserEventTriggered(this.FindContextInbound(), evt);

        private static readonly CacheLambda<object> UserEventTriggered = new CacheLambda<object>
        (
            (context, arg) => context.Handler.UserEventTriggered(context, arg),
            (context, arg) => context.FireUserEventTriggered(arg)
        );
        internal static void InvokeUserEventTriggered(AbstractChannelHandlerContext next, object evt)
        {
            InvokeIfInEventLoop(UserEventTriggered.InvokeAction, next, evt);
        }

        public void FireChannelRead(object msg) => InvokeChannelRead(this.FindContextInbound(), msg);

        private static readonly CacheLambda<object> ChannelRead = new CacheLambda<object>
        (
            (context, arg) => context.Handler.ChannelRead(context, arg),
            (context, arg) => context.FireChannelRead(arg)
        );
        internal static void InvokeChannelRead(AbstractChannelHandlerContext next, object msg)
        {
            InvokeIfInEventLoop(ChannelRead.InvokeAction, next, msg);
        }

        public void FireChannelReadComplete() => InvokeChannelReadComplete(this.FindContextInbound());

        private static readonly CacheLambda<object> ChannelReadComplete = new CacheLambda<object>
        (
            (context, _) => context.Handler.ChannelReadComplete(context),
            (context, _) => context.FireChannelReadComplete()
        );
        internal static void InvokeChannelReadComplete(AbstractChannelHandlerContext next)
        {
            InvokeIfInEventLoop(ChannelReadComplete.InvokeAction, next, default);
        }

        public void FireChannelWritabilityChanged() => InvokeChannelWritabilityChanged(this.FindContextInbound());

        private static readonly CacheLambda<object> ChannelWritabilityChanged = new CacheLambda<object>
        (
            (context, _) => context.Handler.ChannelWritabilityChanged(context),
            (context, arg) => context.FireChannelWritabilityChanged()
        );
        internal static void InvokeChannelWritabilityChanged(AbstractChannelHandlerContext next)
        {
            InvokeIfInEventLoop(ChannelWritabilityChanged.InvokeAction, next, default);
        }

        private static readonly CacheTaskLambda<EndPoint> CacheBindAsync = new CacheTaskLambda<EndPoint>
        (
            (context, arg) => context.Handler.BindAsync(context, arg),
            (context, arg) => context.BindAsync(arg)
        );
        public Task BindAsync(EndPoint localAddress)
        {
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(CacheBindAsync.InvokeAction, next, localAddress);
        }

        public Task ConnectAsync(EndPoint remoteAddress) => this.ConnectAsync(remoteAddress, null);

        private static readonly CacheTaskLambda<(EndPoint, EndPoint)> CacheConnectAsync = new CacheTaskLambda<(EndPoint, EndPoint)>
        (
            (context, arg) => context.Handler.ConnectAsync(context, arg.Item1, arg.Item2),
            (context, arg) => context.ConnectAsync(arg.Item1, arg.Item2)
        );
        public Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(CacheConnectAsync.InvokeAction, next, (remoteAddress, localAddress));
        }

        private static readonly CacheTaskLambda<object> CacheDisconnectAsync = new CacheTaskLambda<object>
        (
            (context, _) => context.Handler.DisconnectAsync(context),
            (context, arg) => context.DisconnectAsync()
        );
        public Task DisconnectAsync()
        {
            if (!this.Channel.Metadata.HasDisconnect) return this.CloseAsync();

            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(CacheDisconnectAsync.InvokeAction, next, default);
        }

        private static readonly CacheTaskLambda<object> CacheCloseAsync = new CacheTaskLambda<object>
        (
            (context, _) => context.Handler.CloseAsync(context),
            (context, arg) => context.CloseAsync()
        );
        public Task CloseAsync()
        {
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(CacheCloseAsync.InvokeAction, next, default);
        }

        private static readonly CacheTaskLambda<object> CacheDeregisterAsync = new CacheTaskLambda<object>
        (
            (context, _) => context.Handler.DeregisterAsync(context),
            (context, arg) => context.DeregisterAsync()
        );
        public Task DeregisterAsync()
        {
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(CacheDeregisterAsync.InvokeAction, next, default);
        }

        private static readonly CacheLambda<object> CacheRead = new CacheLambda<object>
        (
            (context, _) => context.Handler.Read(context),
            (context, arg) => context.Read()
        );
        public void Read()
        {
            var next = this.FindContextOutbound();
            InvokeIfInEventLoop(CacheRead.InvokeAction, next, default);
        }

        private static readonly CacheTaskLambda<(object, int)> CacheWriteAsync = new CacheTaskLambda<(object, int)>
        (
            (context, arg) => context.Handler.WriteAsync(context, arg.Item1),
            (context, arg) => context.WriteAsync(arg.Item1),
            (context, arg) => DecrementPendingOutboundByte(context, arg.Item2)
        );
        public Task WriteAsync(object msg)
        {
            var next = this.FindContextOutbound();
            var size = !next.Executor.InEventLoop ? IncrementPendingOutboundByte(next, msg) : 0;

            return InvokeIfInEventLoop(CacheWriteAsync.InvokeAction, next, (msg, size));
        }

        public void Flush() => InvokeChannelFlush(this.FindContextOutbound());

        private static readonly CacheLambda<object> CacheFlush = new CacheLambda<object>
        (
            (context, _) => context.Handler.Flush(context),
            (context, arg) => context.Flush()
        );
        internal static void InvokeChannelFlush(AbstractChannelHandlerContext next)
        {
            InvokeIfInEventLoop(CacheFlush.InvokeAction, next, default);
        }

        private static readonly CacheTaskLambda<(object, int)> CacheWriteAndFlushAsync = new CacheTaskLambda<(object, int)>
        (
            (context, arg) =>
            {
                var task = context.Handler.WriteAsync(context, arg.Item1);
                InvokeChannelFlush(context);
                return task;
            },
            (context, arg) => context.WriteAndFlushAsync(arg.Item1),
            (context, arg) => DecrementPendingOutboundByte(context, arg.Item2)
        );
        public Task WriteAndFlushAsync(object message)
        {
            var next = this.FindContextOutbound();
            var size = !next.Executor.InEventLoop ? IncrementPendingOutboundByte(next, message) : 0;
            
            return InvokeIfInEventLoop(CacheWriteAndFlushAsync.InvokeAction, next, (message, size));
        }

        private void NotifyHandlerException(Exception cause)
        {
            if (InExceptionCaught(cause))
            {
                if (DefaultChannelPipeline.Logger.WarnEnabled)
                {
                    DefaultChannelPipeline.Logger.Warn("Handler Exception", cause);
                }

                return;
            }

            InvokeExceptionCaught(this, cause);
        }

        private static bool InExceptionCaught(Exception cause) => cause.StackTrace.IndexOf("." + nameof(IChannelHandler.ExceptionCaught) + "(", StringComparison.Ordinal) >= 0;

        private AbstractChannelHandlerContext FindContextInbound()
        {
            var context = this;
            do
            {
                context = context.Next;
            } while ((context.SkipPropagationFlags & SkipFlags.Inbound) == SkipFlags.Inbound);

            return context;
        }

        private AbstractChannelHandlerContext FindContextOutbound()
        {
            var context = this;
            do
            {
                context = context.Prev;
            } while ((context.SkipPropagationFlags & SkipFlags.Outbound) == SkipFlags.Outbound);

            return context;
        }

        private static Task SafeExecuteOutboundAsync(IEventExecutor executor, Func<Task> function)
        {
            var promise = new TaskCompletionSource();
            try
            {
                executor.Execute((tcs, func) => func().LinkOutcome(tcs), promise, function);
            }
            catch (Exception cause)
            {
                promise.TrySetException(cause);
            }

            return promise.Task;
        }

        private static int IncrementPendingOutboundByte(AbstractChannelHandlerContext ctx, object msg)
        {
            var size = 0;
            if (AbstractWriteTask.EstimateTaskSizeOnSubmit)
            {
                var buffer = ctx.Channel.Unsafe.OutboundBuffer;
                if (buffer != null)
                {
                    size = ctx.pipeline.EstimatorHandle.Size(msg) + AbstractWriteTask.WriteTaskOverhead;
                    buffer.IncrementPendingOutboundBytes(size);
                }
            }

            return size;
        }

        private static void DecrementPendingOutboundByte(AbstractChannelHandlerContext ctx, int size)
        {
            if (size != 0 && AbstractWriteTask.EstimateTaskSizeOnSubmit)
            {
                var buffer = ctx.Channel.Unsafe.OutboundBuffer;
                buffer?.DecrementPendingOutboundBytes(size);
            }
        }

        private abstract class AbstractWriteTask
        {
            protected internal static readonly bool EstimateTaskSizeOnSubmit = SystemPropertyUtil.GetBoolean("io.netty.transport.estimateSizeOnSubmit", true);

            // Assuming a 64-bit .NET VM, 16 bytes object header, 4 reference fields and 2 int field
            protected internal static readonly int WriteTaskOverhead = SystemPropertyUtil.GetInt("io.netty.transport.writeTaskSizeOverhead", 56);
        }
    }
}
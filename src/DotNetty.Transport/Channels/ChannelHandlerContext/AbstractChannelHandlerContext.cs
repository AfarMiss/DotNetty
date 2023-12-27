using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common;
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
        
        private static void InvokeIfInEventLoop<T>(Action<AbstractChannelHandlerContext, T> executorAction, AbstractChannelHandlerContext next, T arg)
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                executorAction(next, arg);
            }
            else
            {
                nextExecutor.Execute((ctx, obj) => executorAction((AbstractChannelHandlerContext)ctx, (T)obj), next, arg);
            }
        }
        
        private void FireIfException<T>(Action<IChannelHandlerContext, T> handlerFireAction, Action<IChannelHandlerContext, T> fireAction, T arg)
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
        
        private static Task InvokeIfInEventLoop<T>(Func<AbstractChannelHandlerContext, T, Task> executorAction, AbstractChannelHandlerContext next, T arg)
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
                    return ComposeExceptionTask(ex);
                }
            }
            else
            {
                return fireAction(this, arg);
            }
        }

        public void FireChannelRegistered() => InvokeChannelRegistered(this.FindContextInbound());

        internal static void InvokeChannelRegistered(AbstractChannelHandlerContext next)
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.ChannelRegistered(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireChannelRegistered();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }

        public void FireChannelUnregistered() => InvokeChannelUnregistered(this.FindContextInbound());

        internal static void InvokeChannelUnregistered(AbstractChannelHandlerContext next)
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.ChannelUnregistered(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireChannelUnregistered();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }

        public void FireChannelActive() => InvokeChannelActive(this.FindContextInbound());

        internal static void InvokeChannelActive(AbstractChannelHandlerContext next)
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.ChannelActive(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireChannelActive();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }

        public void FireChannelInactive() => InvokeChannelInactive(this.FindContextInbound());

        internal static void InvokeChannelInactive(AbstractChannelHandlerContext next)
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.ChannelInactive(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireChannelInactive();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }

        public virtual void FireExceptionCaught(Exception cause) => InvokeExceptionCaught(this.FindContextInbound(), cause);

        internal static void InvokeExceptionCaught(AbstractChannelHandlerContext next, Exception cause)
        {
            Contract.Requires(cause != null);

            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeExceptionCaught(cause);
            }
            else
            {
                try
                {
                    nextExecutor.Execute((c, e) => ((AbstractChannelHandlerContext)c).InvokeExceptionCaught((Exception)e), next, cause);
                }
                catch (Exception t)
                {
                    if (DefaultChannelPipeline.Logger.WarnEnabled)
                    {
                        DefaultChannelPipeline.Logger.Warn("Failed to submit an ExceptionCaught() event.", t);
                        DefaultChannelPipeline.Logger.Warn("The ExceptionCaught() event that was failed to submit was:", cause);
                    }
                }
            }
        }

        private void InvokeExceptionCaught(Exception cause)
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.ExceptionCaught(this, cause);
                }
                catch (Exception t)
                {
                    if (DefaultChannelPipeline.Logger.WarnEnabled)
                    {
                        DefaultChannelPipeline.Logger.Warn("Failed to submit an ExceptionCaught() event.", t);
                        DefaultChannelPipeline.Logger.Warn("An exception was thrown by a user handler's " + "ExceptionCaught() method while handling the following exception:", cause);
                    }
                }
            }
            else
            {
                this.FireExceptionCaught(cause);
            }
        }

        public void FireUserEventTriggered(object evt) => InvokeUserEventTriggered(this.FindContextInbound(), evt);

        internal static void InvokeUserEventTriggered(AbstractChannelHandlerContext next, object evt)
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.UserEventTriggered(context, arg);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireUserEventTriggered(arg);
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, evt);
        }

        public void FireChannelRead(object msg) => InvokeChannelRead(this.FindContextInbound(), msg);

        internal static void InvokeChannelRead(AbstractChannelHandlerContext next, object msg)
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.ChannelRead(context, arg);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireChannelRead(arg);
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, msg);
        }

        public void FireChannelReadComplete() => InvokeChannelReadComplete(this.FindContextInbound());

        internal static void InvokeChannelReadComplete(AbstractChannelHandlerContext next) 
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.ChannelReadComplete(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireChannelReadComplete();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }
        
        public void FireChannelWritabilityChanged() => InvokeChannelWritabilityChanged(this.FindContextInbound());

        internal static void InvokeChannelWritabilityChanged(AbstractChannelHandlerContext next)
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.ChannelWritabilityChanged(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.FireChannelWritabilityChanged();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }
        
        public Task BindAsync(EndPoint localAddress)
        {
            static Task FireAction(IChannelHandlerContext context, object arg) => context.Handler.BindAsync(context, (EndPoint)arg);
            static Task NextFireAction(IChannelHandlerContext context, object arg) => context.BindAsync((EndPoint)arg);
            static Task InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(InvokeAction, next, localAddress);
        }
        
        public Task ConnectAsync(EndPoint remoteAddress) => this.ConnectAsync(remoteAddress, null);

        public Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            static Task FireAction(IChannelHandlerContext context, (EndPoint, EndPoint) arg) => context.Handler.ConnectAsync(context, arg.Item1, arg.Item2);
            static Task NextFireAction(IChannelHandlerContext context, (EndPoint, EndPoint) arg) => context.ConnectAsync(arg.Item1, arg.Item2);
            static Task InvokeAction(AbstractChannelHandlerContext next, (EndPoint, EndPoint) arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(InvokeAction, next, (remoteAddress, localAddress));
        }
        
        public Task DisconnectAsync()
        {
            if (!this.Channel.Metadata.HasDisconnect) return this.CloseAsync();

            static Task FireAction(IChannelHandlerContext context, object arg) => context.Handler.DisconnectAsync(context);
            static Task NextFireAction(IChannelHandlerContext context, object arg) => context.DisconnectAsync();
            static Task InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(InvokeAction, next, default(object));
        }
        
        public Task CloseAsync()
        {
            static Task FireAction(IChannelHandlerContext context, object arg) => context.Handler.CloseAsync(context);
            static Task NextFireAction(IChannelHandlerContext context, object arg) => context.CloseAsync();
            static Task InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(InvokeAction, next, default(object));
        }
        
        public Task DeregisterAsync()
        {
            static Task FireAction(IChannelHandlerContext context, object arg) => context.Handler.DeregisterAsync(context);
            static Task NextFireAction(IChannelHandlerContext context, object arg) => context.DeregisterAsync();
            static Task InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            var next = this.FindContextOutbound();
            return InvokeIfInEventLoop(InvokeAction, next, default(object));
        }
        
        public void Read()
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.Read(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.Read();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            var next = this.FindContextOutbound();
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }
        
        public Task WriteAsync(object msg)
        {
            Contract.Requires(msg != null);
            // todo: check for cancellation
            // return this.WriteAsync(msg, false);
            var outbound = this.FindContextOutbound();
            var eventExecutor = outbound.Executor;
            if (eventExecutor.InEventLoop)
            {
                return outbound.InvokeWriteAsync(msg);
            }

            var promise = new TaskCompletionSource();
            var task = WriteTask.Create(outbound, msg, promise);
            SafeExecuteOutbound(eventExecutor, task, promise, msg);
            return promise.Task;
        }

        private Task InvokeWriteAsync(object msg)
        {
            if (this.Added)
                return this.InvokeWriteAsync0(msg);
            else
                return this.WriteAsync(msg);
        }

        private Task InvokeWriteAsync0(object msg)
        {
            try
            {
                return this.Handler.WriteAsync(this, msg);
            }
            catch (Exception ex)
            {
                return ComposeExceptionTask(ex);
            }
        }

        public void Flush()
        {
            static void FireAction(IChannelHandlerContext context, object arg) => context.Handler.Flush(context);
            static void NextFireAction(IChannelHandlerContext context, object arg) => context.Flush();
            static void InvokeAction(AbstractChannelHandlerContext next, object arg) => next.FireIfException(FireAction, NextFireAction, arg);
            
            var next = this.FindContextOutbound();
            InvokeIfInEventLoop(InvokeAction, next, default(object));
        }
        
        public Task WriteAndFlushAsync(object message)
        {
            Contract.Requires(message != null);
            // todo: check for cancellation

            // return this.WriteAsync(message, true);
            var outbound = this.FindContextOutbound();
            var eventExecutor = outbound.Executor;
            if (eventExecutor.InEventLoop)
            {
                return outbound.InvokeWriteAndFlushAsync(message);
            }

            var promise = new TaskCompletionSource();
            var task = WriteAndFlushTask.Create(outbound, message, promise) ;
            SafeExecuteOutbound(eventExecutor, task, promise, message);
            return promise.Task;
        }

        private Task InvokeWriteAndFlushAsync(object msg)
        {
            if (this.Added)
            {
                var task = this.InvokeWriteAsync0(msg);
                // this.InvokeFlush0();
                try
                {
                    this.Handler.Flush(this);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }

                return task;
            }
            else
            {
                return this.WriteAndFlushAsync(msg);
            }
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

            this.InvokeExceptionCaught(cause);
        }

        private static Task ComposeExceptionTask(Exception cause) => TaskEx.FromException(cause);

        private static bool InExceptionCaught(Exception cause) => cause.StackTrace.IndexOf("." + nameof(IChannelHandler.ExceptionCaught) + "(", StringComparison.Ordinal) >= 0;

        private AbstractChannelHandlerContext FindContextInbound()
        {
            var context = this;
            do
            {
                context = context.Next;
            }
            while ((context.SkipPropagationFlags & SkipFlags.Inbound) == SkipFlags.Inbound);
            return context;
        }

        private AbstractChannelHandlerContext FindContextOutbound()
        {
            var context = this;
            do
            {
                context = context.Prev;
            }
            while ((context.SkipPropagationFlags & SkipFlags.Outbound) == SkipFlags.Outbound);
            return context;
        }

        private static Task SafeExecuteOutboundAsync(IEventExecutor executor, Func<Task> function)
        {
            var promise = new TaskCompletionSource();
            try
            {
                executor.Execute((p, func) => ((Func<Task>)func)().LinkOutcome((TaskCompletionSource)p), promise, function);
            }
            catch (Exception cause)
            {
                promise.TrySetException(cause);
            }
            return promise.Task;
        }

        private static void SafeExecuteOutbound(IEventExecutor executor, IRunnable task, TaskCompletionSource promise, object msg)
        {
            try
            {
                executor.Execute(task);
            }
            catch (Exception cause)
            {
                try
                {
                    promise.TrySetException(cause);
                }
                finally
                {
                    ReferenceCountUtil.Release(msg);
                }
            }
        }

        private static int PendingOutboundByte(AbstractChannelHandlerContext ctx, object msg)
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
        
        private abstract class AbstractWriteTask
        {
            protected internal static readonly bool EstimateTaskSizeOnSubmit = SystemPropertyUtil.GetBoolean("io.netty.transport.estimateSizeOnSubmit", true);
            // Assuming a 64-bit .NET VM, 16 bytes object header, 4 reference fields and 2 int field
            protected internal static readonly int WriteTaskOverhead = SystemPropertyUtil.GetInt("io.netty.transport.writeTaskSizeOverhead", 56);
        }
        
        private abstract class AbstractWriteTask<T> : AbstractWriteTask, IRunnable , IRecycle where T : AbstractWriteTask<T>, new()
        {
            private static readonly ThreadLocalPool<T> Pool = new ThreadLocalPool<T>(() => new T());
            
            private AbstractChannelHandlerContext ctx;
            private TaskCompletionSource promise;
            private IRecycleHandle<T> handle;
            private object msg;
            private int size;
            
            protected abstract Task WriteAsync(AbstractChannelHandlerContext ctx, object msg);

            void IRecycle.Recycle()
            {
                this.ctx = null;
                this.msg = null;
                this.promise = null;
            }

            public static T Create(AbstractChannelHandlerContext ctx, object msg, TaskCompletionSource promise)
            {
                var task = Pool.Acquire(out var handle);
                task.ctx = ctx;
                task.msg = msg;
                task.promise = promise;
                task.handle = handle;
                
                if (EstimateTaskSizeOnSubmit)
                {
                    var buffer = ctx.Channel.Unsafe.OutboundBuffer;
                    if (buffer != null)
                    {
                        task.size = ctx.pipeline.EstimatorHandle.Size(msg) + WriteTaskOverhead;
                        buffer.IncrementPendingOutboundBytes(task.size);
                    }
                    else
                    {
                        task.size = 0;
                    }
                }
                else
                {
                    task.size = 0;
                }
                return task;
            }
            
            void IRunnable.Run()
            {
                try
                {
                    if (EstimateTaskSizeOnSubmit)
                    {
                        var buffer = this.ctx.Channel.Unsafe.OutboundBuffer;
                        buffer?.DecrementPendingOutboundBytes(this.size);
                    }
                    this.WriteAsync(this.ctx, this.msg).LinkOutcome(this.promise);
                }
                finally
                {
                    Pool.Recycle(this.handle);
                }
            }
        }

        private sealed class WriteTask : AbstractWriteTask<WriteTask>
        {
            protected override Task WriteAsync(AbstractChannelHandlerContext ctx, object msg) => ctx.InvokeWriteAsync(msg);
        }

        private sealed class WriteAndFlushTask : AbstractWriteTask<WriteAndFlushTask>
        {
            protected override Task WriteAsync(AbstractChannelHandlerContext ctx, object msg) => ctx.InvokeWriteAndFlushAsync(msg);
        }
    }
}
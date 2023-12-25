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
        private static readonly Action<object> InvokeChannelReadCompleteAction = ctx => ((AbstractChannelHandlerContext)ctx).InvokeChannelReadComplete();
        private static readonly Action<object> InvokeReadAction = ctx => ((AbstractChannelHandlerContext)ctx).InvokeRead();
        private static readonly Action<object> InvokeChannelWritabilityChangedAction = ctx => ((AbstractChannelHandlerContext)ctx).InvokeChannelWritabilityChanged();
        private static readonly Action<object> InvokeFlushAction = ctx => ((AbstractChannelHandlerContext)ctx).InvokeFlush();
        private static readonly Action<object, object> InvokeUserEventTriggeredAction = (ctx, evt) => ((AbstractChannelHandlerContext)ctx).InvokeUserEventTriggered(evt);
        private static readonly Action<object, object> InvokeChannelReadAction = (ctx, msg) => ((AbstractChannelHandlerContext)ctx).InvokeChannelRead(msg);
        
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

        public void FireChannelRegistered() => InvokeChannelRegistered(this.FindContextInbound());

        internal static void InvokeChannelRegistered(AbstractChannelHandlerContext next)
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeChannelRegistered();
            }
            else
            {
                nextExecutor.Execute(c => ((AbstractChannelHandlerContext)c).InvokeChannelRegistered(), next);
            }
        }

        private void InvokeChannelRegistered()
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.ChannelRegistered(this);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.FireChannelRegistered();
            }
        }

        public void FireChannelUnregistered() => InvokeChannelUnregistered(this.FindContextInbound());

        internal static void InvokeChannelUnregistered(AbstractChannelHandlerContext next)
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeChannelUnregistered();
            }
            else
            {
                nextExecutor.Execute(c => ((AbstractChannelHandlerContext)c).InvokeChannelUnregistered(), next);
            }
        }

        private void InvokeChannelUnregistered()
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.ChannelUnregistered(this);
                }
                catch (Exception t)
                {
                    this.NotifyHandlerException(t);
                }
            }
            else
            {
                this.FireChannelUnregistered();
            }
        }

        public void FireChannelActive() => InvokeChannelActive(this.FindContextInbound());

        internal static void InvokeChannelActive(AbstractChannelHandlerContext next)
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeChannelActive();
            }
            else
            {
                nextExecutor.Execute(c => ((AbstractChannelHandlerContext)c).InvokeChannelActive(), next);
            }
        }

        private void InvokeChannelActive()
        {
            if (this.Added)
            {
                try
                {
                    (this.Handler).ChannelActive(this);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.FireChannelActive();
            }
        }

        public void FireChannelInactive() => InvokeChannelInactive(this.FindContextInbound());

        internal static void InvokeChannelInactive(AbstractChannelHandlerContext next)
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeChannelInactive();
            }
            else
            {
                nextExecutor.Execute(c => ((AbstractChannelHandlerContext)c).InvokeChannelInactive(), next);
            }
        }

        private void InvokeChannelInactive()
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.ChannelInactive(this);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.FireChannelInactive();
            }
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
                        DefaultChannelPipeline.Logger.Warn(
                                "An exception was thrown by a user handler's " +
                                        "ExceptionCaught() method while handling the following exception:", cause);
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
            Contract.Requires(evt != null);
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeUserEventTriggered(evt);
            }
            else
            {
                nextExecutor.Execute(InvokeUserEventTriggeredAction, next, evt);
            }
        }

        private void InvokeUserEventTriggered(object evt)
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.UserEventTriggered(this, evt);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.FireUserEventTriggered(evt);
            }
        }

        public void FireChannelRead(object msg) => InvokeChannelRead(this.FindContextInbound(), msg);

        internal static void InvokeChannelRead(AbstractChannelHandlerContext next, object msg)
        {
            Contract.Requires(msg != null);

            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeChannelRead(msg);
            }
            else
            {
                nextExecutor.Execute(InvokeChannelReadAction, next, msg);
            }
        }

        private void InvokeChannelRead(object msg)
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.ChannelRead(this, msg);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.FireChannelRead(msg);
            }
        }

        public void FireChannelReadComplete() => InvokeChannelReadComplete(this.FindContextInbound());

        internal static void InvokeChannelReadComplete(AbstractChannelHandlerContext next) 
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeChannelReadComplete();
            }
            else
            {
                // todo: consider caching task
                nextExecutor.Execute(InvokeChannelReadCompleteAction, next);
            }
        }

        private void InvokeChannelReadComplete()
        {
            if (this.Added)
            {
                try
                {
                    (this.Handler).ChannelReadComplete(this);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.FireChannelReadComplete();
            }
        }

        public void FireChannelWritabilityChanged() => InvokeChannelWritabilityChanged(this.FindContextInbound());

        internal static void InvokeChannelWritabilityChanged(AbstractChannelHandlerContext next)
        {
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeChannelWritabilityChanged();
            }
            else
            {
                // todo: consider caching task
                nextExecutor.Execute(InvokeChannelWritabilityChangedAction, next);
            }
        }

        private void InvokeChannelWritabilityChanged()
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.ChannelWritabilityChanged(this);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.FireChannelWritabilityChanged();
            }
        }

        public Task BindAsync(EndPoint localAddress)
        {
            Contract.Requires(localAddress != null);

            var next = this.FindContextOutbound();
            var nextExecutor = next.Executor;
            return nextExecutor.InEventLoop ? next.InvokeBindAsync(localAddress) : SafeExecuteOutboundAsync(nextExecutor, () => next.InvokeBindAsync(localAddress));
        }

        private Task InvokeBindAsync(EndPoint localAddress)
        {
            if (!this.Added) return this.BindAsync(localAddress);
            
            try
            {
                return this.Handler.BindAsync(this, localAddress);
            }
            catch (Exception ex)
            {
                return ComposeExceptionTask(ex);
            }
        }

        public Task ConnectAsync(EndPoint remoteAddress) => this.ConnectAsync(remoteAddress, null);

        public Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            var next = this.FindContextOutbound();
            Contract.Requires(remoteAddress != null);
            // todo: check for cancellation

            var nextExecutor = next.Executor;
            return nextExecutor.InEventLoop ? next.InvokeConnectAsync(remoteAddress, localAddress) : SafeExecuteOutboundAsync(nextExecutor, () => next.InvokeConnectAsync(remoteAddress, localAddress));
        }

        private Task InvokeConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            if (!this.Added) return this.ConnectAsync(remoteAddress, localAddress);
            
            try
            {
                return this.Handler.ConnectAsync(this, remoteAddress, localAddress);
            }
            catch (Exception ex)
            {
                return ComposeExceptionTask(ex);
            }
        }

        public Task DisconnectAsync()
        {
            if (!this.Channel.Metadata.HasDisconnect)
            {
                return this.CloseAsync();
            }

            // todo: check for cancellation
            var next = this.FindContextOutbound();
            var nextExecutor = next.Executor;
            return nextExecutor.InEventLoop ? next.InvokeDisconnectAsync() : SafeExecuteOutboundAsync(nextExecutor, () => next.InvokeDisconnectAsync());
        }

        private Task InvokeDisconnectAsync()
        {
            if (!this.Added) return this.DisconnectAsync();
            
            try
            {
                return this.Handler.DisconnectAsync(this);
            }
            catch (Exception ex)
            {
                return ComposeExceptionTask(ex);
            }
        }

        public Task CloseAsync()
        {
            // todo: check for cancellation
            var next = this.FindContextOutbound();
            var nextExecutor = next.Executor;
            return nextExecutor.InEventLoop ? next.InvokeCloseAsync() : SafeExecuteOutboundAsync(nextExecutor, () => next.InvokeCloseAsync());
        }

        private Task InvokeCloseAsync()
        {
            if (!this.Added) return this.CloseAsync();
            
            try
            {
                return this.Handler.CloseAsync(this);
            }
            catch (Exception ex)
            {
                return ComposeExceptionTask(ex);
            }
        }

        public Task DeregisterAsync()
        {
            // todo: check for cancellation
            var next = this.FindContextOutbound();
            var nextExecutor = next.Executor;
            return nextExecutor.InEventLoop ? next.InvokeDeregisterAsync() : SafeExecuteOutboundAsync(nextExecutor, () => next.InvokeDeregisterAsync());
        }

        private Task InvokeDeregisterAsync()
        {
            if (!this.Added) return this.DeregisterAsync();
            
            try
            {
                return this.Handler.DeregisterAsync(this);
            }
            catch (Exception ex)
            {
                return ComposeExceptionTask(ex);
            }
        }

        public void Read()
        {
            var next = this.FindContextOutbound();
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeRead();
            }
            else
            {
                // todo: consider caching task
                nextExecutor.Execute(InvokeReadAction, next);
            }
        }

        private void InvokeRead()
        {
            if (this.Added)
            {
                try
                {
                    this.Handler.Read(this);
                }
                catch (Exception ex)
                {
                    this.NotifyHandlerException(ex);
                }
            }
            else
            {
                this.Read();
            }
        }

        public Task WriteAsync(object msg)
        {
            Contract.Requires(msg != null);
            // todo: check for cancellation
            return this.WriteAsync(msg, false);
        }

        private Task InvokeWriteAsync(object msg) => this.Added ? this.InvokeWriteAsync0(msg) : this.WriteAsync(msg);

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
            var next = this.FindContextOutbound();
            var nextExecutor = next.Executor;
            if (nextExecutor.InEventLoop)
            {
                next.InvokeFlush();
            }
            else
            {
                nextExecutor.Execute(InvokeFlushAction, next);
            }
        }

        private void InvokeFlush()
        {
            if (this.Added)
            {
                this.InvokeFlush0();
            }
            else
            {
                this.Flush();
            }
        }

        private void InvokeFlush0()
        {
            try
            {
                this.Handler.Flush(this);
            }
            catch (Exception ex)
            {
                this.NotifyHandlerException(ex);
            }
        }

        public Task WriteAndFlushAsync(object message)
        {
            Contract.Requires(message != null);
            // todo: check for cancellation

            return this.WriteAsync(message, true);
        }

        private Task InvokeWriteAndFlushAsync(object msg)
        {
            if (!this.Added) return this.WriteAndFlushAsync(msg);
            
            var task = this.InvokeWriteAsync0(msg);
            this.InvokeFlush0();
            return task;
        }

        private Task WriteAsync(object msg, bool flush)
        {
            var outbound = this.FindContextOutbound();
            var eventExecutor = outbound.Executor;
            if (eventExecutor.InEventLoop)
            {
                return flush ? outbound.InvokeWriteAndFlushAsync(msg) : outbound.InvokeWriteAsync(msg);
            }

            var promise = new TaskCompletionSource();
            var task = flush ? WriteAndFlushTask.Create(outbound, msg, promise) : (IRunnable)WriteTask.Create(outbound, msg, promise);
            SafeExecuteOutbound(eventExecutor, task, promise, msg);
            return promise.Task;
        }

        private void NotifyHandlerException(Exception cause)
        {
            if (InExceptionCaught(cause))
            {
                if (DefaultChannelPipeline.Logger.WarnEnabled)
                {
                    DefaultChannelPipeline.Logger.Warn(
                        "An exception was thrown by a user handler " +
                            "while handling an exceptionCaught event", cause);
                }
                return;
            }

            this.InvokeExceptionCaught(cause);
        }

        private static Task ComposeExceptionTask(Exception cause) => TaskEx.FromException(cause);

        private const string ExceptionCaughtMethodName = nameof(IChannelHandler.ExceptionCaught);

        private static bool InExceptionCaught(Exception cause) => cause.StackTrace.IndexOf("." + ExceptionCaughtMethodName + "(", StringComparison.Ordinal) >= 0;

        private AbstractChannelHandlerContext FindContextInbound()
        {
            var ctx = this;
            do
            {
                ctx = ctx.Next;
            }
            while ((ctx.SkipPropagationFlags & SkipFlags.Inbound) == SkipFlags.Inbound);
            return ctx;
        }

        private AbstractChannelHandlerContext FindContextOutbound()
        {
            var ctx = this;
            do
            {
                ctx = ctx.Prev;
            }
            while ((ctx.SkipPropagationFlags & SkipFlags.Outbound) == SkipFlags.Outbound);
            return ctx;
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

        public override string ToString() => $"{nameof(IChannelHandlerContext)} ({this.Name}, {this.Channel})";

        private abstract class AbstractWriteTask
        {
            protected static readonly bool EstimateTaskSizeOnSubmit = SystemPropertyUtil.GetBoolean("io.netty.transport.estimateSizeOnSubmit", true);
            // Assuming a 64-bit .NET VM, 16 bytes object header, 4 reference fields and 2 int field
            protected static readonly int WriteTaskOverhead = SystemPropertyUtil.GetInt("io.netty.transport.writeTaskSizeOverhead", 56);
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
                        if (buffer != null)
                        {
                            buffer.DecrementPendingOutboundBytes(this.size);
                        }
                    }
                    this.WriteAsync(this.ctx, this.msg).LinkOutcome(this.promise);
                }
                finally
                {
                    this.ctx = null;
                    this.msg = null;
                    this.promise = null;
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
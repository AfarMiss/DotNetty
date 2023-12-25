using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels
{
    public abstract class AbstractChannel : ConstantMap, IChannel
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AbstractChannel>();
        private static readonly NotYetConnectedException NotYetConnectedException = new NotYetConnectedException();
        private readonly IChannelUnsafe channelUnsafe;
        private readonly DefaultChannelPipeline pipeline;
        private readonly TaskCompletionSource closeFuture = new TaskCompletionSource();

        private volatile EndPoint localAddress;
        private volatile EndPoint remoteAddress;
        private volatile IEventLoop eventLoop;
        private volatile bool registered;

        public ConstantMap ConstantMap => this;

        protected AbstractChannel(IChannel parent)
        {
            this.Parent = parent;
            this.Id = this.NewChannelId();
            this.channelUnsafe = this.NewUnsafe();
            this.pipeline = this.NewChannelPipeline();
        }

        protected AbstractChannel(IChannel parent, IChannelId id)
        {
            this.Parent = parent;
            this.Id = id;
            this.channelUnsafe = this.NewUnsafe();
            this.pipeline = this.NewChannelPipeline();
        }

        public IChannelId Id { get; }
        public bool IsWritable => this.channelUnsafe.OutboundBuffer != null && this.channelUnsafe.OutboundBuffer.IsWritable;
        public IChannel Parent { get; }
        public IChannelPipeline Pipeline => this.pipeline;
        public abstract IChannelConfiguration Configuration { get; }

        public IEventLoop EventLoop
        {
            get
            {
                var eventLoop = this.eventLoop;
                if (eventLoop == null)
                {
                    throw new InvalidOperationException("channel not registered to an event loop");
                }
                return eventLoop;
            }
        }

        public EndPoint LocalAddress => this.localAddress ?? this.CacheLocalAddress();
        public EndPoint RemoteAddress => this.remoteAddress ?? this.CacheRemoteAddress();
        
        public abstract bool Open { get; }
        public abstract bool Active { get; }
        public abstract ChannelMetadata Metadata { get; }
        protected abstract EndPoint LocalAddressInternal { get; }

        protected void InvalidateLocalAddress() => this.localAddress = null;

        protected EndPoint CacheLocalAddress()
        {
            try
            {
                return this.localAddress = this.LocalAddressInternal;
            }
            catch (Exception)
            {
                // Sometimes fails on a closed socket in Windows.
                return null;
            }
        }

        protected abstract EndPoint RemoteAddressInternal { get; }

        /// <summary>
        /// 重置缓存的<see cref="RemoteAddress"/>.
        /// </summary>
        protected void InvalidateRemoteAddress() => this.remoteAddress = null;

        protected EndPoint CacheRemoteAddress()
        {
            try
            {
                return this.remoteAddress = this.RemoteAddressInternal;
            }
            catch (Exception)
            {
                // Sometimes fails on a closed socket in Windows.
                return null;
            }
        }

        public bool Registered => this.registered;

        protected virtual IChannelId NewChannelId() => DefaultChannelId.NewInstance();

        protected virtual DefaultChannelPipeline NewChannelPipeline() => new DefaultChannelPipeline(this);

        public virtual Task BindAsync(EndPoint localAddress) => this.pipeline.BindAsync(localAddress);

        public virtual Task ConnectAsync(EndPoint remoteAddress) => this.pipeline.ConnectAsync(remoteAddress);

        public virtual Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress) => this.pipeline.ConnectAsync(remoteAddress, localAddress);

        public virtual Task DisconnectAsync() => this.pipeline.DisconnectAsync();

        public virtual Task CloseAsync() => this.pipeline.CloseAsync();

        public Task DeregisterAsync() => this.pipeline.DeregisterAsync();

        public void Flush() => this.pipeline.Flush();

        public void Read() => this.pipeline.Read();

        public Task WriteAsync(object msg) => this.pipeline.WriteAsync(msg);

        public Task WriteAndFlushAsync(object message) => this.pipeline.WriteAndFlushAsync(message);

        public Task CloseCompletion => this.closeFuture.Task;

        public IChannelUnsafe Unsafe => this.channelUnsafe;

        protected abstract IChannelUnsafe NewUnsafe();

        public override int GetHashCode() => this.Id.GetHashCode();

        public override bool Equals(object o) => this == o;

        public int CompareTo(IChannel o) => ReferenceEquals(this, o) ? 0 : this.Id.CompareTo(o.Id);

        bool IConstantTransfer.TransferSet<T>(IConstant<T> constant, T value)
        {
            this.ConstantMap.Set(constant, value);
            return true;
        }

        protected abstract class AbstractUnsafe : IChannelUnsafe
        {
            protected readonly AbstractChannel channel;
            private ChannelOutboundBuffer outboundBuffer;
            private IRecvByteBufAllocatorHandle recvHandle;
            private bool inFlush0;

            /// <summary> 通道是否从未注册 </summary>
            private bool neverRegistered = true;
            
            public ChannelOutboundBuffer OutboundBuffer => this.outboundBuffer;

            public IRecvByteBufAllocatorHandle RecvBufAllocHandle => this.recvHandle ??= this.channel.Configuration.RecvByteBufAllocator.NewHandle();

            protected AbstractUnsafe(AbstractChannel channel)
            {
                this.channel = channel;
                this.outboundBuffer = new ChannelOutboundBuffer(channel);
            }

            private void AssertEventLoop() => Contract.Assert(!this.channel.registered || this.channel.eventLoop.InEventLoop);

            public Task RegisterAsync(IEventLoop eventLoop)
            {
                if (this.channel.Registered)
                {
                    return TaskEx.FromException(new InvalidOperationException($"已注册到{nameof(IEventLoop)}"));
                }

                if (!this.channel.IsCompatible(eventLoop))
                {
                    return TaskEx.FromException(new InvalidOperationException($"{eventLoop.GetType().Name}不匹配"));
                }

                this.channel.eventLoop = eventLoop;

                var promise = new TaskCompletionSource();

                if (eventLoop.InEventLoop)
                {
                    this.Register0(promise);
                }
                else
                {
                    try
                    {
                        void Register0Action(object u, object p) => ((AbstractUnsafe)u).Register0((TaskCompletionSource)p);
                        eventLoop.Execute(Register0Action, this, promise);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("RegisterAsync Exception: {}", this.channel, ex);
                        this.CloseForcibly();
                        this.channel.closeFuture.Complete();
                        Util.SafeSetFailure(promise, ex, Logger);
                    }
                }

                return promise.Task;
            }

            private void Register0(TaskCompletionSource promise)
            {
                try
                {
                    if (!promise.SetUncancellable() || !this.EnsureOpen(promise))
                    {
                        Util.SafeSetFailure(promise, new ClosedChannelException(), Logger);
                        return;
                    }
                    
                    var firstRegistration = this.neverRegistered;
                    this.channel.DoRegister();
                    this.neverRegistered = false;
                    this.channel.registered = true;

                    Util.SafeSetSuccess(promise, Logger);
                    this.channel.pipeline.FireChannelRegistered();

                    // 仅当通道从未注册时
                    if (this.channel.Active)
                    {
                        if (firstRegistration)
                        {
                            this.channel.pipeline.FireChannelActive();
                        }
                        else if (this.channel.Configuration.AutoRead)
                        {
                            this.BeginRead();
                        }
                    }
                }
                catch (Exception t)
                {
                    this.CloseForcibly();
                    this.channel.closeFuture.Complete();
                    Util.SafeSetFailure(promise, t, Logger);
                }
            }

            public Task BindAsync(EndPoint localAddress)
            {
                this.AssertEventLoop();

                // todo: cancellation support
                if ( /*!promise.setUncancellable() || */!this.channel.Open)
                {
                    return this.CreateClosedChannelExceptionTask();
                }

                var wasActive = this.channel.Active;
                try
                {
                    this.channel.DoBind(localAddress);
                }
                catch (Exception t)
                {
                    this.CloseIfClosed();
                    return TaskEx.FromException(t);
                }

                if (!wasActive && this.channel.Active)
                {
                    this.InvokeLater(() => this.channel.pipeline.FireChannelActive());
                }

                return TaskEx.Completed;
            }

            public abstract Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

            public Task DisconnectAsync()
            {
                this.AssertEventLoop();

                var wasActive = this.channel.Active;
                try
                {
                    this.channel.DoDisconnect();
                }
                catch (Exception ex)
                {
                    this.CloseIfClosed();
                    return TaskEx.FromException(ex);
                }

                if (wasActive && !this.channel.Active)
                {
                    this.InvokeLater(() => this.channel.pipeline.FireChannelInactive());
                }

                this.CloseIfClosed();

                return TaskEx.Completed;
            }

            public Task CloseAsync()
            {
                this.AssertEventLoop();

                return this.CloseAsync(new ClosedChannelException(), false);
            }

            protected Task CloseAsync(Exception cause, bool notify)
            {
                var promise = new TaskCompletionSource();
                if (!promise.SetUncancellable())
                {
                    return promise.Task;
                }

                var outboundBuffer = this.outboundBuffer;
                if (outboundBuffer == null)
                {
                    // tcs尚未完成则返回,且已调用close().则返回closeFuture.Task
                    if (promise != TaskCompletionSource.Void)
                    {
                        return this.channel.closeFuture.Task;
                    }
                    return promise.Task;
                }

                if (this.channel.closeFuture.Task.IsCompleted)
                {
                    Util.SafeSetSuccess(promise, Logger);
                    return promise.Task;
                }

                bool wasActive = this.channel.Active;
                this.outboundBuffer = null;
                try
                {
                    // Close the channel and fail the queued messages input all cases.
                    this.DoClose0(promise);
                }
                finally
                {
                    // Fail all the queued messages.
                    outboundBuffer.FailFlushed(cause, notify);
                    outboundBuffer.Close(new ClosedChannelException());
                }
                if (this.inFlush0)
                {
                    this.InvokeLater(() => this.FireChannelInactiveAndDeregister(wasActive));
                }
                else
                {
                    this.FireChannelInactiveAndDeregister(wasActive);
                }

                return promise.Task;
            }

            private void DoClose0(TaskCompletionSource promise)
            {
                try
                {
                    this.channel.DoClose();
                    this.channel.closeFuture.Complete();
                    Util.SafeSetSuccess(promise, Logger);
                }
                catch (Exception t)
                {
                    this.channel.closeFuture.Complete();
                    Util.SafeSetFailure(promise, t, Logger);
                }
            }

            private void FireChannelInactiveAndDeregister(bool wasActive) => this.DeregisterAsync(wasActive && !this.channel.Active);

            public void CloseForcibly()
            {
                this.AssertEventLoop();

                try
                {
                    this.channel.DoClose();
                }
                catch (Exception e)
                {
                    Logger.Warn("Failed to close a channel.", e);
                }
            }

            /// <summary>
            /// 不要直接调用,inbound/outbound 可能导致嵌套
            /// </summary>
            public Task DeregisterAsync()
            {
                this.AssertEventLoop();

                return this.DeregisterAsync(false);
            }

            private Task DeregisterAsync(bool fireChannelInactive)
            {
                if (!this.channel.registered)
                {
                    return TaskEx.Completed;
                }

                var promise = new TaskCompletionSource();

                // ChannelPipeline进行处理时,可以从任何方法中调用Deregister().
                // 为防止正在进行处理时Deregister(),将Channel注册到新EventLoop,导致多少个EventLoop处理同一个Channel
                // 所以需要延迟执行实际的Deregister()
                this.InvokeLater(() =>
                {
                    try
                    {
                        this.channel.DoDeregister();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"Ex: {nameof(DoDeregister)}", ex);
                    }
                    finally
                    {
                        if (fireChannelInactive)
                        {
                            this.channel.pipeline.FireChannelInactive();
                        }
                        // Some transports like local and AIO does not allow the deregistration of
                        // an open channel.  Their doDeregister() calls close(). Consequently,
                        // close() calls deregister() again - no need to fire channelUnregistered, so check
                        // if it was registered.
                        if (this.channel.registered)
                        {
                            this.channel.registered = false;
                            this.channel.pipeline.FireChannelUnregistered();
                        }
                        Util.SafeSetSuccess(promise, Logger);
                    }
                });

                return promise.Task;
            }

            public void BeginRead()
            {
                this.AssertEventLoop();

                if (this.channel.Active)
                {
                    try
                    {
                        this.channel.DoBeginRead();
                    }
                    catch (Exception e)
                    {
                        this.InvokeLater(() => this.channel.pipeline.FireExceptionCaught(e));
                        this.CloseSafe();
                    }
                }
            }

            public Task WriteAsync(object msg)
            {
                this.AssertEventLoop();

                var outboundBuffer = this.outboundBuffer;
                
                // outboundBuffer == null 即Channel已关闭,则应立即失败
                if (outboundBuffer == null)
                {
                    ReferenceCountUtil.Release(msg);
                    return TaskEx.FromException(new ClosedChannelException());
                }

                int size;
                try
                {
                    msg = this.channel.FilterOutboundMessage(msg);
                    size = this.channel.pipeline.EstimatorHandle.Size(msg);
                    if (size < 0) size = 0;
                }
                catch (Exception ex)
                {
                    ReferenceCountUtil.Release(msg);
                    return TaskEx.FromException(ex);
                }

                var promise = new TaskCompletionSource();
                outboundBuffer.AddMessage(msg, size, promise);
                return promise.Task;
            }

            public void Flush()
            {
                this.AssertEventLoop();

                var outboundBuffer = this.outboundBuffer;
                if (outboundBuffer != null)
                {
                    outboundBuffer.AddFlush();
                    this.Flush0();
                }
            }

            protected virtual void Flush0()
            {
                if (this.inFlush0) return;

                var outboundBuffer = this.outboundBuffer;
                if (outboundBuffer == null || outboundBuffer.IsEmpty) return;

                this.inFlush0 = true;

                if (!this.CanWrite)
                {
                    try
                    {
                        if (this.channel.Open)
                        {
                            outboundBuffer.FailFlushed(NotYetConnectedException, true);
                        }
                        else
                        {
                            outboundBuffer.FailFlushed(new ClosedChannelException(), false);
                        }
                    }
                    finally
                    {
                        this.inFlush0 = false;
                    }
                }
                else
                {
                    try
                    {
                        this.channel.DoWrite(outboundBuffer);
                    }
                    catch (Exception ex)
                    {
                        Util.CompleteChannelCloseTaskSafely(this.channel, this.CloseAsync(new ClosedChannelException("Failed to write", ex), false));
                    }
                    finally
                    {
                        this.inFlush0 = false;
                    }
                }
            }

            protected virtual bool CanWrite => this.channel.Active;

            protected bool EnsureOpen(TaskCompletionSource promise)
            {
                var channelOpen = this.channel.Open;
                if (!channelOpen) Util.SafeSetFailure(promise, new ClosedChannelException(), Logger);
                return channelOpen;
            }

            protected Task CreateClosedChannelExceptionTask() => TaskEx.FromException(new ClosedChannelException());

            protected void CloseIfClosed()
            {
                if (!this.channel.Open) this.CloseSafe();
            }

            private void InvokeLater(Action task)
            {
                try
                {
                    // This method is used by outbound operation implementations to trigger an inbound event later.
                    // They do not trigger an inbound event immediately because an outbound operation might have been
                    // triggered by another inbound event handler method.  If fired immediately, the call stack
                    // will look like this for example:
                    //
                    //   handlerA.inboundBufferUpdated() - (1) an inbound handler method closes a connection.
                    //   -> handlerA.ctx.close()
                    //      -> channel.unsafe.close()
                    //         -> handlerA.channelInactive() - (2) another inbound handler method called while input (1) yet
                    //
                    // which means the execution of two inbound handler methods of the same handler overlap undesirably.
                    this.channel.EventLoop.Execute(task);
                }
                catch (RejectedExecutionException e)
                {
                    Logger.Warn($"{nameof(EventLoop)}拒绝任务", e);
                }
            }

            protected Exception AnnotateConnectException(Exception exception, EndPoint remoteAddress)
            {
                if (exception is SocketException)
                {
                    return new ConnectException("LogError connecting to " + remoteAddress, exception);
                }

                return exception;
            }
        }

        /// <summary>
        /// <see cref="IEventLoop"/>是否为<see cref="AbstractChannel"/>需求的
        /// </summary>
        protected abstract bool IsCompatible(IEventLoop eventLoop);

        protected virtual void DoRegister()
        {
        }

        protected abstract void DoBind(EndPoint localAddress);

        protected abstract void DoDisconnect();

        protected abstract void DoClose();

        protected virtual void DoDeregister()
        {
        }

        protected abstract void DoBeginRead();

        protected abstract void DoWrite(ChannelOutboundBuffer input);

        protected virtual object FilterOutboundMessage(object msg) => msg;
    }
}
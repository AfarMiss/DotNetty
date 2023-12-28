using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels
{
    public abstract partial class AbstractChannel
    {
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
                        eventLoop.Execute((@unsafe, tcs) => @unsafe.Register0(tcs), this, promise);
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
                    if (!this.EnsureOpen(promise))
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

                if (!this.channel.Open)
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
                    this.DoClose0(promise);
                }
                finally
                {
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
                    Logger.Warn("无法关闭channel", e);
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
                        Util.CompleteChannelCloseTaskSafely(this.channel, this.CloseAsync(new ClosedChannelException("写入失败", ex), false));
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
                    return new ConnectException("无法连接到:" + remoteAddress, exception);
                }

                return exception;
            }
        }
    }
}
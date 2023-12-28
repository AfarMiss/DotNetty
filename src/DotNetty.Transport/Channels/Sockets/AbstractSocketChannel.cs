using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels.Sockets
{
    public abstract class AbstractSocketChannel : AbstractChannel
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AbstractSocketChannel>();

        [Flags]
        protected enum StateFlags
        {
            Open = 1,
            ReadScheduled = 1 << 1,
            WriteScheduled = 1 << 2,
            Active = 1 << 3
        }

        internal static readonly EventHandler<SocketAsyncEventArgs> IoCompletedCallback = OnIoCompleted;
        private static readonly Action<object, object> ConnectCallbackAction = (u, e) => ((ISocketChannelUnsafe)u).FinishConnect((SocketChannelAsyncOperation)e);
        private static readonly Action<object, object> ReadCallbackAction = (u, e) => ((ISocketChannelUnsafe)u).FinishRead((SocketChannelAsyncOperation)e);
        private static readonly Action<object, object> WriteCallbackAction = (u, e) => ((ISocketChannelUnsafe)u).FinishWrite((SocketChannelAsyncOperation)e);

        private protected readonly Socket Socket;
        private SocketChannelAsyncOperation readOperation;
        private SocketChannelAsyncOperation writeOperation;
        private volatile bool inputShutdown; 
        internal bool ReadPending;
        private volatile StateFlags state;

        private TaskCompletionSource connectPromise;
        private IScheduledTask connectCancellationTask;

        protected AbstractSocketChannel(IChannel parent, Socket socket) : base()
        {
            this.Socket = socket;
            this.state = StateFlags.Open;

            try
            {
                this.Socket.Blocking = false;
            }
            catch (SocketException ex)
            {
                try
                {
                    socket.Dispose();
                }
                catch (SocketException ex2)
                {
                    if (Logger.WarnEnabled)
                    {
                        Logger.Warn("Failed to close a partially initialized socket.", ex2);
                    }
                }

                throw new ChannelException("Failed to enter non-blocking mode.", ex);
            }
        }

        public override bool Open => this.IsInState(StateFlags.Open);

        public override bool Active => this.IsInState(StateFlags.Active);

        protected internal void ClearReadPending()
        {
            if (this.Registered)
            {
                IEventLoop eventLoop = this.EventLoop;
                if (eventLoop.InEventLoop)
                {
                    this.ClearReadPending0();
                }
                else
                {
                    eventLoop.Execute(channel => ((AbstractSocketChannel)channel).ClearReadPending0(), this);
                }
            }
            else
            {
                // 如果尚未注册,尽量保证清除ReadPending
                this.ReadPending = false;
            }
        }

        private void ClearReadPending0() => this.ReadPending = false;

        protected bool InputShutdown => this.inputShutdown;

        protected void ShutdownInput() => this.inputShutdown = true;

        protected void SetState(StateFlags stateToSet) => this.state = this.state | stateToSet;

        protected StateFlags ResetState(StateFlags stateToReset)
        {
            var oldState = this.state;
            if ((oldState & stateToReset) != 0)
            {
                this.state = oldState & ~stateToReset;
            }
            return oldState;
        }

        protected bool TryResetState(StateFlags stateToReset)
        {
            var oldState = this.state;
            if ((oldState & stateToReset) != 0)
            {
                this.state = oldState & ~stateToReset;
                return true;
            }
            return false;
        }

        protected bool IsInState(StateFlags stateToCheck) => (this.state & stateToCheck) == stateToCheck;

        protected SocketChannelAsyncOperation ReadOperation => this.readOperation ??= new SocketChannelAsyncOperation(this, true);

        private SocketChannelAsyncOperation WriteOperation => this.writeOperation ??= new SocketChannelAsyncOperation(this, false);

        protected SocketChannelAsyncOperation PrepareWriteOperation(ArraySegment<byte> buffer)
        {
            var operation = this.WriteOperation;
            operation.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
            return operation;
        }

        protected SocketChannelAsyncOperation PrepareWriteOperation(IList<ArraySegment<byte>> buffers)
        {
            var operation = this.WriteOperation;
            operation.BufferList = buffers;
            return operation;
        }

        protected void ResetWriteOperation()
        {
            var operation = this.writeOperation;

            Contract.Assert(operation != null);

            if (operation.BufferList == null)
            {
                operation.SetBuffer(null, 0, 0);
            }
            else
            {
                operation.BufferList = null;
            }
        }

        private static void OnIoCompleted(object sender, SocketAsyncEventArgs args)
        {
            var operation = (SocketChannelAsyncOperation)args;
            var channel = operation.Channel;
            var @unsafe = (ISocketChannelUnsafe)channel.Unsafe;
            var eventLoop = channel.EventLoop;
            switch (args.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    if (eventLoop.InEventLoop)
                    {
                        @unsafe.FinishRead(operation);
                    }
                    else
                    {
                        eventLoop.Execute(ReadCallbackAction, @unsafe, operation);
                    }
                    break;
                case SocketAsyncOperation.Connect:
                    if (eventLoop.InEventLoop)
                    {
                        @unsafe.FinishConnect(operation);
                    }
                    else
                    {
                        eventLoop.Execute(ConnectCallbackAction, @unsafe, operation);
                    }
                    break;
                case SocketAsyncOperation.Receive:
                case SocketAsyncOperation.ReceiveFrom:
                    if (eventLoop.InEventLoop)
                    {
                        @unsafe.FinishRead(operation);
                    }
                    else
                    {
                        eventLoop.Execute(ReadCallbackAction, @unsafe, operation);
                    }
                    break;
                case SocketAsyncOperation.Send:
                case SocketAsyncOperation.SendTo:
                    if (eventLoop.InEventLoop)
                    {
                        @unsafe.FinishWrite(operation);
                    }
                    else
                    {
                        eventLoop.Execute(WriteCallbackAction, @unsafe, operation);
                    }
                    break;
                default:
                    // todo: think of a better way to comm exception
                    throw new ArgumentException("The last operation completed on the socket was not expected");
            }
        }

        internal interface ISocketChannelUnsafe : IChannelUnsafe
        {
            void FinishConnect(SocketChannelAsyncOperation operation);

            void FinishRead(SocketChannelAsyncOperation operation);

            void FinishWrite(SocketChannelAsyncOperation operation);
        }

        protected abstract class AbstractSocketUnsafe : AbstractUnsafe, ISocketChannelUnsafe
        {
            protected AbstractSocketUnsafe(AbstractSocketChannel channel)
                : base(channel)
            {
            }

            public AbstractSocketChannel Channel => (AbstractSocketChannel)this.channel;

            public sealed override Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
            {
                // todo: handle cancellation
                var ch = this.Channel;
                if (!ch.Open)
                {
                    return this.CreateClosedChannelExceptionTask();
                }

                try
                {
                    if (ch.connectPromise != null)
                    {
                        throw new InvalidOperationException("connection attempt already made");
                    }

                    bool wasActive = this.channel.Active;
                    if (ch.DoConnect(remoteAddress, localAddress))
                    {
                        this.FulfillConnectPromise(wasActive);
                        return TaskEx.Completed;
                    }
                    else
                    {
                        ch.connectPromise = new TaskCompletionSource(remoteAddress);

                        // Schedule connect timeout.
                        var connectTimeout = ch.Configuration.ConnectTimeout;
                        if (connectTimeout > TimeSpan.Zero)
                        {
                            ch.connectCancellationTask = ch.EventLoop.Schedule((socketChannel, address) =>
                                {
                                    // todo: make static / cache delegate?..
                                    var self = (AbstractSocketChannel)socketChannel;
                                    // todo: call Socket.CancelConnectAsync(...)
                                    var promise = self.connectPromise;
                                    var cause = new ConnectTimeoutException("connection timed out: " + address);
                                    if (promise != null && promise.TrySetException(cause))
                                    {
                                        self.CloseSafe();
                                    }
                                },
                                this.channel, remoteAddress, connectTimeout);
                        }

                        ch.connectPromise.Task.ContinueWith((task, socketChannel) =>
                            {
                                var c = (AbstractSocketChannel)socketChannel;
                                c.connectCancellationTask?.Cancel();
                                c.connectPromise = null;
                                c.CloseSafe();
                            },
                            ch, TaskContinuationOptions.OnlyOnCanceled | TaskContinuationOptions.ExecuteSynchronously);

                        return ch.connectPromise.Task;
                    }
                }
                catch (Exception ex)
                {
                    this.CloseIfClosed();
                    return TaskEx.FromException(this.AnnotateConnectException(ex, remoteAddress));
                }
            }

            private void FulfillConnectPromise(bool wasActive)
            {
                // 无论是否尝试连接被取消 都应ChannelActive()
                if (!wasActive && this.channel.Active)
                {
                    this.channel.Pipeline.FireChannelActive();
                }

                var promise = this.Channel.connectPromise;
                if (promise != null && !promise.TryComplete())
                {
                    this.CloseSafe();
                }
            }

            private void FulfillConnectPromise(Exception cause)
            {
                var promise = this.Channel.connectPromise;
                if (promise == null) return;

                promise.TrySetException(cause);
                this.CloseIfClosed();
            }

            public void FinishConnect(SocketChannelAsyncOperation operation)
            {
                Contract.Assert(this.channel.EventLoop.InEventLoop);

                var socketChannel = this.Channel;
                try
                {
                    var wasActive = socketChannel.Active;
                    socketChannel.DoFinishConnect(operation);
                    this.FulfillConnectPromise(wasActive);
                }
                catch (Exception ex)
                {
                    var promise = socketChannel.connectPromise;
                    var remoteAddress = (EndPoint)promise?.Task.AsyncState;
                    this.FulfillConnectPromise(this.AnnotateConnectException(ex, remoteAddress));
                }
                finally
                {
                    socketChannel.connectCancellationTask?.Cancel();
                    socketChannel.connectPromise = null;
                }
            }

            public abstract void FinishRead(SocketChannelAsyncOperation operation);

            protected sealed override void Flush0()
            {
                if (!this.IsFlushPending()) base.Flush0();
            }

            public void FinishWrite(SocketChannelAsyncOperation operation)
            {
                var resetWritePending = this.Channel.TryResetState(StateFlags.WriteScheduled);

                Contract.Assert(resetWritePending);

                var input = this.OutboundBuffer;
                try
                {
                    operation.Validate();
                    var sent = operation.BytesTransferred;
                    this.Channel.ResetWriteOperation();
                    if (sent > 0)
                    {
                        input.RemoveBytes(sent);
                    }
                }
                catch (Exception ex)
                {
                    Util.CompleteChannelCloseTaskSafely(this.channel, this.CloseAsync(new ClosedChannelException("Failed to write", ex), false));
                }
                this.Flush0();
            }

            private bool IsFlushPending() => this.Channel.IsInState(StateFlags.WriteScheduled);
        }

        protected override bool IsCompatible(IEventLoop eventLoop) => true;

        protected override void DoBeginRead()
        {
            if (this.inputShutdown || !this.Open) return;

            this.ReadPending = true;

            if (!this.IsInState(StateFlags.ReadScheduled))
            {
                this.state = this.state | StateFlags.ReadScheduled;
                this.ScheduleSocketRead();
            }
        }

        protected abstract void ScheduleSocketRead();

        protected abstract bool DoConnect(EndPoint remoteAddress, EndPoint localAddress);

        protected abstract void DoFinishConnect(SocketChannelAsyncOperation operation);

        protected override void DoClose()
        {
            var promise = this.connectPromise;
            if (promise != null)
            {
                promise.TrySetException(new ClosedChannelException());
                this.connectPromise = null;
            }

            var cancellationTask = this.connectCancellationTask;
            if (cancellationTask != null)
            {
                cancellationTask.Cancel();
                this.connectCancellationTask = null;
            }

            var readOp = this.readOperation;
            if (readOp != null)
            {
                readOp.Dispose();
                this.readOperation = null;
            }

            var writeOp = this.writeOperation;
            if (writeOp != null)
            {
                writeOp.Dispose();
                this.writeOperation = null;
            }
        }
    }
}
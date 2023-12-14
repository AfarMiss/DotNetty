using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using DotNetty.Common.Internal.Logging;

namespace DotNetty.Transport.Channels.Sockets
{
    public class TcpServerSocketChannel : AbstractSocketChannel, IServerSocketChannel
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<TcpServerSocketChannel>();
        private static readonly ChannelMetadata METADATA = new ChannelMetadata(false);

        private static readonly Action<object, object> ReadCompletedSyncCallback = OnReadCompletedSync;

        private readonly IServerSocketChannelConfiguration config;

        private SocketChannelAsyncOperation acceptOperation;

        public TcpServerSocketChannel() : this(new Socket(SocketType.Stream, ProtocolType.Tcp))
        {
        }

        public TcpServerSocketChannel(AddressFamily addressFamily) : this(new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
        }

        public TcpServerSocketChannel(Socket socket) : base(null, socket)
        {
            this.config = new TcpServerSocketChannelConfig(this, socket);
        }

        public override IChannelConfiguration Configuration => this.config;

        public override bool Active => this.Socket.IsBound;

        public override ChannelMetadata Metadata => METADATA;

        protected override EndPoint RemoteAddressInternal => null;

        protected override EndPoint LocalAddressInternal => this.Socket.LocalEndPoint;

        SocketChannelAsyncOperation AcceptOperation => this.acceptOperation ??= new SocketChannelAsyncOperation(this, false);

        protected override IChannelUnsafe NewUnsafe() => new TcpServerSocketChannelUnsafe(this);

        protected override void DoBind(EndPoint localAddress)
        {
            this.Socket.Bind(localAddress);
            this.Socket.Listen(this.config.Backlog);
            this.SetState(StateFlags.Active);

            this.CacheLocalAddress();
        }

        protected override void DoClose()
        {
            if (this.TryResetState(StateFlags.Open | StateFlags.Active))
            {
                this.Socket.Dispose();
            }
        }

        protected override void ScheduleSocketRead()
        {
            bool closed = false;
            var operation = this.AcceptOperation;
            while (!closed)
            {
                try
                {
                    bool pending = this.Socket.AcceptAsync(operation);
                    if (!pending)
                    {
                        this.EventLoop.Execute(ReadCompletedSyncCallback, this.Unsafe, operation);
                    }
                    return;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted || ex.SocketErrorCode == SocketError.InvalidArgument)
                {
                    closed = true;
                }
                catch (SocketException ex)
                {
                    // socket exceptions here are internal to channel's operation and should not go through the pipeline
                    // especially as they have no effect on overall channel's operation
                    Logger.Info("Exception on accept.", ex);
                }
                catch (ObjectDisposedException)
                {
                    closed = true;
                }
                catch (Exception ex)
                {
                    this.Pipeline.FireExceptionCaught(ex);
                    closed = true;
                }
            }
            if (closed && this.Open)
            {
                this.Unsafe.CloseSafe();
            }
        }

        private static void OnReadCompletedSync(object u, object p) => ((ISocketChannelUnsafe)u).FinishRead((SocketChannelAsyncOperation)p);

        protected override bool DoConnect(EndPoint remoteAddress, EndPoint localAddress)
        {
            throw new NotSupportedException();
        }

        protected override void DoFinishConnect(SocketChannelAsyncOperation operation)
        {
            throw new NotSupportedException();
        }

        protected override void DoDisconnect()
        {
            throw new NotSupportedException();
        }

        protected override void DoWrite(ChannelOutboundBuffer input)
        {
            throw new NotSupportedException();
        }

        protected sealed override object FilterOutboundMessage(object msg)
        {
            throw new NotSupportedException();
        }

        private sealed class TcpServerSocketChannelUnsafe : AbstractSocketUnsafe
        {
            private new TcpServerSocketChannel Channel => (TcpServerSocketChannel)this.channel;

            public TcpServerSocketChannelUnsafe(TcpServerSocketChannel channel) : base(channel)
            {
            }

            public override void FinishRead(SocketChannelAsyncOperation operation)
            {
                Contract.Assert(this.channel.EventLoop.InEventLoop);

                var ch = this.Channel;
                if ((ch.ResetState(StateFlags.ReadScheduled) & StateFlags.Active) == 0)
                {
                    return; // read was signaled as a result of channel closure
                }
                var config = ch.Configuration;
                var pipeline = ch.Pipeline;
                var allocHandle = this.Channel.Unsafe.RecvBufAllocHandle;
                allocHandle.Reset(config);

                bool closed = false;
                Exception exception = null;

                try
                {
                    try
                    {
                        var connectedSocket = operation.AcceptSocket;
                        operation.AcceptSocket = null;
                        operation.Validate();

                        var message = this.PrepareChannel(connectedSocket);

                        ch.ReadPending = false;
                        pipeline.FireChannelRead(message);
                        allocHandle.IncMessagesRead(1);

                        if (!config.AutoRead && !ch.ReadPending)
                        {
                            // ChannelConfig.setAutoRead(false) was called in the meantime.
                            // Completed Accept has to be processed though.
                            return;
                        }

                        while (allocHandle.ContinueReading())
                        {
                            connectedSocket = ch.Socket.Accept();
                            message = this.PrepareChannel(connectedSocket);

                            ch.ReadPending = false;
                            pipeline.FireChannelRead(message);
                            allocHandle.IncMessagesRead(1);
                        }
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted || ex.SocketErrorCode == SocketError.InvalidArgument)
                    {
                        closed = true;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                    }
                    catch (SocketException ex)
                    {
                        // socket exceptions here are internal to channel's operation and should not go through the pipeline
                        // especially as they have no effect on overall channel's operation
                        Logger.Info("Exception on accept.", ex);
                    }
                    catch (ObjectDisposedException)
                    {
                        closed = true;
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                    }

                    allocHandle.ReadComplete();
                    pipeline.FireChannelReadComplete();

                    if (exception != null)
                    {
                        // ServerChannel should not be closed even on SocketException because it can often continue
                        // accepting incoming connections. (e.g. too many open files)

                        pipeline.FireExceptionCaught(exception);
                    }

                    if (closed && ch.Open)
                    {
                        this.CloseSafe();
                    }
                }
                finally
                {
                    // Check if there is a readPending which was not processed yet.
                    if (!closed && (ch.ReadPending || config.AutoRead))
                    {
                        ch.DoBeginRead();
                    }
                }
            }

            private TcpSocketChannel PrepareChannel(Socket socket)
            {
                try
                {
                    return new TcpSocketChannel(this.channel, socket, true);
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to create a new channel from accepted socket.", ex);
                    try
                    {
                        socket.Dispose();
                    }
                    catch (Exception ex2)
                    {
                        Logger.Warn("Failed to close a socket cleanly.", ex2);
                    }
                    throw;
                }
            }
        }

        private sealed class TcpServerSocketChannelConfig : DefaultServerSocketChannelConfig
        {
            public TcpServerSocketChannelConfig(TcpServerSocketChannel channel, Socket javaSocket)
                : base(channel, javaSocket)
            {
            }

            protected override void AutoReadCleared() => ((TcpServerSocketChannel)this.Channel).ReadPending = false;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotNetty.Buffers;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels.Sockets
{
    public class TcpSocketChannel : AbstractSocketByteChannel, ISocketChannel
    {
        static readonly ChannelMetadata METADATA = new ChannelMetadata(false, 16);

        private readonly ISocketChannelConfiguration config;

        public TcpSocketChannel() : this(new Socket(SocketType.Stream, ProtocolType.Tcp))
        {
        }

        public TcpSocketChannel(AddressFamily addressFamily) : this(new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
        }

        public TcpSocketChannel(Socket socket) : this(null, socket)
        {
        }

        public TcpSocketChannel(IChannel parent, Socket socket) : this(parent, socket, false)
        {
        }

        internal TcpSocketChannel(IChannel parent, Socket socket, bool connected) : base(parent, socket)
        {
            this.config = new TcpSocketChannelConfig(this, socket);
            if (connected) this.OnConnected();
        }

        public override ChannelMetadata Metadata => METADATA;

        public override IChannelConfiguration Configuration => this.config;

        protected override EndPoint LocalAddressInternal => this.Socket.LocalEndPoint;

        protected override EndPoint RemoteAddressInternal => this.Socket.RemoteEndPoint;

        public Task ShutdownOutputAsync()
        {
            var tcs = new TaskCompletionSource();
            // todo: use closeExecutor if available
            var loop = this.EventLoop;
            if (loop.InEventLoop)
            {
                this.ShutdownOutput0(tcs);
            }
            else
            {
                loop.Execute(promise => this.ShutdownOutput0((TaskCompletionSource)promise), tcs);
            }
            return tcs.Task;
        }

        private void ShutdownOutput0(TaskCompletionSource promise)
        {
            try
            {
                this.Socket.Shutdown(SocketShutdown.Send);
                promise.Complete();
            }
            catch (Exception ex)
            {
                promise.SetException(ex);
            }
        }

        protected override void DoBind(EndPoint localAddress) => this.Socket.Bind(localAddress);

        protected override bool DoConnect(EndPoint remoteAddress, EndPoint localAddress)
        {
            if (localAddress != null)
            {
                this.Socket.Bind(localAddress);
            }

            bool success = false;
            try
            {
                var eventPayload = new SocketChannelAsyncOperation(this, false);
                eventPayload.RemoteEndPoint = remoteAddress;
                bool connected = !this.Socket.ConnectAsync(eventPayload);
                if (connected)
                {
                    this.DoFinishConnect(eventPayload);
                }
                success = true;
                return connected;
            }
            finally
            {
                if (!success)
                {
                    this.DoClose();
                }
            }
        }

        protected override void DoFinishConnect(SocketChannelAsyncOperation operation)
        {
            try
            {
                operation.Validate();
            }
            finally
            {
                operation.Dispose();
            }
            this.OnConnected();
        }

        private void OnConnected()
        {
            this.SetState(StateFlags.Active);

            this.CacheLocalAddress();
            this.CacheRemoteAddress();
        }

        protected override void DoDisconnect() => this.DoClose();

        protected override void DoClose()
        {
            try
            {
                if (this.TryResetState(StateFlags.Open))
                {
                    if (this.TryResetState(StateFlags.Active))
                    {
                        this.Socket.Shutdown(SocketShutdown.Both);
                    }
                    this.Socket.Dispose();
                }
            }
            finally
            {
                base.DoClose();
            }
        }

        protected override int DoReadBytes(IByteBuffer byteBuf)
        {
            if (!byteBuf.HasArray) throw new NotImplementedException("Only IByteBuffer implementations backed by array are supported.");

            // prevents ObjectDisposedException from being thrown in case connection has been lost in the meantime
            if (!this.Socket.Connected) return -1;
            
            int received = this.Socket.Receive(byteBuf.Array, byteBuf.ArrayOffset + byteBuf.WriterIndex, byteBuf.WritableBytes, SocketFlags.None, out var errorCode);
            switch (errorCode)
            {
                case SocketError.Success:
                    // Socket已Close
                    if (received == 0) return -1;
                    break;
                case SocketError.WouldBlock:
                    if (received == 0) return 0;
                    break;
                default:
                    throw new SocketException((int)errorCode);
            }

            byteBuf.SetWriterIndex(byteBuf.WriterIndex + received);

            return received;
        }

        protected override int DoWriteBytes(IByteBuffer buf)
        {
            if (!buf.HasArray)
            {
                throw new NotImplementedException("Only IByteBuffer implementations backed by array are supported.");
            }

            int sent = this.Socket.Send(buf.Array, buf.ArrayOffset + buf.ReaderIndex, buf.ReadableBytes, SocketFlags.None, out SocketError errorCode);

            if (errorCode != SocketError.Success && errorCode != SocketError.WouldBlock)
            {
                throw new SocketException((int)errorCode);
            }

            if (sent > 0)
            {
                buf.SetReaderIndex(buf.ReaderIndex + sent);
            }

            return sent;
        }

        protected override void DoWrite(ChannelOutboundBuffer input)
        {
            List<ArraySegment<byte>> sharedBufferList = null;
            try
            {
                while (true)
                {
                    int size = input.Size;
                    if (size == 0) break;
                    
                    long writtenBytes = 0;
                    bool done = false;

                    // Ensure the pending writes are made of ByteBufs only.
                    int maxBytesPerGatheringWrite = ((TcpSocketChannelConfig)this.config).GetMaxBytesPerGatheringWrite();
                    sharedBufferList = input.GetSharedBufferList(1024, maxBytesPerGatheringWrite);
                    int nioBufferCnt = sharedBufferList.Count;
                    long expectedWrittenBytes = input.NioBufferSize;
                    Socket socket = this.Socket;

                    List<ArraySegment<byte>> bufferList = sharedBufferList;
    
                    switch (nioBufferCnt)
                    {
                        case 0:
                            // We have something else beside ByteBuffers to write so fallback to normal writes.
                            base.DoWrite(input);
                            return;
                        default:
                            for (int i = this.Configuration.WriteSpinCount - 1; i >= 0; i--)
                            {
                                long localWrittenBytes = socket.Send(bufferList, SocketFlags.None, out SocketError errorCode);
                                if (errorCode != SocketError.Success && errorCode != SocketError.WouldBlock)
                                {
                                    throw new SocketException((int)errorCode);
                                }

                                if (localWrittenBytes == 0)
                                {
                                    break;
                                }

                                expectedWrittenBytes -= localWrittenBytes;
                                writtenBytes += localWrittenBytes;
                                if (expectedWrittenBytes == 0)
                                {
                                    done = true;
                                    break;
                                }
                                else
                                {
                                    bufferList = AdjustBufferList(localWrittenBytes, bufferList);
                                }
                            }
                            break;
                    }

                    if (writtenBytes > 0)
                    {
                        // 释放完全写入的缓冲区，并更新写入部分缓冲区的索引
                        input.RemoveBytes(writtenBytes);
                    }

                    if (!done)
                    {
                        IList<ArraySegment<byte>> asyncBufferList = bufferList;
                        if (object.ReferenceEquals(sharedBufferList, asyncBufferList))
                        {
                            asyncBufferList = sharedBufferList.ToArray(); // move out of shared list that will be reused which could corrupt buffers still pending update
                        }
                        
                        // Not all buffers were written out completely
                        var asyncOperation = this.PrepareWriteOperation(asyncBufferList);
                        if (this.IncompleteWrite(true, asyncOperation))
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                // Prepare the list for reuse
                sharedBufferList?.Clear();
            }
        }

        private static List<ArraySegment<byte>> AdjustBufferList(long localWrittenBytes, List<ArraySegment<byte>> bufferList)
        {
            var adjusted = new List<ArraySegment<byte>>(bufferList.Count);
            foreach (var buffer in bufferList)
            {
                if (localWrittenBytes > 0)
                {
                    long leftBytes = localWrittenBytes - buffer.Count;
                    if (leftBytes < 0)
                    {
                        int offset = buffer.Offset + (int)localWrittenBytes;
                        int count = -(int)leftBytes;
                        adjusted.Add(new ArraySegment<byte>(buffer.Array!, offset, count));
                        localWrittenBytes = 0;
                    }
                    else
                    {
                        localWrittenBytes = leftBytes;
                    }
                }
                else
                {
                    adjusted.Add(buffer);
                }
            }
            return adjusted;
        }

        protected override IChannelUnsafe NewUnsafe() => new TcpSocketChannelUnsafe(this);

        private sealed class TcpSocketChannelUnsafe : SocketByteChannelUnsafe
        {
            public TcpSocketChannelUnsafe(TcpSocketChannel channel)
                : base(channel)
            {
            }
        }

        private sealed class TcpSocketChannelConfig : DefaultSocketChannelConfiguration
        {
            private volatile int maxBytesPerGatheringWrite = int.MaxValue;

            public TcpSocketChannelConfig(TcpSocketChannel channel, Socket javaSocket)
                : base(channel, javaSocket)
            {
                this.CalculateMaxBytesPerGatheringWrite();
            }

            public int GetMaxBytesPerGatheringWrite() => this.maxBytesPerGatheringWrite;

            public override int SendBufferSize
            {
                get => base.SendBufferSize;
                set
                {
                    base.SendBufferSize = value;
                    this.CalculateMaxBytesPerGatheringWrite();
                }
            }

            private void CalculateMaxBytesPerGatheringWrite()
            {
                int newSendBufferSize = this.SendBufferSize << 1;
                if (newSendBufferSize > 0)
                {
                    this.maxBytesPerGatheringWrite = newSendBufferSize;
                }
            }

            protected override void AutoReadCleared() => ((TcpSocketChannel)this.Channel).ClearReadPending();
        }
    }
}
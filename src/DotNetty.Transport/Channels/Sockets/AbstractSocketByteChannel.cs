using System;
using System.Net.Sockets;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels.Sockets
{
    public abstract class AbstractSocketByteChannel : AbstractSocketChannel
    {
        private static readonly Action<object> FlushAction = o => ((AbstractSocketByteChannel)o).Flush();
        private static readonly Action<object, object> ReadCompletedSyncAction = OnReadCompletedSync;

        protected AbstractSocketByteChannel(IChannel parent, Socket socket) : base(parent, socket)
        {
        }

        protected override IChannelUnsafe NewUnsafe() => new SocketByteChannelUnsafe(this);

        protected class SocketByteChannelUnsafe : AbstractSocketUnsafe
        {
            public SocketByteChannelUnsafe(AbstractSocketByteChannel channel) : base(channel)
            {
            }

            private new AbstractSocketByteChannel Channel => (AbstractSocketByteChannel)this.channel;

            private void CloseOnRead()
            {
                this.Channel.ShutdownInput();
                if (this.channel.Open)
                {
                    this.CloseSafe();
                }
            }

            private void HandleReadException(IChannelPipeline pipeline, IByteBuffer byteBuf, Exception cause, bool close, IRecvByteBufAllocatorHandle allocHandle)
            {
                if (byteBuf != null)
                {
                    if (byteBuf.IsReadable())
                    {
                        this.Channel.ReadPending = false;
                        pipeline.FireChannelRead(byteBuf);
                    }
                    else
                    {
                        byteBuf.Release();
                    }
                }
                allocHandle.ReadComplete();
                pipeline.FireChannelReadComplete();
                pipeline.FireExceptionCaught(cause);
                if (close || cause is SocketException)
                {
                    this.CloseOnRead();
                }
            }

            public override void FinishRead(SocketChannelAsyncOperation operation)
            {
                var channel = this.Channel;
                // Channel已Close
                if ((channel.ResetState(StateFlags.ReadScheduled) & StateFlags.Active) == 0) return;
                
                var config = channel.Configuration;
                var pipeline = channel.Pipeline;
                var allocator = config.Allocator;
                var allocHandle = this.RecvBufAllocHandle;
                allocHandle.Reset(config);

                IByteBuffer byteBuf = null;
                bool close = false;
                try
                {
                    operation.Validate();
                    do
                    {
                        byteBuf = allocHandle.Allocate(allocator);
                        allocHandle.LastBytesRead = channel.DoReadBytes(byteBuf);
                        if (allocHandle.LastBytesRead <= 0)
                        {
                            byteBuf.Release();
                            byteBuf = null;
                            close = allocHandle.LastBytesRead < 0;
                            break;
                        }

                        allocHandle.IncMessagesRead(1);
                        this.Channel.ReadPending = false;

                        pipeline.FireChannelRead(byteBuf);
                        byteBuf = null;
                    }
                    while (allocHandle.ContinueReading());

                    allocHandle.ReadComplete();
                    pipeline.FireChannelReadComplete();

                    if (close) this.CloseOnRead();
                }
                catch (Exception t)
                {
                    this.HandleReadException(pipeline, byteBuf, t, close, allocHandle);
                }
                finally
                {
                    if (!close && (channel.ReadPending || config.AutoRead))
                    {
                        channel.DoBeginRead();
                    }
                }
            }
        }

        protected override void ScheduleSocketRead()
        {
            var operation = this.ReadOperation;
            var pending = this.Socket.ReceiveAsync(operation);

            if (!pending)
            {
                // todo: potential allocation / non-static field?
                this.EventLoop.Execute(ReadCompletedSyncAction, this.Unsafe, operation);
            }
        }

        private static void OnReadCompletedSync(object u, object e) => ((ISocketChannelUnsafe)u).FinishRead((SocketChannelAsyncOperation)e);

        protected override void DoWrite(ChannelOutboundBuffer input)
        {
            int writeSpinCount = -1;

            while (true)
            {
                object msg = input.Current;
                if (msg == null) break;

                if (msg is IByteBuffer buf)
                {
                    var readableBytes = buf.ReadableBytes;
                    if (readableBytes == 0)
                    {
                        input.Remove();
                        continue;
                    }

                    bool scheduleAsync = false;
                    bool done = false;
                    long flushedAmount = 0;
                    if (writeSpinCount == -1)
                    {
                        writeSpinCount = this.Configuration.WriteSpinCount;
                    }
                    for (int i = writeSpinCount - 1; i >= 0; i--)
                    {
                        int localFlushedAmount = this.DoWriteBytes(buf);
                        if (localFlushedAmount == 0) // todo: check for "sent less than attempted bytes" to avoid unnecessary extra doWriteBytes call?
                        {
                            scheduleAsync = true;
                            break;
                        }

                        flushedAmount += localFlushedAmount;
                        if (!buf.IsReadable())
                        {
                            done = true;
                            break;
                        }
                    }

                    input.Progress(flushedAmount);

                    if (done)
                    {
                        input.Remove();
                    }
                    else if (this.IncompleteWrite(scheduleAsync, this.PrepareWriteOperation(buf.GetIoBuffer())))
                    {
                        break;
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        protected override object FilterOutboundMessage(object msg)
        {
            if (msg is IByteBuffer) return msg;

            throw new NotSupportedException("Message Type: " + msg.GetType().Name);
        }

        protected bool IncompleteWrite(bool scheduleAsync, SocketChannelAsyncOperation operation)
        {
            if (scheduleAsync)
            {
                this.SetState(StateFlags.WriteScheduled);

                var pending = this.Socket.SendAsync(operation);
                if (!pending)
                {
                    ((ISocketChannelUnsafe)this.Unsafe).FinishWrite(operation);
                }

                return pending;
            }
            else
            {
                this.EventLoop.Execute(FlushAction, this);
                return true;
            }
        }


        protected abstract int DoReadBytes(IByteBuffer buf);

        protected abstract int DoWriteBytes(IByteBuffer buf);
    }
}
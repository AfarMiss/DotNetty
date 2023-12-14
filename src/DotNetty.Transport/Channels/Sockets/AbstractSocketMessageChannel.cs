using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net.Sockets;

namespace DotNetty.Transport.Channels.Sockets
{
    public abstract class AbstractSocketMessageChannel : AbstractSocketChannel
    {
        /// <summary>
        /// Creates a new <see cref="AbstractSocketMessageChannel"/> instance.
        /// </summary>
        /// <param name="parent">The parent <see cref="IChannel"/>. Pass <c>null</c> if there's no parent.</param>
        /// <param name="socket">The <see cref="Socket"/> used by the <see cref="IChannel"/> for communication.</param>
        protected AbstractSocketMessageChannel(IChannel parent, Socket socket)
            : base(parent, socket)
        {
        }

        protected override IChannelUnsafe NewUnsafe() => new SocketMessageUnsafe(this);

        protected class SocketMessageUnsafe : AbstractSocketUnsafe
        {
            private readonly List<object> readBuf = new List<object>();

            public SocketMessageUnsafe(AbstractSocketMessageChannel channel)
                : base(channel)
            {
            }

            private new AbstractSocketMessageChannel Channel => (AbstractSocketMessageChannel)this.channel;

            public override void FinishRead(SocketChannelAsyncOperation operation)
            {
                Contract.Assert(this.channel.EventLoop.InEventLoop);

                var messageChannel = this.Channel;
                if ((messageChannel.ResetState(StateFlags.ReadScheduled) & StateFlags.Active) == 0)
                {
                    return; // read was signaled as a result of channel closure
                }
                var config = messageChannel.Configuration;

                var pipeline = messageChannel.Pipeline;
                var allocHandle = this.Channel.Unsafe.RecvBufAllocHandle;
                allocHandle.Reset(config);

                bool closed = false;
                Exception exception = null;
                try
                {
                    try
                    {
                        do
                        {
                            int localRead = messageChannel.DoReadMessages(this.readBuf);
                            if (localRead == 0)
                            {
                                break;
                            }
                            if (localRead < 0)
                            {
                                closed = true;
                                break;
                            }

                            allocHandle.IncMessagesRead(localRead);
                        }
                        while (allocHandle.ContinueReading());
                    }
                    catch (Exception t)
                    {
                        exception = t;
                    }
                    int size = this.readBuf.Count;
                    for (int i = 0; i < size; i++)
                    {
                        messageChannel.ReadPending = false;
                        pipeline.FireChannelRead(this.readBuf[i]);
                    }

                    this.readBuf.Clear();
                    allocHandle.ReadComplete();
                    pipeline.FireChannelReadComplete();

                    if (exception != null)
                    {
                        var asSocketException = exception as SocketException;
                        if (asSocketException != null && asSocketException.SocketErrorCode != SocketError.TryAgain) // todo: other conditions for not closing message-based socket?
                        {
                            // ServerChannel should not be closed even on SocketException because it can often continue
                            // accepting incoming connections. (e.g. too many open files)
                            closed = !(messageChannel is IServerChannel);
                        }

                        pipeline.FireExceptionCaught(exception);
                    }

                    if (closed)
                    {
                        if (messageChannel.Open)
                        {
                            this.CloseSafe();
                        }
                    }
                }
                finally
                {
                    // Check if there is a readPending which was not processed yet.
                    // This could be for two reasons:
                    // /// The user called Channel.read() or ChannelHandlerContext.read() in channelRead(...) method
                    // /// The user called Channel.read() or ChannelHandlerContext.read() in channelReadComplete(...) method
                    //
                    // See https://github.com/netty/netty/issues/2254
                    if (!closed && (messageChannel.ReadPending || config.AutoRead))
                    {
                        messageChannel.DoBeginRead();
                    }
                }
            }
        }

        protected override void DoWrite(ChannelOutboundBuffer input)
        {
            while (true)
            {
                object msg = input.Current;
                if (msg == null)
                {
                    break;
                }
                try
                {
                    bool done = false;
                    for (int i = this.Configuration.WriteSpinCount - 1; i >= 0; i--)
                    {
                        if (this.DoWriteMessage(msg, input))
                        {
                            done = true;
                            break;
                        }
                    }

                    if (done)
                    {
                        input.Remove();
                    }
                    else
                    {
                        // Did not write all messages.
                        this.ScheduleMessageWrite(msg);
                        break;
                    }
                }
                catch (SocketException e)
                {
                    if (this.ContinueOnWriteError)
                    {
                        input.Remove(e);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        protected abstract void ScheduleMessageWrite(object message);

        /// <summary>
        /// Returns <c>true</c> if we should continue the write loop on a write error.
        /// </summary>
        protected virtual bool ContinueOnWriteError => false;

        /// <summary>
        /// Reads messages into the given list and returns the amount which was read.
        /// </summary>
        /// <param name="buf">The list into which message objects should be inserted.</param>
        /// <returns>The number of messages which were read.</returns>
        protected abstract int DoReadMessages(List<object> buf);

        /// <summary>
        /// Writes a message to the underlying <see cref="IChannel"/>.
        /// </summary>
        /// <param name="msg">The message to be written.</param>
        /// <param name="input">The destination channel buffer for the message.</param>
        /// <returns><c>true</c> if the message was successfully written, otherwise <c>false</c>.</returns>
        protected abstract bool DoWriteMessage(object msg, ChannelOutboundBuffer input);
    }
}
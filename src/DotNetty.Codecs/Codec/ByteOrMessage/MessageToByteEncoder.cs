using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs
{
    public abstract class MessageToByteEncoder<T> : ChannelHandlerAdapter
    {
        public virtual bool AcceptOutboundMessage(object message) => message is T;

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            Contract.Requires(context != null);

            IByteBuffer buffer = null;
            Task result;
            try
            {
                if (this.AcceptOutboundMessage(message))
                {
                    buffer = this.AllocateBuffer(context);
                    try
                    {
                        this.Encode(context, (T)message, buffer);
                    }
                    finally
                    {
                        ReferenceCountUtil.Release((T)message);
                    }

                    if (buffer.IsReadable())
                    {
                        result = context.WriteAsync(buffer);
                    }
                    else
                    {
                        buffer.Release();
                        result = context.WriteAsync(ByteBuffer.Empty);
                    }

                    buffer = null;
                }
                else
                {
                    return context.WriteAsync(message);
                }
            }
            catch (EncoderException e)
            {
                return TaskEx.FromException(e);
            }
            catch (Exception ex)
            {
                return TaskEx.FromException(new EncoderException(ex));
            }
            finally
            {
                buffer?.Release();
            }

            return result;
        }

        protected virtual IByteBuffer AllocateBuffer(IChannelHandlerContext context)
        {
            Contract.Requires(context != null);

            return context.Allocator.Buffer();
        }

        protected abstract void Encode(IChannelHandlerContext context, T message, IByteBuffer output);
    }
}

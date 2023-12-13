using System;
using System.Collections.Generic;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs
{
    public abstract class MessageToMessageDecoder<T> : ChannelHandlerAdapter
    {
        public virtual bool AcceptInboundMessage(object msg) => msg is T;

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var output = ThreadLocalListPool.Acquire();
            try
            {
                if (this.AcceptInboundMessage(message))
                {
                    try
                    {
                        this.Decode(context, (T)message, output);
                    }
                    finally
                    {
                        ReferenceCountUtil.Release((T)message);
                    }
                }
                else
                {
                    output.Add(message);
                }
            }
            catch (DecoderException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DecoderException(e);
            }
            finally
            {
                int size = output.Count;
                for (int i = 0; i < size; i++)
                {
                    context.FireChannelRead(output[i]);
                }
                ThreadLocalListPool.Recycle(output);
            }
        }

        protected internal abstract void Decode(IChannelHandlerContext context, T message, List<object> output);
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs
{
    public abstract class MessageToMessageEncoder<T> : ChannelHandlerAdapter
    {
        public virtual bool AcceptOutboundMessage(object msg) => msg is T;

        public override Task WriteAsync(IChannelHandlerContext ctx, object msg)
        {
            Task result;
            PoolList output = null;
            try
            {
                if (this.AcceptOutboundMessage(msg))
                {
                    output = ThreadLocalListPool.Acquire();
                    try
                    {
                        this.Encode(ctx, (T)msg, output);
                    }
                    finally
                    {
                        ReferenceCountUtil.Release((T)msg);
                    }

                    if (output.Count == 0)
                    {
                        ThreadLocalListPool.Recycle(output);
                        output = null;

                        throw new EncoderException(this.GetType().Name + " must produce at least one message.");
                    }
                }
                else
                {
                    return ctx.WriteAsync(msg);
                }
            }
            catch (EncoderException e)
            {
                return TaskEx.FromException(e);
            }
            catch (Exception ex)
            {
                // TODO: 在EncoderException上没有堆栈，但它存在于内部异常上
                return TaskEx.FromException(new EncoderException(ex)); 
            }
            finally
            {
                if (output != null)
                {
                    int lastItemIndex = output.Count - 1;
                    if (lastItemIndex == 0)
                    {
                        result = ctx.WriteAsync(output[0]);
                    }
                    else if (lastItemIndex > 0)
                    {
                        for (int i = 0; i < lastItemIndex; i++)
                        {
                            // we don't care about output from these messages as failure while sending one of these messages will fail all messages up to the last message - which will be observed by the caller in Task result.
                            // todo: optimize: once IChannelHandlerContext allows, pass "not interested in task" flag
                            ctx.WriteAsync(output[i]);
                        }
                        result = ctx.WriteAsync(output[lastItemIndex]);
                    }
                    else
                    {
                        // 0 items in output - must never get here
                        result = null;
                    }
                    ThreadLocalListPool.Recycle(output);
                }
                else
                {
                    // output was reset during exception handling - must never get here
                    result = null;
                }
            }
            return result;
        }

        protected internal abstract void Encode(IChannelHandlerContext context, T message, List<object> output);
    }
}
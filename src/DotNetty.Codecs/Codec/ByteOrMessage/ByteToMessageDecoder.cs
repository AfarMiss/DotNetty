using System;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Common;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs
{
    public abstract class ByteToMessageDecoder : ChannelHandlerAdapter
    {
        private IByteBuffer cumulation;
        private CumulationFunc cumulator = MergeCumulator;
        private bool decodeWasNull;
        private bool first;
        
        /// <summary> 数据只解码一次 </summary>
        public bool SingleDecode { get; set; }
        
        protected int ActualReadableBytes => this.InternalBuffer.ReadableBytes;
        protected IByteBuffer InternalBuffer => this.cumulation ?? Unpooled.Empty;

        public delegate IByteBuffer CumulationFunc(IByteBufferAllocator alloc, IByteBuffer cumulation, IByteBuffer input);

        /// <summary> 合并<see cref="IByteBuffer"/> 不足则产生新<see cref="IByteBuffer"/></summary>
        public static readonly CumulationFunc MergeCumulator = (allocator, cumulation, input) =>
        {
            IByteBuffer buffer;
            if (cumulation.WriterIndex > cumulation.MaxCapacity - input.ReadableBytes || cumulation.ReferenceCount > 1)
            {
                buffer = ExpandCumulation(allocator, cumulation, input.ReadableBytes);
            }
            else
            {
                buffer = cumulation;
            }
            buffer.WriteBytes(input);
            input.Release();
            return buffer;
        };

        /// <summary> 合并<see cref="IByteBuffer"/> 不足则产生新<see cref="CompositeByteBuffer"/></summary>
        public static CumulationFunc CompositionCumulation = (alloc, cumulation, input) =>
        {
            IByteBuffer buffer;
            if (cumulation.ReferenceCount > 1)
            {
                buffer = ExpandCumulation(alloc, cumulation, input.ReadableBytes);
                buffer.WriteBytes(input);
                input.Release();
            }
            else
            {
                if (!(cumulation is CompositeByteBuffer composite))
                {
                    int readable = cumulation.ReadableBytes;
                    composite = alloc.CompositeBuffer();
                    composite.AddComponent(cumulation).SetWriterIndex(readable);
                }

                composite.AddComponent(input).SetWriterIndex(composite.WriterIndex + input.ReadableBytes);
                buffer = composite;
            }
            return buffer;
        };

        protected ByteToMessageDecoder()
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor -- used for safety check only
            if (this.IsSharable)
            {
                throw new InvalidOperationException($"Decoders inheriting from {nameof(ByteToMessageDecoder)} cannot be sharable.");
            }
        }

        public void SetCumulator(CumulationFunc cumulationFunc) => this.cumulator = cumulationFunc;

        protected internal abstract void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output);

        private static IByteBuffer ExpandCumulation(IByteBufferAllocator allocator, IByteBuffer cumulation, int readable)
        {
            var oldCumulation = cumulation;
            cumulation = allocator.Buffer(oldCumulation.ReadableBytes + readable);
            cumulation.WriteBytes(oldCumulation);
            oldCumulation.Release();
            return cumulation;
        }

        public override void HandlerRemoved(IChannelHandlerContext context)
        {
            var buf = this.InternalBuffer;

            this.cumulation = null;
            int readable = buf.ReadableBytes;
            if (readable > 0)
            {
                var bytes = context.Allocator.Buffer(readable, buf.MaxCapacity);
                buf.ReadBytes(bytes, readable);
                buf.Release();
                context.FireChannelRead(bytes);
            }
            else
            {
                buf.Release();
            }

            context.FireChannelReadComplete();
            this.HandlerRemovedInternal(context);
        }

        protected virtual void HandlerRemovedInternal(IChannelHandlerContext context)
        {
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is IByteBuffer data)
            {
                var output = ThreadLocalListPool.Acquire();
                try
                {
                    this.first = this.cumulation == null;
                    this.cumulation = this.first ? data : this.cumulator(context.Allocator, this.cumulation, data);
                    this.CallDecode(context, this.cumulation, output);
                }
                catch (DecoderException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new DecoderException(ex);
                }
                finally
                {
                    if (this.cumulation != null && !this.cumulation.IsReadable())
                    {
                        this.cumulation.Release();
                        this.cumulation = null;
                    }
                    int size = output.Count;
                    this.decodeWasNull = size == 0;

                    for (int i = 0; i < size; i++)
                    {
                        context.FireChannelRead(output[i]);
                    }
                    ThreadLocalListPool.Recycle(output);
                }
            }
            else
            {
                context.FireChannelRead(message);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            this.DiscardSomeReadBytes();
            if (this.decodeWasNull)
            {
                this.decodeWasNull = false;
                if (!context.Channel.Configuration.AutoRead)
                {
                    context.Read();
                }
            }
            context.FireChannelReadComplete();
        }

        protected void DiscardSomeReadBytes()
        {
            if (this.cumulation != null && !this.first && this.cumulation.ReferenceCount == 1)
            {
                this.cumulation.DiscardSomeReadBytes();
            }
        }

        public override void ChannelInactive(IChannelHandlerContext ctx)
        {
            var output = ThreadLocalListPool.Acquire();
            try
            {
                if (this.cumulation != null)
                {
                    this.CallDecode(ctx, this.cumulation, output);
                    this.DecodeLast(ctx, this.cumulation, output);
                }
                else
                {
                    this.DecodeLast(ctx, Unpooled.Empty, output);
                }
            }
            catch (DecoderException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new DecoderException(e);
            }
            finally
            {
                try
                {
                    if (this.cumulation != null)
                    {
                        this.cumulation.Release();
                        this.cumulation = null;
                    }
                    int size = output.Count;
                    for (int i = 0; i < size; i++)
                    {
                        ctx.FireChannelRead(output[i]);
                    }
                    if (size > 0)
                    {
                        // Something was read, call fireChannelReadComplete()
                        ctx.FireChannelReadComplete();
                    }
                    ctx.FireChannelInactive();
                }
                finally
                {
                    // recycle in all cases
                    ThreadLocalListPool.Recycle(output);
                }
            }
        }

        protected virtual void CallDecode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            try
            {
                while (input.IsReadable())
                {
                    int initialOutputCount = output.Count;
                    int oldInputLength = input.ReadableBytes;
                    this.Decode(context, input, output);

                    // 检测 可能已被移除
                    if (context.Removed) break;

                    // 解码列表长度相等且需解码数据未读 无需继续处理
                    if (initialOutputCount == output.Count && oldInputLength == input.ReadableBytes) break;
                    // 解码列表长度相等且需解码数据未已读 继续处理
                    if (initialOutputCount == output.Count && oldInputLength != input.ReadableBytes) continue;
                    // 解码列表长度不等且需解码数据未读
                    if (oldInputLength == input.ReadableBytes)
                    {
                        throw new DecoderException($"{this.GetType().Name}.Decode() did not read anything but decoded a message.");
                    }

                    if (this.SingleDecode) break;
                }
            }
            catch (DecoderException)
            {
                throw;
            }
            catch (Exception cause)
            {
                throw new DecoderException(cause);
            }
        }

        protected virtual void DecodeLast(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            if (input.IsReadable()) this.Decode(context, input, output);
        }
    }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;

namespace DotNetty.Codecs
{
    public abstract class MessageToMessageCodec<TInbound, TOutbound> : ChannelDuplexHandler
    {
        private readonly Encoder encoder;
        private readonly Decoder decoder;

        private sealed class Encoder : MessageToMessageEncoder<object>
        {
            private readonly MessageToMessageCodec<TInbound, TOutbound> codec;

            public Encoder(MessageToMessageCodec<TInbound, TOutbound> codec) => this.codec = codec;

            public override bool AcceptOutboundMessage(object msg) => this.codec.AcceptOutboundMessage(msg);

            protected internal override void Encode(IChannelHandlerContext context, object message, List<object> output) => this.codec.Encode(context, (TOutbound)message, output);
        }

        private sealed class Decoder : MessageToMessageDecoder<object>
        {
            private readonly MessageToMessageCodec<TInbound, TOutbound> codec;

            public Decoder(MessageToMessageCodec<TInbound, TOutbound> codec) => this.codec = codec;

            public override bool AcceptInboundMessage(object msg) => this.codec.AcceptInboundMessage(msg);

            protected internal override void Decode(IChannelHandlerContext context, object message, List<object> output) => this.codec.Decode(context, (TInbound)message, output);
        }

        protected MessageToMessageCodec()
        {
            this.encoder = new Encoder(this);
            this.decoder = new Decoder(this);
        }

        protected abstract void Encode(IChannelHandlerContext ctx, TOutbound msg, List<object> output);
        protected abstract void Decode(IChannelHandlerContext ctx, TInbound msg, List<object> output);
        
        public sealed override void ChannelRead(IChannelHandlerContext context, object message) => this.decoder.ChannelRead(context, message);

        public sealed override Task WriteAsync(IChannelHandlerContext context, object message) => this.encoder.WriteAsync(context, message);

        public virtual bool AcceptInboundMessage(object msg) => msg is TInbound;

        public virtual bool AcceptOutboundMessage(object msg) => msg is TOutbound;
    }
}

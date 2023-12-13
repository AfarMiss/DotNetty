using System;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace DotNetty.Codecs
{
    public class DatagramPacketDecoder : MessageToMessageDecoder<DatagramPacket>
    {
        private readonly MessageToMessageDecoder<IByteBuffer> decoder;

        public DatagramPacketDecoder(MessageToMessageDecoder<IByteBuffer> decoder) => this.decoder = decoder;

        public override bool AcceptInboundMessage(object msg)
        {
            var envelope = msg as DatagramPacket;
            return envelope != null && this.decoder.AcceptInboundMessage(envelope.Content);
        }

        protected internal override void Decode(IChannelHandlerContext context, DatagramPacket message, List<object> output) => 
            this.decoder.Decode(context, message.Content, output);

        public override void ChannelRegistered(IChannelHandlerContext context) => 
            this.decoder.ChannelRegistered(context);

        public override void ChannelUnregistered(IChannelHandlerContext context) => 
            this.decoder.ChannelUnregistered(context);

        public override void ChannelActive(IChannelHandlerContext context) => this.decoder.ChannelActive(context);

        public override void ChannelInactive(IChannelHandlerContext context) => this.decoder.ChannelInactive(context);

        public override void ChannelReadComplete(IChannelHandlerContext context) => this.decoder.ChannelReadComplete(context);

        public override void UserEventTriggered(IChannelHandlerContext context, object evt) => this.decoder.UserEventTriggered(context, evt);

        public override void ChannelWritabilityChanged(IChannelHandlerContext context) => this.decoder.ChannelWritabilityChanged(context);

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => this.decoder.ExceptionCaught(context, exception);

        public override void HandlerAdded(IChannelHandlerContext context) => this.decoder.HandlerAdded(context);

        public override void HandlerRemoved(IChannelHandlerContext context) => this.decoder.HandlerRemoved(context);
    }
}

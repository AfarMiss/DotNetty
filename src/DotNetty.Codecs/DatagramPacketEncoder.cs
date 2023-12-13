﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace DotNetty.Codecs
{
    public class DatagramPacketEncoder<T> : MessageToMessageEncoder<IAddressedEnvelope<T>>
    {
        private readonly MessageToMessageEncoder<T> encoder;

        public DatagramPacketEncoder(MessageToMessageEncoder<T> encoder) => this.encoder = encoder;

        public override bool AcceptOutboundMessage(object msg)
        {
            var envelope = msg as IAddressedEnvelope<T>;
            return envelope != null && this.encoder.AcceptOutboundMessage(envelope.Content) && (envelope.Sender != null || envelope.Recipient != null);
        }

        protected internal override void Encode(IChannelHandlerContext context, IAddressedEnvelope<T> message, List<object> output)
        {
            this.encoder.Encode(context, message.Content, output);
            if (output.Count != 1) throw new EncoderException($"{this.encoder.GetType()} must produce only one message.");

            if (!(output[0] is IByteBuffer content)) throw new EncoderException($"{this.encoder.GetType()} must produce only IByteBuffer.");

            // DatagramPacket替换ByteBuf
            output[0] = new DatagramPacket(content, message.Sender, message.Recipient);
        }

        public override Task BindAsync(IChannelHandlerContext context, EndPoint localAddress) => 
            this.encoder.BindAsync(context, localAddress);

        public override Task ConnectAsync(IChannelHandlerContext context, EndPoint remoteAddress, EndPoint localAddress) => 
            this.encoder.ConnectAsync(context, remoteAddress, localAddress);

        public override Task DisconnectAsync(IChannelHandlerContext context) => this.encoder.DisconnectAsync(context);

        public override Task CloseAsync(IChannelHandlerContext context) => this.encoder.CloseAsync(context);

        public override Task DeregisterAsync(IChannelHandlerContext context) => this.encoder.DeregisterAsync(context);

        public override void Read(IChannelHandlerContext context) => this.encoder.Read(context);

        public override void Flush(IChannelHandlerContext context) => this.encoder.Flush(context);

        public override void HandlerAdded(IChannelHandlerContext context) => this.encoder.HandlerAdded(context);

        public override void HandlerRemoved(IChannelHandlerContext context) => this.encoder.HandlerRemoved(context);

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => 
            this.encoder.ExceptionCaught(context, exception);
    }
}

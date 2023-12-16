using System.Net;
using DotNetty.Buffers;
using DotNetty.Common;

namespace DotNetty.Transport.Channels.Sockets
{
    public sealed class DatagramPacket : DefaultAddressedEnvelope<IByteBuffer>, IByteBufferHolder
    {
        public DatagramPacket(IByteBuffer message, EndPoint recipient)
            : base(message, recipient)
        {
        }

        public DatagramPacket(IByteBuffer message, EndPoint sender, EndPoint recipient)
            : base(message, sender, recipient)
        {
        }

        public IByteBufferHolder Copy() => new DatagramPacket(this.Content.Copy(), this.Sender, this.Recipient);

        public IByteBufferHolder Duplicate() => new DatagramPacket(this.Content.Duplicate(), this.Sender, this.Recipient);

        public IByteBufferHolder RetainedDuplicate() => this.Replace(this.Content.RetainedDuplicate());

        public IByteBufferHolder Replace(IByteBuffer content) => new DatagramPacket(content, this.Recipient, this.Sender);

        public override IReferenceCounted Retain(int increment = 1)
        {
            base.Retain(increment);
            return this;
        }
    }
}
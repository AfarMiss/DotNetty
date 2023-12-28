using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public abstract class AbstractServerChannel : AbstractChannel, IServerChannel
    {
        private static readonly ChannelMetadata METADATA = new ChannelMetadata(false, 16);

        protected AbstractServerChannel() : base()
        {
        }

        public override ChannelMetadata Metadata => METADATA;
        protected override EndPoint RemoteAddressInternal => null;

        protected override void DoDisconnect() => throw new NotSupportedException();

        protected override IChannelUnsafe NewUnsafe() => new DefaultServerUnsafe(this);

        protected override void DoWrite(ChannelOutboundBuffer buf) => throw new NotSupportedException();

        protected override object FilterOutboundMessage(object msg) => throw new NotSupportedException();

        private sealed class DefaultServerUnsafe : AbstractUnsafe
        {
            private readonly Task error = TaskEx.FromException(new NotSupportedException());

            public DefaultServerUnsafe(AbstractChannel channel) : base(channel) { }

            public override Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress) => this.error;
        }
    }
}
using System;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    /// <summary>
    /// A skeletal server-side <see cref="IChannel"/> implementation. A server-side <see cref="IChannel"/> does not
    /// allow the following operations: <see cref="IChannel.ConnectAsync(EndPoint)"/>,
    /// <see cref="IChannel.DisconnectAsync()"/>, <see cref="IChannel.WriteAsync(object)"/>,
    /// <see cref="IChannel.Flush()"/>.
    /// </summary>
    public abstract class AbstractServerChannel : AbstractChannel, IServerChannel
    {
        private static readonly ChannelMetadata METADATA = new ChannelMetadata(false, 16);

        protected AbstractServerChannel() : base(null)
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
            private readonly Task err = TaskEx.FromException(new NotSupportedException());

            public DefaultServerUnsafe(AbstractChannel channel) : base(channel) { }

            public override Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress) => this.err;
        }
    }
}
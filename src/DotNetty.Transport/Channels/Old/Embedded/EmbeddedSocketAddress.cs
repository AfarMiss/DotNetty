using System.Net;

namespace DotNetty.Transport.Channels.Embedded
{
    internal sealed class EmbeddedSocketAddress : EndPoint
    {
        public override string ToString() => "embedded";
    }
}
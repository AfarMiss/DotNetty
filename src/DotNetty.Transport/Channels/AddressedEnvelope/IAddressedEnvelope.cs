using System.Net;
using DotNetty.Common;

namespace DotNetty.Transport.Channels
{
    public interface IAddressedEnvelope<out T> : IReferenceCounted
    {
        T Content { get; }
        EndPoint Sender { get; }
        EndPoint Recipient { get; }
    }
}
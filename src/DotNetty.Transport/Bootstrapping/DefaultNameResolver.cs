using System.Net;
using System.Threading.Tasks;

namespace DotNetty.Transport.Bootstrapping
{
    public class DefaultNameResolver : INameResolver
    {
        public bool IsResolved(EndPoint address) => !(address is DnsEndPoint);

        public async Task<EndPoint> ResolveAsync(EndPoint address)
        {
            if (!(address is DnsEndPoint asDns)) return address;
            
            var resolved = await Dns.GetHostEntryAsync(asDns.Host);
            return new IPEndPoint(resolved.AddressList[0], asDns.Port);
        }
    }
}
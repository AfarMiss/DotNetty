using System.Net;
using System.Threading.Tasks;

namespace DotNetty.Transport.Bootstrapping
{
    public interface INameResolver
    {
        bool IsResolved(EndPoint address);
        Task<EndPoint> ResolveAsync(EndPoint address);
    }
}
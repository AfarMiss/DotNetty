using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Pool
{
    public interface IChannelHealthChecker
    {
        ValueTask<bool> IsHealthyAsync(IChannel channel);
    }
}
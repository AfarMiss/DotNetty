using System;
using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Pool
{
    public interface IChannelPool : IDisposable
    {
        ValueTask<IChannel> AcquireAsync();
        ValueTask<bool> ReleaseAsync(IChannel channel);
    }
}
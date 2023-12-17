using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Pool
{
    public class ChannelActiveHealthChecker : IChannelHealthChecker
    {
        public static readonly IChannelHealthChecker Instance = new ChannelActiveHealthChecker();

        private ChannelActiveHealthChecker() { }

        public ValueTask<bool> IsHealthyAsync(IChannel channel) => new ValueTask<bool>(channel.Active);
    }
}
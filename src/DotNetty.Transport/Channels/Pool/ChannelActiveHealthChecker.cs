using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Pool
{
    public class ChannelActiveHealthChecker : IChannelHealthChecker
    {
        public static readonly IChannelHealthChecker Instance;

        static ChannelActiveHealthChecker()
        {
            Instance = new ChannelActiveHealthChecker();
        }

        ChannelActiveHealthChecker()
        {
        }

        public ValueTask<bool> IsHealthyAsync(IChannel channel) => new ValueTask<bool>(channel.Active);
    }
}
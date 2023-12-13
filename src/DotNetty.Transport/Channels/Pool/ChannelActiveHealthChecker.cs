using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Pool
{
    /// <summary>
    /// <see cref="IChannelHealthChecker"/> implementation that checks if <see cref="IChannel.Active"/> returns <c>true</c>.
    /// </summary>
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
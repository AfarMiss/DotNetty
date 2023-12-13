using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Groups
{
    public interface IChannelGroupTaskCompletionSource : IEnumerator<Task>
    {
        IChannelGroup Group { get; }

        ChannelGroupException Cause { get; }

        Task Find(IChannel channel);

        bool IsPartialSucess();

        bool IsSucess();

        bool IsPartialFailure();
    }
}
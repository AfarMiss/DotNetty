using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Groups
{
    public interface IChannelGroup : ICollection<IChannel>, IComparable<IChannelGroup>
    {
        string Name { get; }

        IChannel Find(IChannelId id);
        Task WriteAsync(object message, IChannelMatcher matcher);
        void Flush(IChannelMatcher matcher);
        Task WriteAndFlushAsync(object message, IChannelMatcher matcher = null);
        Task DisconnectAsync(IChannelMatcher matcher = null);
        Task CloseAsync(IChannelMatcher matcher = null);
        Task DeregisterAsync(IChannelMatcher matcher = null);
        Task NewCloseFuture(IChannelMatcher matcher = null);
    }
}
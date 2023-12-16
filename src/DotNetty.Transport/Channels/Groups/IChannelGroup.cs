using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetty.Transport.Channels.Groups
{
    public interface IChannelGroup : ICollection<IChannel>, IComparable<IChannelGroup>
    {
        string Name { get; }

        IChannel Find(IChannelId id);
        Task WriteAsync(object message);
        Task WriteAsync(object message, IChannelMatcher matcher);
        void Flush();
        void Flush(IChannelMatcher matcher);
        Task WriteAndFlushAsync(object message);
        Task WriteAndFlushAsync(object message, IChannelMatcher matcher);
        Task DisconnectAsync();
        Task DisconnectAsync(IChannelMatcher matcher);
        Task CloseAsync();
        Task CloseAsync(IChannelMatcher matcher);
        Task DeregisterAsync();
        Task DeregisterAsync(IChannelMatcher matcher);
        Task NewCloseFuture();
        Task NewCloseFuture(IChannelMatcher matcher);
    }
}
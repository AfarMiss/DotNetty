using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels.Groups
{
    public class DefaultChannelGroup : IChannelGroup
    {
        private static int nextId;
        private readonly IEventExecutor executor;
        private readonly ConcurrentDictionary<IChannelId, IChannel> channels = new ConcurrentDictionary<IChannelId, IChannel>();

        public string Name { get; }
        public int Count => this.channels.Count;
        public bool IsReadOnly => false;
        public bool IsEmpty => this.channels.Count == 0;

        public DefaultChannelGroup(IEventExecutor executor) : this($"group-{Interlocked.Increment(ref nextId):X2}", executor)
        {
        }

        public DefaultChannelGroup(string name, IEventExecutor executor)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.executor = executor;
        }

        public IChannel Find(IChannelId id)
        {
            this.channels.TryGetValue(id, out var channel);
            return channel;
        }

        public Task WriteAsync(object message) => this.WriteAsync(message, ChannelMatchers.All());

        public Task WriteAsync(object message, IChannelMatcher matcher)
        {
            Contract.Requires(message != null);
            Contract.Requires(matcher != null);
            var futures = new List<Task>();
            foreach (var channel in this.channels.Values)
            {
                // TODO 换个方式判定非IServerChannel
                if (!(channel is IServerChannel))
                {
                    if (matcher.Matches(channel))
                    {
                        futures.Add(channel.WriteAsync(SafeDuplicate(message)));
                    }
                }
            }

            ReferenceCountUtil.Release(message);
            return new DefaultChannelGroupCompletionSource(this, futures).Task;
        }

        public void Flush() => this.Flush(ChannelMatchers.All());

        public void Flush(IChannelMatcher matcher)
        {
            foreach (var channel in this.channels.Values)
            {
                if (matcher.Matches(channel))
                {
                    channel.Flush();
                }
            }
        }

        public Task WriteAndFlushAsync(object message) => this.WriteAndFlushAsync(message, ChannelMatchers.All());

        public Task WriteAndFlushAsync(object message, IChannelMatcher matcher)
        {
            Contract.Requires(message != null);
            Contract.Requires(matcher != null);
            var futures = new List<Task>();
            foreach (var channel in this.channels.Values)
            {
                if (!(channel is IServerChannel))
                {
                    if (matcher.Matches(channel))
                    {
                        futures.Add(channel.WriteAndFlushAsync(SafeDuplicate(message)));
                    }
                }
            }

            ReferenceCountUtil.Release(message);
            return new DefaultChannelGroupCompletionSource(this, futures).Task;
        }

        public Task DisconnectAsync() => this.DisconnectAsync(ChannelMatchers.All());

        public Task DisconnectAsync(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new List<Task>();
            foreach (var channel in this.channels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel.DisconnectAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures).Task;
        }

        public Task CloseAsync() => this.CloseAsync(ChannelMatchers.All());

        public Task CloseAsync(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new List<Task>();
            foreach (var channel in this.channels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel.CloseAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures).Task;
        }

        public Task DeregisterAsync() => this.DeregisterAsync(ChannelMatchers.All());

        public Task DeregisterAsync(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new List<Task>();
            foreach (var channel in this.channels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel.DeregisterAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures).Task;
        }

        public Task NewCloseFuture() => this.NewCloseFuture(ChannelMatchers.All());

        public Task NewCloseFuture(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new List<Task>();
            foreach (var channel in this.channels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel.CloseCompletion);
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures).Task;
        }

        private static object SafeDuplicate(object message)
        {
            if (message is IByteBuffer buffer)
            {
                return buffer.Duplicate().Retain();
            }

            if (message is IByteBufferHolder byteBufferHolder)
            {
                return byteBufferHolder.Duplicate().Retain();
            }

            return ReferenceCountUtil.Retain(message);
        }
        
        void ICollection<IChannel>.Add(IChannel item) => this.Add(item);

        public bool Add(IChannel channel)
        {
            bool added = this.channels.TryAdd(channel.Id, channel);
            if (added)
            {
                channel.CloseCompletion.ContinueWith(x => this.Remove(channel));
            }
            return added;
        }
        
        public void Clear() => this.channels.Clear();

        public bool Contains(IChannel item) => this.channels.TryGetValue(item!.Id, out var channel) && channel == item;

        public void CopyTo(IChannel[] array, int arrayIndex) => this.channels.Values.CopyTo(array, arrayIndex);

        public bool Remove(IChannel channel) => this.channels.TryRemove(channel!.Id, out _);

        public bool Remove(IChannelId channelId) => this.channels.TryRemove(channelId, out _);

        public int CompareTo(IChannelGroup other)
        {
            var compare = string.Compare(this.Name, other.Name, StringComparison.Ordinal);
            if (compare != 0) return compare;

            return this.GetHashCode() - other.GetHashCode();
        }
        
        public IEnumerator<IChannel> GetEnumerator() => this.channels.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.channels.Values.GetEnumerator();
    }
}
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
        private readonly ConcurrentDictionary<IChannelId, IChannel> nonServerChannels = new ConcurrentDictionary<IChannelId, IChannel>();
        private readonly ConcurrentDictionary<IChannelId, IChannel> serverChannels = new ConcurrentDictionary<IChannelId, IChannel>();

        public DefaultChannelGroup(IEventExecutor executor)
            : this($"group-{Interlocked.Increment(ref nextId):X2}", executor)
        {
        }

        public DefaultChannelGroup(string name, IEventExecutor executor)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.executor = executor;
        }

        public bool IsEmpty => this.serverChannels.Count == 0 && this.nonServerChannels.Count == 0;

        public string Name { get; }

        public IChannel Find(IChannelId id)
        {
            if (this.nonServerChannels.TryGetValue(id, out var channel))
            {
                return channel;
            }
            else
            {
                this.serverChannels.TryGetValue(id, out channel);
                return channel;
            }
        }

        public Task WriteAsync(object message) => this.WriteAsync(message, ChannelMatchers.All());

        public Task WriteAsync(object message, IChannelMatcher matcher)
        {
            Contract.Requires(message != null);
            Contract.Requires(matcher != null);
            var futures = new Dictionary<IChannel, Task>();
            foreach (var channel in this.nonServerChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.WriteAsync(SafeDuplicate(message)));
                }
            }

            ReferenceCountUtil.Release(message);
            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public IChannelGroup Flush(IChannelMatcher matcher)
        {
            foreach (var channel in this.nonServerChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    channel.Flush();
                }
            }
            return this;
        }

        public IChannelGroup Flush() => this.Flush(ChannelMatchers.All());

        public int CompareTo(IChannelGroup other)
        {
            int v = string.Compare(this.Name, other.Name, StringComparison.Ordinal);
            if (v != 0)
            {
                return v;
            }

            return this.GetHashCode() - other.GetHashCode();
        }

        void ICollection<IChannel>.Add(IChannel item) => this.Add(item);

        public void Clear()
        {
            this.serverChannels.Clear();
            this.nonServerChannels.Clear();
        }

        public bool Contains(IChannel item)
        {
            IChannel channel;
            if (item is IServerChannel)
            {
                return this.serverChannels.TryGetValue(item.Id, out channel) && channel == item;
            }
            else
            {
                return this.nonServerChannels.TryGetValue(item.Id, out channel) && channel == item;
            }
        }

        public void CopyTo(IChannel[] array, int arrayIndex) => this.ToArray().CopyTo(array, arrayIndex);

        public int Count => this.nonServerChannels.Count + this.serverChannels.Count;

        public bool IsReadOnly => false;

        public bool Remove(IChannel channel)
        {
            if (channel is IServerChannel)
            {
                return this.serverChannels.TryRemove(channel.Id, out _);
            }
            else
            {
                return this.nonServerChannels.TryRemove(channel.Id, out _);
            }
        }

        public IEnumerator<IChannel> GetEnumerator() => new CombinedEnumerator<IChannel>(this.serverChannels.Values.GetEnumerator(),
            this.nonServerChannels.Values.GetEnumerator());

        IEnumerator IEnumerable.GetEnumerator() => new CombinedEnumerator<IChannel>(this.serverChannels.Values.GetEnumerator(),
            this.nonServerChannels.Values.GetEnumerator());

        public Task WriteAndFlushAsync(object message) => this.WriteAndFlushAsync(message, ChannelMatchers.All());

        public Task WriteAndFlushAsync(object message, IChannelMatcher matcher)
        {
            Contract.Requires(message != null);
            Contract.Requires(matcher != null);
            var futures = new Dictionary<IChannel, Task>();
            foreach (var channel in this.nonServerChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.WriteAndFlushAsync(SafeDuplicate(message)));
                }
            }

            ReferenceCountUtil.Release(message);
            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task DisconnectAsync() => this.DisconnectAsync(ChannelMatchers.All());

        public Task DisconnectAsync(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new Dictionary<IChannel, Task>();
            foreach (var channel in this.nonServerChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.DisconnectAsync());
                }
            }
            foreach (var channel in this.serverChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.DisconnectAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task CloseAsync() => this.CloseAsync(ChannelMatchers.All());

        public Task CloseAsync(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new Dictionary<IChannel, Task>();
            foreach (var channel in this.nonServerChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.CloseAsync());
                }
            }
            foreach (var channel in this.serverChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.CloseAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task DeregisterAsync() => this.DeregisterAsync(ChannelMatchers.All());

        public Task DeregisterAsync(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new Dictionary<IChannel, Task>();
            foreach (var channel in this.nonServerChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.DeregisterAsync());
                }
            }
            foreach (var channel in this.serverChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.DeregisterAsync());
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
        }

        public Task NewCloseFuture() => this.NewCloseFuture(ChannelMatchers.All());

        public Task NewCloseFuture(IChannelMatcher matcher)
        {
            Contract.Requires(matcher != null);
            var futures = new Dictionary<IChannel, Task>();
            foreach (var channel in this.nonServerChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.CloseCompletion);
                }
            }
            foreach (var channel in this.serverChannels.Values)
            {
                if (matcher.Matches(channel))
                {
                    futures.Add(channel, channel.CloseCompletion);
                }
            }

            return new DefaultChannelGroupCompletionSource(this, futures /*, this.executor*/).Task;
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

        public override string ToString() => $"{this.GetType().Name}(name: {this.Name}, size: {this.Count})";

        public bool Add(IChannel channel)
        {
            var map = channel is IServerChannel ? this.serverChannels : this.nonServerChannels;
            bool added = map.TryAdd(channel.Id, channel);
            if (added)
            {
                channel.CloseCompletion.ContinueWith(x => this.Remove(channel));
            }
            return added;
        }

        public IChannel[] ToArray()
        {
            var channels = new List<IChannel>(this.Count);
            channels.AddRange(this.serverChannels.Values);
            channels.AddRange(this.nonServerChannels.Values);
            return channels.ToArray();
        }

        public bool Remove(IChannelId channelId)
        {
            if (this.serverChannels.TryRemove(channelId, out _))
            {
                return true;
            }

            if (this.nonServerChannels.TryRemove(channelId, out _))
            {
                return true;
            }

            return false;
        }

        public bool Remove(object o)
        {
            if (o is IChannelId id)
            {
                return this.Remove(id);
            }

            if (o is IChannel channel)
            {
                return this.Remove(channel);
            }
            return false;
        }
    }
}
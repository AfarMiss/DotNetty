using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using DotNetty.Common.Internal;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Bootstrapping;

namespace DotNetty.Transport.Channels.Pool
{
    public class SimpleChannelPool : IChannelPool
    {
        public static readonly AttributeKey<SimpleChannelPool> PoolKey = AttributeKey<SimpleChannelPool>.NewInstance("channelPool");
        private static readonly InvalidOperationException FullException = new InvalidOperationException("ChannelPool full");
        private readonly IQueue<IChannel> store;

        public SimpleChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler)
            : this(bootstrap, handler, ChannelActiveHealthChecker.Instance)
        {
        }

        public SimpleChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler, IChannelHealthChecker healthChecker)
            : this(bootstrap, handler, healthChecker, true)
        {
        }

        public SimpleChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler, IChannelHealthChecker healthChecker, bool releaseHealthCheck)
            : this(bootstrap, handler, healthChecker, releaseHealthCheck, true)
        {
        }

        public SimpleChannelPool(Bootstrap bootstrap, IChannelPoolHandler handler, IChannelHealthChecker healthChecker, bool releaseHealthCheck, bool lastRecentUsed)
        {
            Contract.Requires(handler != null);
            Contract.Requires(healthChecker != null);
            Contract.Requires(bootstrap != null);

            this.Handler = handler;
            this.HealthChecker = healthChecker;
            this.ReleaseHealthCheck = releaseHealthCheck;

            // Clone the original Bootstrap as we want to set our own handler
            this.Bootstrap = bootstrap.Clone();
            this.Bootstrap.SetHandler(new ActionChannelInitializer<IChannel>(this.OnChannelInitializing));
            this.store =
                lastRecentUsed
                    ? (IQueue<IChannel>)new CompatibleConcurrentStack<IChannel>()
                    : new CompatibleConcurrentQueue<IChannel>();
        }

        private void OnChannelInitializing(IChannel channel)
        {
            Contract.Assert(channel.EventLoop.InEventLoop);
            this.Handler.ChannelCreated(channel);
        }

        internal Bootstrap Bootstrap { get; }

        internal IChannelPoolHandler Handler { get; }

        internal IChannelHealthChecker HealthChecker { get; }

        internal bool ReleaseHealthCheck { get; }

        public virtual ValueTask<IChannel> AcquireAsync()
        {
            if (!this.TryPollChannel(out var channel))
            {
                var clone = this.Bootstrap.Clone();
                clone.Attribute(PoolKey, this);
                return new ValueTask<IChannel>(this.ConnectChannel(clone));
            }
            
            IEventLoop eventLoop = channel.EventLoop;
            if (eventLoop.InEventLoop)
            {
                return this.DoHealthCheck(channel);
            }
            else
            {
                var completionSource = new TaskCompletionSource<IChannel>();
                eventLoop.Execute(this.DoHealthCheck, channel, completionSource);
                return new ValueTask<IChannel>(completionSource.Task);    
            }
        }

        private async void DoHealthCheck(object channel, object state)
        {
            var promise = state as TaskCompletionSource<IChannel>;
            try
            {
                var result = await this.DoHealthCheck((IChannel)channel);
                promise.TrySetResult(result);
            }
            catch (Exception ex)
            {
                promise.TrySetException(ex);
            }
        }

        private async ValueTask<IChannel> DoHealthCheck(IChannel channel)
        {
            Contract.Assert(channel.EventLoop.InEventLoop);
            try
            {
                if (await this.HealthChecker.IsHealthyAsync(channel))
                {
                    try
                    {
                        channel.ConstantMap.Set(PoolKey, this);
                        this.Handler.ChannelAcquired(channel);
                        return channel;
                    }
                    catch (Exception)
                    {
                        CloseChannel(channel);
                        throw;
                    }
                }
                else
                {
                    CloseChannel(channel);
                    return await this.AcquireAsync();
                }
            }
            catch
            {
                CloseChannel(channel);
                return await this.AcquireAsync();
            }
        }

        protected virtual Task<IChannel> ConnectChannel(Bootstrap bs) => bs.ConnectAsync();

        public virtual async ValueTask<bool> ReleaseAsync(IChannel channel)
        {
            Contract.Requires(channel != null);
            try
            {
                IEventLoop loop = channel.EventLoop;
                if (loop.InEventLoop)
                {
                    return await this.DoReleaseChannel(channel);
                }
                else
                {
                    var promise = new TaskCompletionSource<bool>();
                    loop.Execute(this.DoReleaseChannel, channel, promise);
                    return await promise.Task;
                }
            }
            catch (Exception)
            {
                CloseChannel(channel);
                throw;
            }
        }

        private async void DoReleaseChannel(object channel, object state)
        {
            var promise = state as TaskCompletionSource<bool>;
            try
            {
                var result = await this.DoReleaseChannel((IChannel)channel);
                promise.TrySetResult(result);
            }
            catch (Exception ex)
            {
                promise.TrySetException(ex);
            }
        }

        private async ValueTask<bool> DoReleaseChannel(IChannel channel)
        {
            Contract.Assert(channel.EventLoop.InEventLoop);

            // Remove the POOL_KEY attribute from the Channel and check if it was acquired from this pool, if not fail.
            var channelPool = channel.ConstantMap.Get(PoolKey);
            channel.ConstantMap.Set(PoolKey, null);
            if (channelPool != this)
            {
                CloseChannel(channel);
                // Better include a stacktrace here as this is an user error.
                throw new ArgumentException($"Channel {channel} was not acquired from this ChannelPool");
            }
            else
            {
                try
                {
                    if (this.ReleaseHealthCheck)
                    {
                        return await this.DoHealthCheckOnRelease(channel);
                    }
                    else
                    {
                        this.ReleaseAndOffer(channel);
                        return true;
                    }
                }
                catch
                {
                    CloseChannel(channel);
                    throw;
                }
            }
        }

        private async ValueTask<bool> DoHealthCheckOnRelease(IChannel channel)
        {
            if (await this.HealthChecker.IsHealthyAsync(channel))
            {
                //channel turns out to be healthy, offering and releasing it.
                this.ReleaseAndOffer(channel);
                return true;
            }
            else
            {
                //channel not healthy, just releasing it.
                this.Handler.ChannelReleased(channel);
                return false;
            }
        }

        private void ReleaseAndOffer(IChannel channel)
        {
            if (this.TryOfferChannel(channel))
            {
                this.Handler.ChannelReleased(channel);
            }
            else
            {
                CloseChannel(channel);
                throw FullException;
            }
        }

        private static void CloseChannel(IChannel channel)
        {
            channel.ConstantMap.Set(PoolKey, null);
            channel.CloseAsync();
        }

        protected virtual bool TryPollChannel(out IChannel channel) => this.store.TryDequeue(out channel);

        protected virtual bool TryOfferChannel(IChannel channel) => this.store.TryEnqueue(channel);

        public virtual void Dispose()
        {
            while (this.TryPollChannel(out IChannel channel))
            {
                channel.CloseAsync();
            }
        }

        private class CompatibleConcurrentStack<T> : ConcurrentStack<T>, IQueue<T>
        {
            public bool TryEnqueue(T item)
            {
                this.Push(item);
                return true;
            }

            public bool TryDequeue(out T item) => this.TryPop(out item);
        }
    }
}
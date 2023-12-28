using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Bootstrapping
{
    /// <summary> 客户端引导 </summary>
    public class Bootstrap : AbstractBootstrap<Bootstrap, IChannel>
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Bootstrap>();
        private static readonly INameResolver DefaultResolver = new DefaultNameResolver();
        private volatile INameResolver resolver = DefaultResolver;
        private volatile EndPoint remoteAddress;

        public Bootstrap() { }

        private Bootstrap(Bootstrap bootstrap) : base(bootstrap)
        {
            this.resolver = bootstrap.resolver;
            this.remoteAddress = bootstrap.remoteAddress;
        }

        public void Resolver(INameResolver resolver)
        {
            Contract.Requires(resolver != null);
            this.resolver = resolver;
        }

        public Bootstrap RemoteAddress(EndPoint remoteAddress)
        {
            this.remoteAddress = remoteAddress;
            return this;
        }

        public Task<IChannel> ConnectAsync()
        {
            this.Validate();
            var remoteAddress = this.remoteAddress;
            if (remoteAddress == null)
            {
                throw new InvalidOperationException("remoteAddress not set");
            }

            return this.DoResolveAndConnectAsync(remoteAddress, this.LocalAddress());
        }

        public Task<IChannel> ConnectAsync(EndPoint remoteAddress)
        {
            this.Validate();
            return this.DoResolveAndConnectAsync(remoteAddress, this.LocalAddress());
        }

        public Task<IChannel> ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            Contract.Requires(remoteAddress != null);

            this.Validate();
            return this.DoResolveAndConnectAsync(remoteAddress, localAddress);
        }

        /// <summary>
        /// EndPoint DNS解析并连接
        /// </summary>
        private async Task<IChannel> DoResolveAndConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            var channel = await this.InitAndRegisterAsync();

            if (this.resolver.IsResolved(remoteAddress))
            {
                await DoConnectAsync(channel, remoteAddress, localAddress);
                return channel;
            }

            EndPoint resolvedAddress;
            try
            {
                resolvedAddress = await this.resolver.ResolveAsync(remoteAddress);
            }
            catch (Exception)
            {
                try
                {
                    await channel.CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to close channel: " + channel, ex);
                }

                throw;
            }

            await DoConnectAsync(channel, resolvedAddress, localAddress);
            return channel;
        }

        private static Task DoConnectAsync(IChannel channel, EndPoint remoteAddress, EndPoint localAddress)
        {
            var promise = new TaskCompletionSource();
            channel.EventLoop.Execute(() =>
            {
                try
                {
                    if (localAddress == null)
                    {
                        channel.ConnectAsync(remoteAddress).LinkOutcome(promise);
                    }
                    else
                    {
                        channel.ConnectAsync(remoteAddress, localAddress).LinkOutcome(promise);
                    }
                }
                catch (Exception ex)
                {
                    channel.CloseSafe();
                    promise.TrySetException(ex);
                }
            });
            return promise.Task;
        }

        protected override void Init(IChannel channel)
        {
            var pipeline = channel.Pipeline;
            pipeline.AddLast(null, (string)null, this.Handler);

            foreach (var (_, accessor) in this.Options)
            {
                accessor.TransferSet(channel.Configuration);
            }
            foreach (var (_, accessor) in this.Attrs)
            {
                accessor.TransferSet(channel);
            }
        }

        protected override void Validate()
        {
            base.Validate();
            if (this.Handler == null)
            {
                throw new InvalidOperationException($"{this.Handler}未设置!");
            }
        }

        public override Bootstrap Clone() => new Bootstrap(this);

        public Bootstrap Clone(IEventLoopGroup group)
        {
            var bs = new Bootstrap(this);
            bs.SetGroup(group);
            return bs;
        }
    }
}
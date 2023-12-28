using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Net;
using System.Threading.Tasks;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Bootstrapping
{
    /// <summary> 引导基类 </summary>
    public abstract class AbstractBootstrap<TBootstrap, TChannel> where TBootstrap : AbstractBootstrap<TBootstrap, TChannel> where TChannel : IChannel
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AbstractBootstrap<TBootstrap, TChannel>>();

        private volatile IEventLoopGroup group;
        private volatile Func<TChannel> channelFactory;
        private volatile EndPoint localAddress;
        protected readonly ConstantMap Options;
        protected readonly ConstantMap Attrs;
        private volatile IChannelHandler handler;

        protected EndPoint LocalAddress() => this.localAddress;
        
        protected IChannelHandler Handler => this.handler;
        public IEventLoopGroup Group => this.group;
        
        protected abstract void Init(IChannel channel);

        protected internal AbstractBootstrap()
        {
            this.Options = new ConstantMap();
            this.Attrs = new ConstantMap();
        }

        protected internal AbstractBootstrap(AbstractBootstrap<TBootstrap, TChannel> bootstrap)
        {
            this.group = bootstrap.group;
            this.channelFactory = bootstrap.channelFactory;
            this.handler = bootstrap.handler;
            this.localAddress = bootstrap.localAddress;
            this.Options = new ConstantMap(bootstrap.Options);
            this.Attrs = new ConstantMap(bootstrap.Attrs);
        }

        /// <summary> 指定<see cref="IEventLoopGroup"/>处理<see cref="IChannel"/>事件 </summary>
        public virtual TBootstrap SetGroup(IEventLoopGroup group)
        {
            if (this.group != null)
            {
                throw new InvalidOperationException($"重复设置{nameof(IEventLoopGroup)}");
            }
            this.group = group;
            return (TBootstrap)this;
        }

        /// <summary> SocketChannel </summary>
        public TBootstrap Channel<T>() where T : TChannel, new() => this.Channel(() => new T());

        public TBootstrap Channel(Func<TChannel> channelFactory)
        {
            Contract.Requires(channelFactory != null);
            this.channelFactory = channelFactory;
            return (TBootstrap)this;
        }

        public TBootstrap SetHandler(IChannelHandler handler)
        {
            Contract.Requires(handler != null);
            this.handler = handler;
            return (TBootstrap)this;
        }
        
        public TBootstrap LocalAddress(EndPoint localAddress)
        {
            this.localAddress = localAddress;
            return (TBootstrap)this;
        }

        public TBootstrap LocalAddress(int inetPort) => this.LocalAddress(new IPEndPoint(IPAddress.Any, inetPort));
        public TBootstrap LocalAddress(string inetHost, int inetPort) => this.LocalAddress(new DnsEndPoint(inetHost, inetPort));
        public TBootstrap LocalAddress(IPAddress inetHost, int inetPort) => this.LocalAddress(new IPEndPoint(inetHost, inetPort));

        public TBootstrap Option<T>(ChannelOption<T> option, T value)
        {
            if (value == null)
            {
                this.Options.Remove(option);
            }
            else
            {
                this.Options.Set(option, value);
            }
            return (TBootstrap)this;
        }

        public void Attribute<T>(AttributeKey<T> key, T value) where T : class
        {
            Contract.Requires(key != null);

            if (value == null)
            {
                this.Attrs.Remove(key);
            }
            else
            {
                this.Attrs.Set(key, value);
            }
        }

        protected virtual void Validate()
        {
            if (this.group == null) throw new InvalidOperationException("Group 未设置");
            if (this.channelFactory == null) throw new InvalidOperationException("Channel 未设置");
        }

        /// <summary> 非全克隆 </summary>
        public abstract TBootstrap Clone();

        /// <summary> 创建<see cref="IChannel"/>并注册到<see cref="IEventLoop"/> </summary>
        public Task RegisterAsync()
        {
            this.Validate();
            return this.InitAndRegisterAsync();
        }

        /// <inheritdoc cref="BindAsync(EndPoint)"/>
        public Task<IChannel> BindAsync(int inetPort) => this.BindAsync(new IPEndPoint(IPAddress.Any, inetPort));

        /// <summary>
        /// 参考<see cref="RegisterAsync"/> 并绑定到指定EndPoint
        /// </summary>
        public Task<IChannel> BindAsync(EndPoint localAddress)
        {
            this.Validate();
            return this.DoBindAsync(localAddress);
        }

        private async Task<IChannel> DoBindAsync(EndPoint localAddress)
        {
            var channel = await this.InitAndRegisterAsync();
            await DoBind0Async(channel, localAddress);
            return channel;
        }

        protected async Task<IChannel> InitAndRegisterAsync()
        {
            IChannel channel = this.channelFactory();
            try
            {
                this.Init(channel);
            }
            catch (Exception)
            {
                channel.Unsafe.CloseForcibly();
                throw;
            }

            try
            {
                await this.Group.GetNext().RegisterAsync(channel);
            }
            catch (Exception)
            {
                if (channel.Registered)
                {
                    try
                    {
                        await channel.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                       Logger.Warn("Failed to close channel: " + channel, ex);
                    }
                }
                else
                {
                    channel.Unsafe.CloseForcibly();
                }
                throw;
            }

            return channel;
        }

        private static Task DoBind0Async(IChannel channel, EndPoint localAddress)
        {
            var promise = new TaskCompletionSource();
            channel.EventLoop.Execute(() =>
            {
                try
                {
                    channel.BindAsync(localAddress).LinkOutcome(promise);
                }
                catch (Exception ex)
                {
                    channel.CloseSafe();
                    promise.TrySetException(ex);
                }
            });
            return promise.Task;
        }
    }
}
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
        protected readonly ConstantMap options;
        protected readonly ConstantMap attrs;
        private volatile IChannelHandler handler;

        protected EndPoint LocalAddress() => this.localAddress;
        
        protected IChannelHandler Handler => this.handler;
        public IEventLoopGroup Group => this.group;
        
        protected abstract void Init(IChannel channel);

        protected internal AbstractBootstrap()
        {
            this.options = new ConstantMap();
            this.attrs = new ConstantMap();
        }

        protected internal AbstractBootstrap(AbstractBootstrap<TBootstrap, TChannel> bootstrap)
        {
            this.group = bootstrap.group;
            this.channelFactory = bootstrap.channelFactory;
            this.handler = bootstrap.handler;
            this.localAddress = bootstrap.localAddress;
            this.options = new ConstantMap(bootstrap.options);
            this.attrs = new ConstantMap(bootstrap.attrs);
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
                this.options.DelConstant(option);
            }
            else
            {
                this.options.SetConstant(option, value);
            }
            return (TBootstrap)this;
        }

        public void Attribute<T>(AttributeKey<T> key, T value) where T : class
        {
            Contract.Requires(key != null);

            if (value == null)
            {
                this.attrs.DelConstant(key);
            }
            else
            {
                this.attrs.SetConstant(key, value);
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
        public Task<IChannel> BindAsync()
        {
            this.Validate();
            var address = this.localAddress;
            if (address == null)
            {
                throw new InvalidOperationException("localAddress must be set beforehand.");
            }
            return this.DoBindAsync(address);
        }

        /// <inheritdoc cref="BindAsync(EndPoint)"/>
        public Task<IChannel> BindAsync(int inetPort) => this.BindAsync(new IPEndPoint(IPAddress.Any, inetPort));

        /// <inheritdoc cref="BindAsync(EndPoint)"/>
        public Task<IChannel> BindAsync(string inetHost, int inetPort) => this.BindAsync(new DnsEndPoint(inetHost, inetPort));

        /// <inheritdoc cref="BindAsync(EndPoint)"/>
        public Task<IChannel> BindAsync(IPAddress inetHost, int inetPort) => this.BindAsync(new IPEndPoint(inetHost, inetPort));

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
                // as the Channel is not registered yet we need to force the usage of the GlobalEventExecutor
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

            // If we are here and the promise is not failed, it's one of the following cases:
            // 1) If we attempted registration from the event loop, the registration has been completed at this point.
            //    i.e. It's safe to attempt bind() or connect() now because the channel has been registered.
            // 2) If we attempted registration from the other thread, the registration request has been successfully
            //    added to the event loop's task queue for later execution.
            //    i.e. It's safe to attempt bind() or connect() now:
            //         because bind() or connect() will be executed *after* the scheduled registration task is executed
            //         because register(), bind(), and connect() are all bound to the same thread.

            return channel;
        }

        private static Task DoBind0Async(IChannel channel, EndPoint localAddress)
        {
            // This method is invoked before channelRegistered() is triggered.  Give user handlers a chance to set up
            // the pipeline in its channelRegistered() implementation.
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

        protected static void SetChannelOptions(IChannel channel, ICollection<IConstantAccessor> options, IInternalLogger logger)
        {
            foreach (var e in options)
            {
                SetChannelOption(channel, e, logger);
            }
        }

        protected static void SetChannelOptions(IChannel channel, ConstantMap options, IInternalLogger logger)
        {
            foreach (var (_, accessor) in options)
            {
                SetChannelOption(channel, accessor, logger);
            }
        }

        protected static void SetChannelOption(IChannel channel, IConstantAccessor option, IInternalLogger logger)
        {
            try
            {
                if (!option.TransferSet(channel.Configuration))
                {
                    // logger.Warn("Unknown channel option '{}' for channel '{}'", option.Option, channel);
                }
            }
            catch (Exception ex)
            {
                // logger.Warn("Failed to set channel option '{}' with value '{}' for channel '{}'", option.Option, option, channel, ex);
            }
        }

        // protected abstract class ChannelOptionValue
        // {
        //     public abstract IConstant Option { get; }
        //     public abstract bool Set(IChannelConfiguration config);
        // }
        //
        // protected sealed class ChannelOptionValue<T> : ChannelOptionValue
        // {
        //     public override IConstant Option { get; }
        //     private readonly T value;
        //
        //     public ChannelOptionValue(ChannelOption<T> option, T value)
        //     {
        //         this.Option = option;
        //         this.value = value;
        //     }
        //
        //     public override bool Set(IChannelConfiguration config) => config.SetOption((ChannelOption<T>)this.Option, this.value);
        //
        //     public override string ToString() => this.value.ToString();
        // }
        //
        // protected abstract class AttributeValue
        // {
        //     public abstract void Set(IAttributeMap map);
        // }
        //
        // protected sealed class AttributeValue<T> : AttributeValue where T : class
        // {
        //     private readonly AttributeKey<T> key;
        //     private readonly T value;
        //
        //     public AttributeValue(AttributeKey<T> key, T value)
        //     {
        //         this.key = key;
        //         this.value = value;
        //     }
        //
        //     public override void Set(IAttributeMap config) => config.GetAttribute(this.key).Set(this.value);
        // }
    }
}
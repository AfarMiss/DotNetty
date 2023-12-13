using System;
using System.Collections.Concurrent;
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
    /// <summary>
    /// This is a helper class that makes it easy to bootstrap an <see cref="IChannel"/>. It supports method-
    /// chaining to provide an easy way to configure the <see cref="AbstractBootstrap{TBootstrap,TChannel}"/>.
    /// 
    /// When not used in a <see cref="ServerBootstrap"/> context, the <see cref="BindAsync(EndPoint)"/> methods
    /// are useful for connectionless transports such as datagram (UDP).
    /// </summary>
    public abstract class AbstractBootstrap<TBootstrap, TChannel> where TBootstrap : AbstractBootstrap<TBootstrap, TChannel> where TChannel : IChannel
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AbstractBootstrap<TBootstrap, TChannel>>();

        private volatile IEventLoopGroup group;
        private volatile Func<TChannel> channelFactory;
        private volatile EndPoint localAddress;
        private readonly ConcurrentDictionary<ChannelOption, ChannelOptionValue> options;
        private readonly ConcurrentDictionary<IConstant, AttributeValue> attrs;
        private volatile IChannelHandler handler;

        protected ICollection<ChannelOptionValue> Options => this.options.Values;
        protected ICollection<AttributeValue> Attributes => this.attrs.Values;
        protected EndPoint LocalAddress() => this.localAddress;
        
        protected IChannelHandler Handler => this.handler;
        public IEventLoopGroup Group => this.group;
        
        protected internal AbstractBootstrap()
        {
            this.options = new ConcurrentDictionary<ChannelOption, ChannelOptionValue>();
            this.attrs = new ConcurrentDictionary<IConstant, AttributeValue>();
            // Disallow extending from a different package.
        }

        protected internal AbstractBootstrap(AbstractBootstrap<TBootstrap, TChannel> bootstrap)
        {
            this.group = bootstrap.group;
            this.channelFactory = bootstrap.channelFactory;
            this.handler = bootstrap.handler;
            this.localAddress = bootstrap.localAddress;
            this.options = new ConcurrentDictionary<ChannelOption, ChannelOptionValue>(bootstrap.options);
            this.attrs = new ConcurrentDictionary<IConstant, AttributeValue>(bootstrap.attrs);
        }

        /// <summary>
        /// Specifies the <see cref="IEventLoopGroup"/> which will handle events for the <see cref="IChannel"/> being built.
        /// </summary>
        /// <param name="group">The <see cref="IEventLoopGroup"/> which is used to handle all the events for the to-be-created <see cref="IChannel"/>.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TChannel}"/> instance.</returns>
        public virtual TBootstrap SetGroup(IEventLoopGroup group)
        {
            Contract.Requires(group != null);

            if (this.group != null)
            {
                throw new InvalidOperationException("group has already been set.");
            }
            this.group = group;
            return (TBootstrap)this;
        }

        /// <summary>
        /// Specifies the <see cref="Type"/> of <see cref="IChannel"/> which will be created.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> which is used to create <see cref="IChannel"/> instances from.</typeparam>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TChannel}"/> instance.</returns>
        public TBootstrap Channel<T>() where T : TChannel, new() => this.ChannelFactory(() => new T());

        public TBootstrap ChannelFactory(Func<TChannel> channelFactory)
        {
            Contract.Requires(channelFactory != null);
            this.channelFactory = channelFactory;
            return (TBootstrap)this;
        }

        /// <summary>
        /// Assigns the <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// </summary>
        /// <param name="localAddress">The <see cref="EndPoint"/> instance to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TChannel}"/> instance.</returns>
        public TBootstrap LocalAddress(EndPoint localAddress)
        {
            this.localAddress = localAddress;
            return (TBootstrap)this;
        }

        /// <summary>
        /// Assigns the local <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// This overload binds to a <see cref="IPEndPoint"/> for any IP address on the local machine, given a specific port.
        /// </summary>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TChannel}"/> instance.</returns>
        public TBootstrap LocalAddress(int inetPort) => this.LocalAddress(new IPEndPoint(IPAddress.Any, inetPort));

        /// <summary>
        /// Assigns the local <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// This overload binds to a <see cref="DnsEndPoint"/> for a given hostname and port.
        /// </summary>
        /// <param name="inetHost">The hostname to bind the local "end" to.</param>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TChannel}"/> instance.</returns>
        public TBootstrap LocalAddress(string inetHost, int inetPort) => this.LocalAddress(new DnsEndPoint(inetHost, inetPort));

        /// <summary>
        /// Assigns the local <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// </summary>
        /// <param name="inetHost">The <see cref="IPAddress"/> to bind the local "end" to.</param>
        /// This overload binds to a <see cref="IPEndPoint"/> for a given <see cref="IPAddress"/> and port.
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TChannel}"/> instance.</returns>
        public TBootstrap LocalAddress(IPAddress inetHost, int inetPort) => this.LocalAddress(new IPEndPoint(inetHost, inetPort));

        /// <summary>
        /// Allows the specification of a <see cref="ChannelOption{T}"/> which is used for the
        /// <see cref="IChannel"/> instances once they get created. Use a value of <c>null</c> to remove
        /// a previously set <see cref="ChannelOption{T}"/>.
        /// </summary>
        /// <param name="option">The <see cref="ChannelOption{T}"/> to configure.</param>
        /// <param name="value">The value to set the given option.</param>
        public TBootstrap Option<T>(ChannelOption<T> option, T value)
        {
            Contract.Requires(option != null);

            if (value == null)
            {
                ChannelOptionValue removed;
                this.options.TryRemove(option, out removed);
            }
            else
            {
                this.options[option] = new ChannelOptionValue<T>(option, value);
            }
            return (TBootstrap)this;
        }

        /// <summary>
        /// Allows specification of an initial attribute of the newly created <see cref="IChannel" />. If the <c>value</c> is
        /// <c>null</c>, the attribute of the specified <c>key</c> is removed.
        /// </summary>
        public void Attribute<T>(AttributeKey<T> key, T value) where T : class
        {
            Contract.Requires(key != null);

            if (value == null)
            {
                this.attrs.TryRemove(key, out _);
            }
            else
            {
                this.attrs[key] = new AttributeValue<T>(key, value);
            }
        }

        /// <summary>
        /// Validates all the parameters. Sub-classes may override this, but should call the super method in that case.
        /// </summary>
        public virtual void Validate()
        {
            if (this.group == null)
            {
                throw new InvalidOperationException("group not set");
            }
            if (this.channelFactory == null)
            {
                throw new InvalidOperationException("channel or channelFactory not set");
            }
        }

        /// <summary>
        /// Returns a deep clone of this bootstrap which has the identical configuration.  This method is useful when making
        /// multiple <see cref="IChannel"/>s with similar settings.  Please note that this method does not clone the
        /// <see cref="IEventLoopGroup"/> deeply but shallowly, making the group a shared resource.
        /// </summary>
        public abstract TBootstrap Clone();

        /// <summary>
        /// Creates a new <see cref="IChannel"/> and registers it with an <see cref="IEventLoop"/>.
        /// </summary>
        public Task RegisterAsync()
        {
            this.Validate();
            return this.InitAndRegisterAsync();
        }

        /// <summary>
        /// Creates a new <see cref="IChannel"/> and binds it to the endpoint specified via the <see cref="LocalAddress(EndPoint)"/> methods.
        /// </summary>
        /// <returns>The bound <see cref="IChannel"/>.</returns>
        public Task<IChannel> BindAsync()
        {
            this.Validate();
            EndPoint address = this.localAddress;
            if (address == null)
            {
                throw new InvalidOperationException("localAddress must be set beforehand.");
            }
            return this.DoBindAsync(address);
        }

        /// <summary>
        /// Creates a new <see cref="IChannel"/> and binds it.
        /// This overload binds to a <see cref="IPEndPoint"/> for any IP address on the local machine, given a specific port.
        /// </summary>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IChannel"/>.</returns>
        public Task<IChannel> BindAsync(int inetPort) => this.BindAsync(new IPEndPoint(IPAddress.Any, inetPort));

        /// <summary>
        /// Creates a new <see cref="IChannel"/> and binds it.
        /// This overload binds to a <see cref="DnsEndPoint"/> for a given hostname and port.
        /// </summary>
        /// <param name="inetHost">The hostname to bind the local "end" to.</param>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IChannel"/>.</returns>
        public Task<IChannel> BindAsync(string inetHost, int inetPort) => this.BindAsync(new DnsEndPoint(inetHost, inetPort));

        /// <summary>
        /// Creates a new <see cref="IChannel"/> and binds it.
        /// This overload binds to a <see cref="IPEndPoint"/> for a given <see cref="IPAddress"/> and port.
        /// </summary>
        /// <param name="inetHost">The <see cref="IPAddress"/> to bind the local "end" to.</param>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IChannel"/>.</returns>
        public Task<IChannel> BindAsync(IPAddress inetHost, int inetPort) => this.BindAsync(new IPEndPoint(inetHost, inetPort));

        /// <summary>
        /// Creates a new <see cref="IChannel"/> and binds it.
        /// </summary>
        /// <param name="localAddress">The <see cref="EndPoint"/> instance to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IChannel"/>.</returns>
        public Task<IChannel> BindAsync(EndPoint localAddress)
        {
            this.Validate();
            Contract.Requires(localAddress != null);

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

        protected abstract void Init(IChannel channel);

        /// <summary>
        /// Specifies the <see cref="IChannelHandler"/> to use for serving the requests.
        /// </summary>
        /// <param name="handler">The <see cref="IChannelHandler"/> to use for serving requests.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TChannel}"/> instance.</returns>
        public TBootstrap SetHandler(IChannelHandler handler)
        {
            Contract.Requires(handler != null);
            this.handler = handler;
            return (TBootstrap)this;
        }

        protected static void SetChannelOptions(IChannel channel, ICollection<ChannelOptionValue> options, IInternalLogger logger)
        {
            foreach (var e in options)
            {
                SetChannelOption(channel, e, logger);
            }
        }

        protected static void SetChannelOptions(IChannel channel, ChannelOptionValue[] options, IInternalLogger logger)
        {
            foreach (var e in options)
            {
                SetChannelOption(channel, e, logger);
            }
        }

        protected static void SetChannelOption(IChannel channel, ChannelOptionValue option, IInternalLogger logger)
        {
            try
            {
                if (!option.Set(channel.Configuration))
                {
                    logger.Warn("Unknown channel option '{}' for channel '{}'", option.Option, channel);
                }
            }
            catch (Exception ex)
            {
                logger.Warn("Failed to set channel option '{}' with value '{}' for channel '{}'", option.Option, option, channel, ex);
            }
        }

        protected abstract class ChannelOptionValue
        {
            public abstract ChannelOption Option { get; }
            public abstract bool Set(IChannelConfiguration config);
        }

        protected sealed class ChannelOptionValue<T> : ChannelOptionValue
        {
            public override ChannelOption Option { get; }
            private readonly T value;

            public ChannelOptionValue(ChannelOption<T> option, T value)
            {
                this.Option = option;
                this.value = value;
            }

            public override bool Set(IChannelConfiguration config) => config.SetOption(this.Option, this.value);

            public override string ToString() => this.value.ToString();
        }

        protected abstract class AttributeValue
        {
            public abstract void Set(IAttributeMap map);
        }

        protected sealed class AttributeValue<T> : AttributeValue where T : class
        {
            private readonly AttributeKey<T> key;
            private readonly T value;

            public AttributeValue(AttributeKey<T> key, T value)
            {
                this.key = key;
                this.value = value;
            }

            public override void Set(IAttributeMap config) => config.GetAttribute(this.key).Set(this.value);
        }
    }
}
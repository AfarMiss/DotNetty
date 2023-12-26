using System.Net;
using System.Threading.Tasks;
using DotNetty.Common.Internal.Logging;
using DotNetty.Common.Utilities;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels
{
    public abstract partial class AbstractChannel : ConstantMap, IChannel
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AbstractChannel>();
        private static readonly NotYetConnectedException NotYetConnectedException = new NotYetConnectedException();
        private readonly IChannelUnsafe channelUnsafe;
        private readonly DefaultChannelPipeline pipeline;
        private readonly TaskCompletionSource closeFuture = new TaskCompletionSource();

        private volatile EndPoint localAddress;
        private volatile EndPoint remoteAddress;
        private volatile IEventLoop eventLoop;
        private volatile bool registered;

        public ConstantMap ConstantMap => this;

        protected AbstractChannel(IChannel parent)
        {
            this.Parent = parent;
            this.Id = this.NewChannelId();
            this.channelUnsafe = this.NewUnsafe();
            this.pipeline = this.NewChannelPipeline();
        }

        protected AbstractChannel(IChannel parent, IChannelId id)
        {
            this.Parent = parent;
            this.Id = id;
            this.channelUnsafe = this.NewUnsafe();
            this.pipeline = this.NewChannelPipeline();
        }

        public IChannelId Id { get; }
        public IEventLoop EventLoop => this.eventLoop;
        public IChannel Parent { get; }
        public abstract bool Open { get; }
        public abstract bool Active { get; }
        public bool Registered => this.registered;

        public abstract ChannelMetadata Metadata { get; }
        public EndPoint LocalAddress => this.localAddress ?? this.CacheLocalAddress();
        public EndPoint RemoteAddress => this.remoteAddress ?? this.CacheRemoteAddress();
        public bool IsWritable => this.channelUnsafe.OutboundBuffer != null && this.channelUnsafe.OutboundBuffer.IsWritable;

        public IChannelUnsafe Unsafe => this.channelUnsafe;
        public IChannelPipeline Pipeline => this.pipeline;
        public abstract IChannelConfiguration Configuration { get; }
        public Task CloseCompletion => this.closeFuture.Task;
        
        protected abstract EndPoint LocalAddressInternal { get; }
        protected abstract EndPoint RemoteAddressInternal { get; }

        protected void InvalidateLocalAddress() => this.localAddress = null;

        protected EndPoint CacheLocalAddress() => this.localAddress = this.LocalAddressInternal;

        protected void InvalidateRemoteAddress() => this.remoteAddress = null;

        protected EndPoint CacheRemoteAddress() => this.remoteAddress = this.RemoteAddressInternal;

        protected virtual IChannelId NewChannelId() => DefaultChannelId.NewInstance();

        protected virtual DefaultChannelPipeline NewChannelPipeline() => new DefaultChannelPipeline(this);

        public virtual Task BindAsync(EndPoint localAddress) => this.pipeline.BindAsync(localAddress);
        public virtual Task ConnectAsync(EndPoint remoteAddress) => this.pipeline.ConnectAsync(remoteAddress);
        public virtual Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress) => this.pipeline.ConnectAsync(remoteAddress, localAddress);
        public virtual Task DisconnectAsync() => this.pipeline.DisconnectAsync();
        public virtual Task CloseAsync() => this.pipeline.CloseAsync();
        public Task DeregisterAsync() => this.pipeline.DeregisterAsync();
        public void Flush() => this.pipeline.Flush();
        public void Read() => this.pipeline.Read();
        public Task WriteAsync(object msg) => this.pipeline.WriteAsync(msg);
        public Task WriteAndFlushAsync(object message) => this.pipeline.WriteAndFlushAsync(message);
        
        protected abstract IChannelUnsafe NewUnsafe();

        public override int GetHashCode() => this.Id.GetHashCode();
        public override bool Equals(object o) => this == o;
        public int CompareTo(IChannel o) => ReferenceEquals(this, o) ? 0 : this.Id.CompareTo(o.Id);

        bool IConstantTransfer.TransferSet<T>(IConstant<T> constant, T value)
        {
            this.ConstantMap.Set(constant, value);
            return true;
        }

        protected abstract bool IsCompatible(IEventLoop eventLoop);
        protected virtual void DoRegister() { }
        protected abstract void DoBind(EndPoint localAddress);
        protected abstract void DoDisconnect();
        protected abstract void DoClose();
        protected virtual void DoDeregister() { }
        protected abstract void DoBeginRead();
        protected abstract void DoWrite(ChannelOutboundBuffer input);
        protected virtual object FilterOutboundMessage(object msg) => msg;
    }
}
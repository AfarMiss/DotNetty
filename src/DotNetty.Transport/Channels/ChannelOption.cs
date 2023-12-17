using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public abstract class ChannelOption : AbstractConstant<ChannelOption>
    {
        private static readonly ChannelOptionPool Pool = new ChannelOptionPool();

        private class ChannelOptionPool : ConstantPool
        {
            protected override IConstant NewConstant<T>(in int id, string name) => new ChannelOption<T>(id, name);
        }

        public static ChannelOption<T> ValueOf<T>(string name) => (ChannelOption<T>)Pool.ValueOf<T>(name);

        public static bool Exists(string name) => Pool.Exists(name);

        public static  ChannelOption<T> NewInstance<T>(string name) => (ChannelOption<T>)Pool.NewInstance<T>(name);

        public static readonly ChannelOption<IByteBufferAllocator> Allocator = ValueOf<IByteBufferAllocator>(nameof(Allocator));
        public static readonly ChannelOption<IRecvByteBufAllocator> RcvbufAllocator = ValueOf<IRecvByteBufAllocator>(nameof(RcvbufAllocator));
        public static readonly ChannelOption<IMessageSizeEstimator> MessageSizeEstimator = ValueOf<IMessageSizeEstimator>(nameof(MessageSizeEstimator));

        public static readonly ChannelOption<TimeSpan> ConnectTimeout = ValueOf<TimeSpan>(nameof(ConnectTimeout));
        public static readonly ChannelOption<int> WriteSpinCount = ValueOf<int>(nameof(WriteSpinCount));
        public static readonly ChannelOption<int> WriteBufferHighWaterMark = ValueOf<int>(nameof(WriteBufferHighWaterMark));
        public static readonly ChannelOption<int> WriteBufferLowWaterMark = ValueOf<int>(nameof(WriteBufferLowWaterMark));

        public static readonly ChannelOption<bool> AllowHalfClosure = ValueOf<bool>(nameof(AllowHalfClosure));
        public static readonly ChannelOption<bool> AutoRead = ValueOf<bool>(nameof(AutoRead));

        public static readonly ChannelOption<bool> SoBroadcast = ValueOf<bool>(nameof(SoBroadcast));
        public static readonly ChannelOption<bool> SoKeepalive = ValueOf<bool>(nameof(SoKeepalive));
        public static readonly ChannelOption<int> SoSndbuf = ValueOf<int>(nameof(SoSndbuf));
        public static readonly ChannelOption<int> SoRcvbuf = ValueOf<int>(nameof(SoRcvbuf));
        public static readonly ChannelOption<bool> SoReuseaddr = ValueOf<bool>(nameof(SoReuseaddr));
        public static readonly ChannelOption<bool> SoReuseport = ValueOf<bool>(nameof(SoReuseport));
        public static readonly ChannelOption<int> SoLinger = ValueOf<int>(nameof(SoLinger));
        public static readonly ChannelOption<int> SoBacklog = ValueOf<int>(nameof(SoBacklog));
        public static readonly ChannelOption<int> SoTimeout = ValueOf<int>(nameof(SoTimeout));

        public static readonly ChannelOption<int> IpTos = ValueOf<int>(nameof(IpTos));
        public static readonly ChannelOption<EndPoint> IpMulticastAddr = ValueOf<EndPoint>(nameof(IpMulticastAddr));
        public static readonly ChannelOption<NetworkInterface> IpMulticastIf = ValueOf<NetworkInterface>(nameof(IpMulticastIf));
        public static readonly ChannelOption<int> IpMulticastTtl = ValueOf<int>(nameof(IpMulticastTtl));
        public static readonly ChannelOption<bool> IpMulticastLoopDisabled = ValueOf<bool>(nameof(IpMulticastLoopDisabled));

        public static readonly ChannelOption<bool> TcpNodelay = ValueOf<bool>(nameof(TcpNodelay));

        internal ChannelOption(int id, string name) : base(id, name)
        {
        }

        public abstract bool Set<TV>(IChannelConfiguration configuration, TV value);
    }

    public sealed class ChannelOption<T> : ChannelOption
    {
        internal ChannelOption(int id, string name) : base(id, name)
        {
        }

        public void Validate(T value) => Contract.Requires(value != null);

        public override bool Set<TV>(IChannelConfiguration configuration, TV value)
        {
            return configuration.SetOption(this, Unsafe.As<TV, T>(ref value));
        }
    }
}
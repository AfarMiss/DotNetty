using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public static class ChannelOption
    {
        // private static readonly ChannelOptionPool Pool = new ChannelOptionPool();
        //
        // private class ChannelOptionPool : ConstantPool
        // {
        //     protected override IConstant NewConstant<T>(in int id, string name) => new ChannelOption<T>(id, name);
        // }
        //
        // public static ChannelOption<T> ValueOf<T>(string name) => (ChannelOption<T>)Pool.ValueOf<T>(name);
        //
        // public static bool Exists(string name) => Pool.Exists(name);
        //
        // public static  ChannelOption<T> NewInstance<T>(string name) => (ChannelOption<T>)Pool.NewInstance<T>(name);

        public static readonly ChannelOption<IByteBufferAllocator> Allocator = ChannelOption<IByteBufferAllocator>.ValueOf(nameof(Allocator));
        public static readonly ChannelOption<IRecvByteBufAllocator> RcvbufAllocator = ChannelOption<IRecvByteBufAllocator>.ValueOf(nameof(RcvbufAllocator));
        public static readonly ChannelOption<IMessageSizeEstimator> MessageSizeEstimator = ChannelOption<IMessageSizeEstimator>.ValueOf(nameof(MessageSizeEstimator));

        public static readonly ChannelOption<TimeSpan> ConnectTimeout = ChannelOption<TimeSpan>.ValueOf(nameof(ConnectTimeout));
        public static readonly ChannelOption<int> WriteSpinCount = ChannelOption<int>.ValueOf(nameof(WriteSpinCount));
        public static readonly ChannelOption<int> WriteBufferHighWaterMark = ChannelOption<int>.ValueOf(nameof(WriteBufferHighWaterMark));
        public static readonly ChannelOption<int> WriteBufferLowWaterMark = ChannelOption<int>.ValueOf(nameof(WriteBufferLowWaterMark));

        public static readonly ChannelOption<bool> AllowHalfClosure = ChannelOption<bool>.ValueOf(nameof(AllowHalfClosure));
        public static readonly ChannelOption<bool> AutoRead = ChannelOption<bool>.ValueOf(nameof(AutoRead));

        public static readonly ChannelOption<bool> SoBroadcast = ChannelOption<bool>.ValueOf(nameof(SoBroadcast));
        public static readonly ChannelOption<bool> SoKeepalive = ChannelOption<bool>.ValueOf(nameof(SoKeepalive));
        public static readonly ChannelOption<int> SoSndbuf = ChannelOption<int>.ValueOf(nameof(SoSndbuf));
        public static readonly ChannelOption<int> SoRcvbuf = ChannelOption<int>.ValueOf(nameof(SoRcvbuf));
        public static readonly ChannelOption<bool> SoReuseaddr = ChannelOption<bool>.ValueOf(nameof(SoReuseaddr));
        public static readonly ChannelOption<bool> SoReuseport = ChannelOption<bool>.ValueOf(nameof(SoReuseport));
        public static readonly ChannelOption<int> SoLinger = ChannelOption<int>.ValueOf(nameof(SoLinger));
        public static readonly ChannelOption<int> SoBacklog = ChannelOption<int>.ValueOf(nameof(SoBacklog));
        public static readonly ChannelOption<int> SoTimeout = ChannelOption<int>.ValueOf(nameof(SoTimeout));

        public static readonly ChannelOption<int> IpTos = ChannelOption<int>.ValueOf(nameof(IpTos));
        public static readonly ChannelOption<EndPoint> IpMulticastAddr = ChannelOption<EndPoint>.ValueOf(nameof(IpMulticastAddr));
        public static readonly ChannelOption<NetworkInterface> IpMulticastIf = ChannelOption<NetworkInterface>.ValueOf(nameof(IpMulticastIf));
        public static readonly ChannelOption<int> IpMulticastTtl = ChannelOption<int>.ValueOf(nameof(IpMulticastTtl));
        public static readonly ChannelOption<bool> IpMulticastLoopDisabled = ChannelOption<bool>.ValueOf(nameof(IpMulticastLoopDisabled));

        public static readonly ChannelOption<bool> TcpNodelay = ChannelOption<bool>.ValueOf(nameof(TcpNodelay));
        //
        // internal ChannelOption(int id, string name) : base(id, name)
        // {
        // }
        //
        // public abstract bool Set<TV>(IChannelConfiguration configuration, TV value);
    }

    public sealed class ChannelOption<T> : AbstractConstant<ChannelOption<T>, T>
    {
        public void Validate(T value) => Contract.Requires(value != null);

        // public override bool Set<TV>(IChannelConfiguration configuration, TV value)
        // {
        //     return configuration.SetOption(this, Unsafe.As<TV, T>(ref value));
        // }
    }
}
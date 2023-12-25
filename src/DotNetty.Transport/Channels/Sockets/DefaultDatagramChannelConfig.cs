using System;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace DotNetty.Transport.Channels.Sockets
{
    public class DefaultDatagramChannelConfig : DefaultChannelConfiguration, IDatagramChannelConfig
    {
        private const int DefaultFixedBufferSize = 2048;

        private readonly Socket socket;

        public DefaultDatagramChannelConfig(IDatagramChannel channel, Socket socket)
            : base(channel, new FixedRecvByteBufAllocator(DefaultFixedBufferSize))
        {
            Contract.Requires(socket != null);

            this.socket = socket;
        }

        public override T GetOption<T>(ChannelOption<T> option)
        {
            if (ChannelOption.SoBroadcast.Equals(option))
            {
                return OptionAs<T>.As(this.Broadcast);
            }
            if (ChannelOption.SoRcvbuf.Equals(option))
            {
                return OptionAs<T>.As(this.ReceiveBufferSize);
            }
            if (ChannelOption.SoSndbuf.Equals(option))
            {
                return OptionAs<T>.As(this.SendBufferSize);
            }
            if (ChannelOption.SoReuseaddr.Equals(option))
            {
                return OptionAs<T>.As(this.ReuseAddress);
            }
            if (ChannelOption.IpMulticastLoopDisabled.Equals(option))
            {
                return OptionAs<T>.As(this.LoopbackModeDisabled);
            }
            if (ChannelOption.IpMulticastTtl.Equals(option))
            {
                return OptionAs<T>.As(this.TimeToLive);
            }
            if (ChannelOption.IpMulticastAddr.Equals(option))
            {
                return OptionAs<T>.As(this.Interface);
            }
            if (ChannelOption.IpMulticastIf.Equals(option))
            {
                return OptionAs<T>.As(this.NetworkInterface);
            }
            if (ChannelOption.IpTos.Equals(option))
            {
                return OptionAs<T>.As(this.TrafficClass);
            }

            return base.GetOption(option);
        }

        public override bool SetOption<T>(ChannelOption<T> option, T value)
        {
            if (base.SetOption(option, value))
            {
                return true;
            }

            if (ChannelOption.SoBroadcast.Equals(option))
            {
                this.Broadcast = OptionAs<bool>.As(ref value);
            }
            else if (ChannelOption.SoRcvbuf.Equals(option))
            {
                this.ReceiveBufferSize = OptionAs<int>.As(ref value);
            }
            else if (ChannelOption.SoSndbuf.Equals(option))
            {
                this.SendBufferSize = OptionAs<int>.As(ref value);
            }
            else if (ChannelOption.SoReuseaddr.Equals(option))
            {
                this.ReuseAddress = OptionAs<bool>.As(ref value);
            }
            else if (ChannelOption.IpMulticastLoopDisabled.Equals(option))
            {
                this.LoopbackModeDisabled = OptionAs<bool>.As(ref value);
            }
            else if (ChannelOption.IpMulticastTtl.Equals(option))
            {
                this.TimeToLive = OptionAs<short>.As(ref value);
            }
            else if (ChannelOption.IpMulticastAddr.Equals(option))
            {
                this.Interface = OptionAs<EndPoint>.As(ref value);
            }
            else if (ChannelOption.IpMulticastIf.Equals(option))
            {
                this.NetworkInterface = OptionAs<NetworkInterface>.As(ref value);
            }
            else if (ChannelOption.IpTos.Equals(option))
            {
                this.TrafficClass = OptionAs<int>.As(ref value);
            }
            else
            {
                return false;
            }

            return true;
        }

        public int SendBufferSize
        {
            get
            {
                try
                {
                    return this.socket.SendBufferSize;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                try
                {
                    this.socket.SendBufferSize = value;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public int ReceiveBufferSize
        {
            get
            {
                try
                {
                    return this.socket.ReceiveBufferSize;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                try
                {
                    this.socket.ReceiveBufferSize = value;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public int TrafficClass
        {
            get
            {
                try
                {
                    return (int)this.socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.TypeOfService);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                try
                {
                    this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.TypeOfService, value);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public bool ReuseAddress
        {
            get
            {
                try
                {
                    return (int)this.socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress) != 0;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                try
                {
                    this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value ? 1 : 0);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public bool Broadcast
        {
            get
            {
                try
                {
                    return this.socket.EnableBroadcast;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                try
                {
                    this.socket.EnableBroadcast = value;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public bool LoopbackModeDisabled
        {
            get
            {
                try
                {
                    return !this.socket.MulticastLoopback;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                try
                {
                    this.socket.MulticastLoopback = !value;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public short TimeToLive
        {
            get
            {
                try
                {
                    return (short)this.socket.GetSocketOption(
                        this.AddressFamilyOptionLevel,
                        SocketOptionName.MulticastTimeToLive);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                try
                {
                    this.socket.SetSocketOption(
                        this.AddressFamilyOptionLevel,
                        SocketOptionName.MulticastTimeToLive,
                        value);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public EndPoint Interface
        {
            get
            {
                try
                {
                    return this.socket.LocalEndPoint;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                Contract.Requires(value != null);

                try
                {
                    this.socket.Bind(value);
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        public NetworkInterface NetworkInterface
        {
            get
            {
                try
                {
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    int value = (int)this.socket.GetSocketOption(
                        this.AddressFamilyOptionLevel,
                        SocketOptionName.MulticastInterface);
                    int index = IPAddress.NetworkToHostOrder(value);

                    if (interfaces.Length > 0
                        && index >= 0
                        && index < interfaces.Length)
                    {
                        return interfaces[index];
                    }

                    return null;
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
            set
            {
                Contract.Requires(value != null);

                try
                {
                    int index = GetNetworkInterfaceIndex(value);
                    if (index >= 0)
                    {
                        this.socket.SetSocketOption(this.AddressFamilyOptionLevel, SocketOptionName.MulticastInterface, index);
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    throw new ChannelException(ex);
                }
                catch (SocketException ex)
                {
                    throw new ChannelException(ex);
                }
            }
        }

        internal SocketOptionLevel AddressFamilyOptionLevel
        {
            get
            {
                if (this.socket.AddressFamily == AddressFamily.InterNetwork)
                {
                    return SocketOptionLevel.IP;
                }

                if (this.socket.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return SocketOptionLevel.IPv6;
                }

                throw new NotSupportedException($"Socket address family {this.socket.AddressFamily} not supported, expecting InterNetwork or InterNetworkV6");
            }
        }

        internal static int GetNetworkInterfaceIndex(NetworkInterface networkInterface)
        {
            Contract.Requires(networkInterface != null);

            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            return Array.FindIndex(interfaces, @interface => @interface.Id == networkInterface.Id);
        }
    }
}
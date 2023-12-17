using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace DotNetty.Transport.Channels.Sockets
{
    /// <summary>
    /// The default <see cref="ISocketChannelConfiguration"/> implementation.
    /// </summary>
    public class DefaultSocketChannelConfiguration : DefaultChannelConfiguration, ISocketChannelConfiguration
    {
        protected readonly Socket Socket;
        private volatile bool allowHalfClosure;

        public DefaultSocketChannelConfiguration(ISocketChannel channel, Socket socket)
            : base(channel)
        {
            Contract.Requires(socket != null);
            this.Socket = socket;

            // Enable TCP_NODELAY by default if possible.
            try
            {
                this.TcpNoDelay = true;
            }
            catch
            {
            }
        }

        public override T GetOption<T>(ChannelOption<T> option)
        {
            if (ChannelOption.SoRcvbuf.Equals(option))
            {
                return OptionAs<T>.As(this.ReceiveBufferSize);
            }
            if (ChannelOption.SoSndbuf.Equals(option))
            {
                return OptionAs<T>.As(this.SendBufferSize);
            }
            if (ChannelOption.TcpNodelay.Equals(option))
            {
                return OptionAs<T>.As(this.TcpNoDelay);
            }
            if (ChannelOption.SoKeepalive.Equals(option))
            {
                return OptionAs<T>.As(this.KeepAlive);
            }
            if (ChannelOption.SoReuseaddr.Equals(option))
            {
                return OptionAs<T>.As(this.ReuseAddress);
            }
            if (ChannelOption.SoLinger.Equals(option))
            {
                return OptionAs<T>.As(this.Linger);
            }
            if (ChannelOption.AllowHalfClosure.Equals(option))
            {
                return OptionAs<T>.As(this.AllowHalfClosure);
            }

            return base.GetOption(option);
        }

        public override bool SetOption<T>(ChannelOption<T> option, T value)
        {
            if (base.SetOption(option, value))
            {
                return true;
            }

            if (ChannelOption.SoRcvbuf.Equals(option))
            {
                this.ReceiveBufferSize = Unsafe.As<T, int>(ref value);
            }
            else if (ChannelOption.SoSndbuf.Equals(option))
            {
                this.SendBufferSize = Unsafe.As<T, int>(ref value);
            }
            else if (ChannelOption.TcpNodelay.Equals(option))
            {
                this.TcpNoDelay = Unsafe.As<T, bool>(ref value);
            }
            else if (ChannelOption.SoKeepalive.Equals(option))
            {
                this.KeepAlive = Unsafe.As<T, bool>(ref value);
            }
            else if (ChannelOption.SoReuseaddr.Equals(option))
            {
                this.ReuseAddress = Unsafe.As<T, bool>(ref value);
            }
            else if (ChannelOption.SoLinger.Equals(option))
            {
                this.Linger = Unsafe.As<T, int>(ref value);
            }
            else if (ChannelOption.AllowHalfClosure.Equals(option))
            {
                this.allowHalfClosure = Unsafe.As<T, bool>(ref value);
            }
            else
            {
                return false;
            }

            return true;
        }

        public bool AllowHalfClosure
        {
            get { return this.allowHalfClosure; }
            set { this.allowHalfClosure = value; }
        }

        public int ReceiveBufferSize
        {
            get
            {
                try
                {
                    return this.Socket.ReceiveBufferSize;
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
                    this.Socket.ReceiveBufferSize = value;
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

        public virtual int SendBufferSize
        {
            get
            {
                try
                {
                    return this.Socket.SendBufferSize;
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
                    this.Socket.SendBufferSize = value;
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

        public int Linger
        {
            get
            {
                try
                {
                    LingerOption lingerState = this.Socket.LingerState;
                    return lingerState.Enabled ? lingerState.LingerTime : -1;
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
                    if (value < 0)
                    {
                        this.Socket.LingerState = new LingerOption(false, 0);
                    }
                    else
                    {
                        this.Socket.LingerState = new LingerOption(true, value);
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

        public bool KeepAlive
        {
            get
            {
                try
                {
                    return (int)this.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive) != 0;
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
                    this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, value ? 1 : 0);
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
                    return (int)this.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress) != 0;
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
                    this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value ? 1 : 0);
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

        public bool TcpNoDelay
        {
            get
            {
                try
                {
                    return this.Socket.NoDelay;
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
                    this.Socket.NoDelay = value;
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
    }
}
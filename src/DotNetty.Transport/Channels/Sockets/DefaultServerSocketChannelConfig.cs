﻿using System;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace DotNetty.Transport.Channels.Sockets
{
    /// <summary>
    /// The default <see cref="IServerSocketChannelConfiguration"/> implementation.
    /// </summary>
    public class DefaultServerSocketChannelConfig : DefaultChannelConfiguration, IServerSocketChannelConfiguration
    {
        protected readonly Socket Socket;
        private volatile int backlog = 200; //todo: NetUtil.SOMAXCONN;

        /// <summary>
        ///     Creates a new instance.
        /// </summary>
        public DefaultServerSocketChannelConfig(IServerSocketChannel channel, Socket socket)
            : base(channel)
        {
            Contract.Requires(socket != null);

            this.Socket = socket;
        }

        public override T GetOption<T>(ChannelOption<T> option)
        {
            if (ChannelOption.SoRcvbuf.Equals(option))
            {
                return OptionAs<T>.As(this.ReceiveBufferSize);
            }
            if (ChannelOption.SoReuseaddr.Equals(option))
            {
                return OptionAs<T>.As(this.ReuseAddress);
            }
            if (ChannelOption.SoBacklog.Equals(option))
            {
                return OptionAs<T>.As(this.Backlog);
            }

            return base.GetOption(option);
        }

        public override bool SetOption<T>(ChannelOption<T> option, T value)
        {
            this.Validate(option, value);

            if (ChannelOption.SoRcvbuf.Equals(option))
            {
                this.ReceiveBufferSize = OptionAs<int>.As(ref value);
            }
            else if (ChannelOption.SoReuseaddr.Equals(option))
            {
                this.ReuseAddress = OptionAs<bool>.As(ref value);
            }
            else if (ChannelOption.SoBacklog.Equals(option))
            {
                this.Backlog = OptionAs<int>.As(ref value);
            }
            else
            {
                return base.SetOption(option, value);
            }

            return true;
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

        public int Backlog
        {
            get { return this.backlog; }
            set
            {
                Contract.Requires(value >= 0);

                this.backlog = value;
            }
        }
    }
}
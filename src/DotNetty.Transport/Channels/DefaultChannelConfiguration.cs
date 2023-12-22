using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public class DefaultChannelConfiguration : IChannelConfiguration
    {
        private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(30);

        private volatile IByteBufferAllocator allocator = ByteBufferUtil.DefaultAllocator;
        private volatile IRecvByteBufAllocator recvByteBufAllocator = FixedRecvByteBufAllocator.Default;
        private volatile IMessageSizeEstimator messageSizeEstimator = DefaultMessageSizeEstimator.Default;

        private volatile int autoRead = 1;
        private volatile int writeSpinCount = 16;
        private volatile int writeBufferHighWaterMark = 64 * 1024;
        private volatile int writeBufferLowWaterMark = 32 * 1024;
        private long connectTimeout = DefaultConnectTimeout.Ticks;

        protected readonly IChannel Channel;

        public DefaultChannelConfiguration(IChannel channel) : this(channel, new FixedRecvByteBufAllocator(4 * 1024))
        {
        }

        public DefaultChannelConfiguration(IChannel channel, IRecvByteBufAllocator allocator)
        {
            Contract.Requires(channel != null);

            this.Channel = channel;
            allocator.MaxMessagesPerRead = channel.Metadata.DefaultMaxMessagesPerRead;
            this.RecvByteBufAllocator = allocator;
        }

        protected static class OptionAs<TTo>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static TTo As<T>(T value) => Unsafe.As<T, TTo>(ref value);
            public static TTo As<T>(ref T value) => Unsafe.As<T, TTo>(ref value);
        }
        
        public virtual T GetOption<T>(ChannelOption<T> option)
        {
            Contract.Requires(option != null);

            if (ChannelOption.ConnectTimeout.Equals(option))
            {
                return OptionAs<T>.As(this.ConnectTimeout);
            }
            if (ChannelOption.WriteSpinCount.Equals(option))
            {
                return OptionAs<T>.As(this.WriteSpinCount);
            }
            if (ChannelOption.Allocator.Equals(option))
            {
                return (T)this.Allocator;
            }
            if (ChannelOption.RcvbufAllocator.Equals(option))
            {
                return (T)this.RecvByteBufAllocator;
            }
            if (ChannelOption.AutoRead.Equals(option))
            {
                return OptionAs<T>.As(this.AutoRead);
            }
            if (ChannelOption.WriteBufferHighWaterMark.Equals(option))
            {
                return OptionAs<T>.As(this.WriteBufferHighWaterMark);
            }
            if (ChannelOption.WriteBufferLowWaterMark.Equals(option))
            {
                return OptionAs<T>.As(this.WriteBufferLowWaterMark);
            }
            if (ChannelOption.MessageSizeEstimator.Equals(option))
            {
                return (T)this.MessageSizeEstimator;
            }
            return default(T);
        }

        public virtual bool SetOption<T>(ChannelOption<T> option, T value)
        {
            this.Validate(option, value);

            if (ChannelOption.ConnectTimeout.Equals(option))
            {
                this.ConnectTimeout = OptionAs<TimeSpan>.As(ref value);
            }
            else if (ChannelOption.WriteSpinCount.Equals(option))
            {
                this.WriteSpinCount = OptionAs<int>.As(ref value);
            }
            else if (ChannelOption.Allocator.Equals(option))
            {
                this.Allocator = (IByteBufferAllocator)value;
            }
            else if (ChannelOption.RcvbufAllocator.Equals(option))
            {
                this.RecvByteBufAllocator = (IRecvByteBufAllocator)value;
            }
            else if (ChannelOption.AutoRead.Equals(option))
            {
                this.AutoRead = OptionAs<bool>.As(ref value);
            }
            else if (ChannelOption.WriteBufferHighWaterMark.Equals(option))
            {
                this.WriteBufferHighWaterMark = OptionAs<int>.As(ref value);
            }
            else if (ChannelOption.WriteBufferLowWaterMark.Equals(option))
            {
                this.WriteBufferLowWaterMark = Unsafe.As<T, int>(ref value);
            }
            else if (ChannelOption.MessageSizeEstimator.Equals(option))
            {
                this.MessageSizeEstimator = (IMessageSizeEstimator)value;
            }
            else
            {
                return false;
            }

            return true;
        }

        protected virtual void Validate<T>(ChannelOption<T> option, T value)
        {
            Contract.Requires(option != null);
            option.Validate(value);
        }

        public TimeSpan ConnectTimeout
        {
            get => new TimeSpan(Volatile.Read(ref this.connectTimeout));
            set
            {
                Contract.Requires(value >= TimeSpan.Zero);
                Volatile.Write(ref this.connectTimeout, value.Ticks);
            }
        }

        public IByteBufferAllocator Allocator
        {
            get => this.allocator;
            set
            {
                Contract.Requires(value != null);
                this.allocator = value;
            }
        }

        public IRecvByteBufAllocator RecvByteBufAllocator
        {
            get => this.recvByteBufAllocator;
            set
            {
                Contract.Requires(value != null);
                this.recvByteBufAllocator = value;
            }
        }

        public IMessageSizeEstimator MessageSizeEstimator
        {
            get => this.messageSizeEstimator;
            set
            {
                Contract.Requires(value != null);
                this.messageSizeEstimator = value;
            }
        }

        public bool AutoRead
        {
            get => this.autoRead == 1;
            set
            {
                bool oldAutoRead = Interlocked.Exchange(ref this.autoRead, value ? 1 : 0) == 1;
                if (value && !oldAutoRead)
                {
                    this.Channel.Read();
                }
                else if (!value && oldAutoRead)
                {
                    this.AutoReadCleared();
                }
            }
        }

        protected virtual void AutoReadCleared()
        {
        }

        public int WriteBufferHighWaterMark
        {
            get => this.writeBufferHighWaterMark;
            set
            {
                Contract.Requires(value >= 0);
                Contract.Requires(value >= this.writeBufferLowWaterMark);

                this.writeBufferHighWaterMark = value;
            }
        }

        public int WriteBufferLowWaterMark
        {
            get => this.writeBufferLowWaterMark;
            set
            {
                Contract.Requires(value >= 0);
                Contract.Requires(value <= this.writeBufferHighWaterMark);

                this.writeBufferLowWaterMark = value;
            }
        }

        public int WriteSpinCount
        {
            get => this.writeSpinCount;
            set
            {
                Contract.Requires(value >= 1);

                this.writeSpinCount = value;
            }
        }

        public bool ConstantSet<T>(IConstant constant, T value)
        {
            throw new NotImplementedException();
        }
    }
}
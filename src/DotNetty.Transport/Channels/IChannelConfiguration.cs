using System;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public interface IChannelConfiguration : IConstantAccessor
    {
        T GetOption<T>(ChannelOption<T> option);
        bool SetOption<T>(ChannelOption<T> option, T value);

        TimeSpan ConnectTimeout { get; set; }
        int WriteSpinCount { get; set; }
        IByteBufferAllocator Allocator { get; set; }
        IRecvByteBufAllocator RecvByteBufAllocator { get; set; }
        /// 是否自动读取数据
        bool AutoRead { get; set; }
        int WriteBufferHighWaterMark { get; set; }
        int WriteBufferLowWaterMark { get; set; }
        IMessageSizeEstimator MessageSizeEstimator { get; set; }
    }
}
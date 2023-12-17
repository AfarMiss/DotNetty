using DotNetty.Buffers;

namespace DotNetty.Transport.Channels
{
    public interface IRecvByteBufAllocatorHandle
    {
        /// <summary>
        /// 通过<see cref="IEventLoopGroup"/>分配缓冲区
        /// </summary>
        IByteBuffer Allocate(IByteBufferAllocator alloc);
        /// <summary>
        /// 预先设置的接受缓冲区大小
        /// </summary>
        int Guess();
        /// <summary>
        /// 重置统计参数
        /// </summary>
        void Reset(IChannelConfiguration config);
        /// <summary>
        /// 统计读取的消息数量
        /// </summary>
        void IncMessagesRead(int numMessages);
        /// <summary>
        /// 上一次读取的字节数
        /// </summary>
        int LastBytesRead { get; set; }
        /// <summary>
        /// 尝试读取的字节数 默认是缓冲区可写的尺寸
        /// </summary>
        int AttemptedBytesRead { get; set; }
        /// <summary>
        /// 是否还能继续读
        /// </summary>
        bool ContinueReading();
        /// <summary>
        /// 读取完成
        /// </summary>
        void ReadComplete();
    }
}
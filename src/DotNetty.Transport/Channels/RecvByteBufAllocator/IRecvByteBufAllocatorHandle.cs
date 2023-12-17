using DotNetty.Buffers;

namespace DotNetty.Transport.Channels
{
    public interface IRecvByteBufAllocatorHandle
    {
        /// <summary>
        /// 根据预测大小分配ByteBuffer
        /// </summary>
        IByteBuffer Allocate(IByteBufferAllocator alloc);
        /// <summary>
        /// 设置预测大小
        /// </summary>
        int Guess();
        /// <summary>
        /// 重置
        /// </summary>
        void Reset(IChannelConfiguration config);
        /// <summary>
        /// 增加统计读取的消息数量
        /// </summary>
        void IncMessagesRead(int numMessages);
        /// <summary>
        /// 最后一次读取的字节数
        /// </summary>
        int LastBytesRead { get; set; }
        /// <summary>
        /// 预测大小之后分配的ByteBuffer写入大小
        /// </summary>
        int AttemptedBytesRead { get; set; }
        /// <summary>
        /// 读循环 是否还能继续
        /// </summary>
        bool ContinueReading();
        /// <summary>
        /// 读循环完成
        /// </summary>
        void ReadComplete();
    }
}
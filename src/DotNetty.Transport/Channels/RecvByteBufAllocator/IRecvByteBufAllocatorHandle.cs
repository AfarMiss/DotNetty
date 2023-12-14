using DotNetty.Buffers;

namespace DotNetty.Transport.Channels
{
    public interface IRecvByteBufAllocatorHandle
    {
        IByteBuffer Allocate(IByteBufferAllocator alloc);
        int Guess();
        void Reset(IChannelConfiguration config);
        void IncMessagesRead(int numMessages);
        int LastBytesRead { get; set; }
        int AttemptedBytesRead { get; set; }
        bool ContinueReading();
        void ReadComplete();
    }
}
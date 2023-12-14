namespace DotNetty.Transport.Channels
{
    public interface IMaxMessagesRecvByteBufAllocator : IRecvByteBufAllocator
    {
        int MaxMessagesPerRead { get; set; }
    }
}
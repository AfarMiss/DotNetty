namespace DotNetty.Transport.Channels
{
    public interface IRecvByteBufAllocator
    {
        /// <summary>
        /// 读循环 最大能读取的消息数量
        /// </summary>
        int MaxMessagesPerRead { get; set; }
        
        IRecvByteBufAllocatorHandle NewHandle();
    }
}
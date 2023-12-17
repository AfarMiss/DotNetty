namespace DotNetty.Transport.Channels
{
    public interface IRecvByteBufAllocator
    {
        /// <summary>
        /// 单次读最多读取多少数据
        /// </summary>
        int MaxMessagesPerRead { get; set; }
        
        IRecvByteBufAllocatorHandle NewHandle();
    }
}
namespace DotNetty.Transport.Channels
{
    public interface IRecvByteBufAllocator
    {
        IRecvByteBufAllocatorHandle NewHandle();
    }
}
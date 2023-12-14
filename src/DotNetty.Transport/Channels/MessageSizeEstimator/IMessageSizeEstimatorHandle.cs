namespace DotNetty.Transport.Channels
{
    public interface IMessageSizeEstimatorHandle
    {
        int Size(object msg);
    }
}
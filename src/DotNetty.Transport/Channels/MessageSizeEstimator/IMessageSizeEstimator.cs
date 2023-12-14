namespace DotNetty.Transport.Channels
{
    public interface IMessageSizeEstimator
    {
        IMessageSizeEstimatorHandle NewHandle();
    }
}
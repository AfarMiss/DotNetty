namespace DotNetty.Transport.Channels
{
    public interface IMessageSizeEstimator
    {
        /// <summary>
        ///     Creates a new handle. The handle provides the actual operations.
        /// </summary>
        IMessageSizeEstimatorHandle NewHandle();
    }
}
namespace DotNetty.Transport.Channels.Sockets
{
    public sealed class ChannelInputShutdownEvent
    {
        /// <summary>
        /// Singleton instance to use.
        /// </summary>
        public static readonly ChannelInputShutdownEvent Instance = new ChannelInputShutdownEvent();

        ChannelInputShutdownEvent()
        {
        }
    }
}
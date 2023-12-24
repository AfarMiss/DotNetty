using System.Diagnostics.Contracts;

namespace DotNetty.Transport.Channels
{
    ///<see cref="IChannel"/> 基本元数据
    public sealed class ChannelMetadata
    {
        /// <summary> 通道具备<see cref="IChannel.DisconnectAsync()"/>且允许用户断开链接并重新<see cref="IChannel.ConnectAsync(System.Net.EndPoint)"/> </summary>
        public bool HasDisconnect { get; }
        /// <summary> <see cref="IRecvByteBufAllocator.MaxMessagesPerRead"/> </summary>
        public int DefaultMaxMessagesPerRead { get; }
        
        /// <param name="hasDisconnect"> <see cref="HasDisconnect"/> </param>
        /// <param name="defaultMaxMessagesPerRead"> <see cref="HasDisconnect"/> </param>
        public ChannelMetadata(bool hasDisconnect, int defaultMaxMessagesPerRead = 1)
        {
            Contract.Requires(defaultMaxMessagesPerRead > 0);
            this.HasDisconnect = hasDisconnect;
            this.DefaultMaxMessagesPerRead = defaultMaxMessagesPerRead;
        }
    }
}
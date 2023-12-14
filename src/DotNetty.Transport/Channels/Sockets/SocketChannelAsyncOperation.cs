using System.Diagnostics.Contracts;
using System.Net.Sockets;
using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels.Sockets
{
    public class SocketChannelAsyncOperation : SocketAsyncEventArgs
    {
        public SocketChannelAsyncOperation(AbstractSocketChannel channel, bool setEmptyBuffer = true)
        {
            Contract.Requires(channel != null);

            this.Channel = channel;
            this.Completed += AbstractSocketChannel.IoCompletedCallback;
            if (setEmptyBuffer)
            {
                this.SetBuffer(System.Array.Empty<byte>(), 0, 0);
            }
        }

        public void Validate()
        {
            SocketError socketError = this.SocketError;
            if (socketError != SocketError.Success)
            {
                throw new SocketException((int)socketError);
            }
        }

        public AbstractSocketChannel Channel { get; private set; }
    }
}
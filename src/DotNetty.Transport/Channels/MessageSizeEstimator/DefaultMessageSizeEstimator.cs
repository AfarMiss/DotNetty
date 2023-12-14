using System.Diagnostics.Contracts;
using DotNetty.Buffers;

namespace DotNetty.Transport.Channels
{
    public sealed class DefaultMessageSizeEstimator : IMessageSizeEstimator
    {
        public static readonly IMessageSizeEstimator Default = new DefaultMessageSizeEstimator(0);
        private readonly IMessageSizeEstimatorHandle handle;

        private sealed class HandleImpl : IMessageSizeEstimatorHandle
        {
            private readonly int unknownSize;

            public HandleImpl(int unknownSize)
            {
                this.unknownSize = unknownSize;
            }

            public int Size(object msg)
            {
                switch (msg)
                {
                    case IByteBuffer buffer:
                        return buffer.ReadableBytes;
                    case IByteBufferHolder holder:
                        return holder.Content.ReadableBytes;
                    default:
                        return this.unknownSize;
                }
            }
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="unknownSize">The size which is returned for unknown messages.</param>
        public DefaultMessageSizeEstimator(int unknownSize)
        {
            Contract.Requires(unknownSize >= 0);
            this.handle = new HandleImpl(unknownSize);
        }

        public IMessageSizeEstimatorHandle NewHandle() => this.handle;
    }
}
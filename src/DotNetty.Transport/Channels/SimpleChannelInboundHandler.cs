using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public abstract class SimpleChannelInboundHandler<T> : ChannelHandlerAdapter
    {
        private readonly bool autoRelease;

        protected SimpleChannelInboundHandler(bool autoRelease = true)
        {
            this.autoRelease = autoRelease;
        }

        public bool AcceptInboundMessage(object msg) => msg is T;

        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            var isInboundMsg = this.AcceptInboundMessage(msg);
            try
            {
                if (isInboundMsg)
                {
                    this.ChannelRead0(ctx, (T)msg);
                }
                else
                {
                    ctx.FireChannelRead(msg);
                }
            }
            finally
            {
                if (this.autoRelease && isInboundMsg)
                {
                    ReferenceCountUtil.Release(msg);
                }
            }
        }

        protected abstract void ChannelRead0(IChannelHandlerContext ctx, T msg);
    }
}

using DotNetty.Common.Utilities;

namespace DotNetty.Transport.Channels
{
    public abstract class SimpleChannelInboundHandler<I> : ChannelHandlerAdapter
    {
        private readonly bool autoRelease;

        protected SimpleChannelInboundHandler() : this(true)
        {
        }

        protected SimpleChannelInboundHandler(bool autoRelease)
        {
            this.autoRelease = autoRelease;
        }

        public bool AcceptInboundMessage(object msg) => msg is I;

        public override void ChannelRead(IChannelHandlerContext ctx, object msg)
        {
            bool release = true;
            try
            {
                if (this.AcceptInboundMessage(msg))
                {
                    this.ChannelRead0(ctx, (I)msg);
                }
                else
                {
                    release = false;
                    ctx.FireChannelRead(msg);
                }
            }
            finally
            {
                if (autoRelease && release)
                {
                    ReferenceCountUtil.Release(msg);
                }
            }
        }

        protected abstract void ChannelRead0(IChannelHandlerContext ctx, I msg);
    }
}

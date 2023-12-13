using System.Diagnostics.Contracts;
using DotNetty.Common.Concurrency;

namespace DotNetty.Transport.Channels
{
    internal sealed class DefaultChannelHandlerContext : AbstractChannelHandlerContext
    {
        public override IChannelHandler Handler { get; }

        public DefaultChannelHandlerContext(DefaultChannelPipeline pipeline, IEventExecutor executor, string name, IChannelHandler handler)
            : base(pipeline, executor, name, GetSkipPropagationFlags(handler))
        {
            Contract.Requires(handler != null);
            this.Handler = handler;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;

namespace DotNetty.Transport.Channels
{
    public class AffinitizedEventLoopGroup : AbstractEventExecutorGroup, IEventLoopGroup
    {
        private readonly IEventLoopGroup innerGroup;

        public override bool IsShutdown => this.innerGroup.IsShutdown;

        public override bool IsTerminated => this.innerGroup.IsTerminated;

        public override bool IsShuttingDown => this.innerGroup.IsShuttingDown;

        public override Task TerminationCompletion => this.innerGroup.TerminationCompletion;

        protected override IEnumerable<IEventExecutor> GetItems() => this.innerGroup.Items;

        public new IEnumerable<IEventLoop> Items => this.innerGroup.Items;

        public AffinitizedEventLoopGroup(IEventLoopGroup innerGroup)
        {
            this.innerGroup = innerGroup;
        }

        public override IEventExecutor GetNext()
        {
            if (ExecutionEnvironment.TryGetCurrentExecutor(out var executor))
            {
                if (executor is IEventLoop loop && loop.Parent == this.innerGroup)
                {
                    return loop;
                }
            }
            return this.innerGroup.GetNext();
        }

        IEventLoop IEventLoopGroup.GetNext() => (IEventLoop)this.GetNext();

        public Task RegisterAsync(IChannel channel) => ((IEventLoop)this.GetNext()).RegisterAsync(channel);

        public override Task ShutdownGracefullyAsync(TimeSpan quietPeriod, TimeSpan timeout) => this.innerGroup.ShutdownGracefullyAsync(quietPeriod, timeout);
    }
}
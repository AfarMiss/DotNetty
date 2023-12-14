using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;

namespace DotNetty.Transport.Channels
{
    public sealed class MultiThreadEventLoopGroup : AbstractEventExecutorGroup, IEventLoopGroup
    {
        private static readonly int DefaultEventLoopThreadCount = Environment.ProcessorCount * 2;
        private static readonly Func<IEventLoopGroup, IEventLoop> DefaultEventLoopFactory = group => new SingleThreadEventLoop(group);

        private readonly IEventLoop[] eventLoops;
        private int requestId;

        public override bool IsShutdown => eventLoops.All(eventLoop => eventLoop.IsShutdown);

        public override bool IsTerminated => eventLoops.All(eventLoop => eventLoop.IsTerminated);

        public override bool IsShuttingDown => eventLoops.All(eventLoop => eventLoop.IsShuttingDown);

        public override Task TerminationCompletion { get; }

        protected override IEnumerable<IEventExecutor> GetItems() => this.eventLoops;

        public new IEnumerable<IEventLoop> Items => this.eventLoops;

        public MultiThreadEventLoopGroup()
            : this(DefaultEventLoopFactory, DefaultEventLoopThreadCount)
        {
        }

        public MultiThreadEventLoopGroup(int eventLoopCount)
            : this(DefaultEventLoopFactory, eventLoopCount)
        {
        }

        public MultiThreadEventLoopGroup(Func<IEventLoopGroup, IEventLoop> eventLoopFactory)
            : this(eventLoopFactory, DefaultEventLoopThreadCount)
        {
        }

        public MultiThreadEventLoopGroup(Func<IEventLoopGroup, IEventLoop> eventLoopFactory, int eventLoopCount)
        {
            this.eventLoops = new IEventLoop[eventLoopCount];
            var terminationTasks = new Task[eventLoopCount];
            for (int i = 0; i < eventLoopCount; i++)
            {
                IEventLoop eventLoop;
                bool success = false;
                try
                {
                    eventLoop = eventLoopFactory(this);
                    success = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("failed to create a child event loop.", ex);
                }
                finally
                {
                    if (!success)
                    {
                        Task.WhenAll(this.eventLoops.Take(i).Select(loop => loop.ShutdownGracefullyAsync())).Wait();
                    }
                }

                this.eventLoops[i] = eventLoop;
                terminationTasks[i] = eventLoop.TerminationCompletion;
            }
            this.TerminationCompletion = Task.WhenAll(terminationTasks);
        }

        IEventLoop IEventLoopGroup.GetNext() => (IEventLoop)this.GetNext();

        public override IEventExecutor GetNext()
        {
            int id = Interlocked.Increment(ref this.requestId);
            return this.eventLoops[Math.Abs(id % this.eventLoops.Length)];
        }

        public Task RegisterAsync(IChannel channel) => ((IEventLoop)this.GetNext()).RegisterAsync(channel);

        public override Task ShutdownGracefullyAsync(TimeSpan quietPeriod, TimeSpan timeout)
        {
            foreach (var eventLoop in this.eventLoops)
            {
                eventLoop.ShutdownGracefullyAsync(quietPeriod, timeout);
            }
            return this.TerminationCompletion;
        }
    }
}
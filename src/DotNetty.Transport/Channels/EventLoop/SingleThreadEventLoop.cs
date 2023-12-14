using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal;

namespace DotNetty.Transport.Channels
{
    public class SingleThreadEventLoop : SingleThreadEventExecutor, IEventLoop
    {
        private static readonly TimeSpan DefaultBreakoutInterval = TimeSpan.FromMilliseconds(100);

        public new IEventLoopGroup Parent => (IEventLoopGroup)base.Parent;
        public new IEnumerable<IEventLoop> Items => new[] { this };
        
        public SingleThreadEventLoop() : this(null, DefaultBreakoutInterval)
        {
        }

        public SingleThreadEventLoop(string threadName) : this(threadName, DefaultBreakoutInterval)
        {
        }

        public SingleThreadEventLoop(string threadName, TimeSpan breakoutInterval)
            : base(threadName, breakoutInterval)
        {
        }

        public SingleThreadEventLoop(IEventLoopGroup parent)
            : this(parent, null, DefaultBreakoutInterval)
        {
        }

        public SingleThreadEventLoop(IEventLoopGroup parent, string threadName)
            : this(parent, threadName, DefaultBreakoutInterval)
        {
        }

        public SingleThreadEventLoop(IEventLoopGroup parent, string threadName, TimeSpan breakoutInterval)
            : base(parent, threadName, breakoutInterval)
        {
        }

        protected SingleThreadEventLoop(string threadName, TimeSpan breakoutInterval, ConcurrentQueue<IRunnable> taskQueue)
            : base(null, threadName, breakoutInterval, taskQueue)
        {
        }

        protected SingleThreadEventLoop(IEventLoopGroup parent, string threadName, TimeSpan breakoutInterval, ConcurrentQueue<IRunnable> taskQueue)
            : base(parent, threadName, breakoutInterval, taskQueue)
        {
        }

        public new IEventLoop GetNext() => this;

        public Task RegisterAsync(IChannel channel) => channel.Unsafe.RegisterAsync(this);
    }
}
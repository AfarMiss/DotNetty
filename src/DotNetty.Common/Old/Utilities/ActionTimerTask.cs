using System;

namespace DotNetty.Common.Utilities
{
    public class ActionTimerTask : ITimerTask
    {
        private readonly Action<ITimeout> action;

        public ActionTimerTask(Action<ITimeout> action) => this.action = action;

        public void Run(ITimeout timeout) => this.action(timeout);
    }
}
using System;

namespace DotNetty.Common.Concurrency
{
    public interface IScheduledRunnable : IRunnable, IScheduledTask, IComparable<IScheduledRunnable>
    {
    }
}
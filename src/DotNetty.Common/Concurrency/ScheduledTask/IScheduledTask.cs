using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DotNetty.Common.Concurrency
{
    public interface IScheduledTask
    {
        PreciseTimeSpan Deadline { get; }
        Task Completion { get; }
        
        bool Cancel();
        TaskAwaiter GetAwaiter();
    }
}
using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace DotNetty.Common.Utilities
{
    public static class ThreadExtensions
    {
        public static bool Join(this Thread thread, TimeSpan timeout)
        {
            long tm = (long)timeout.TotalMilliseconds;
            Contract.Requires(tm >= 0 && tm <= int.MaxValue);

            return thread.Join((int)tm);
        }
    }
}
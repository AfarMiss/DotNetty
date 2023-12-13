using System;

namespace DotNetty.Common.Concurrency
{
    public class RejectedExecutionException : Exception
    {
        public RejectedExecutionException()
        {
        }

        public RejectedExecutionException(string message)
            : base(message)
        {
        }
    }
}
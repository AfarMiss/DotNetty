using System;

namespace DotNetty.Common.Concurrency
{
    public static class ExecutionEnvironment
    {
        [ThreadStatic] 
        private static IEventExecutor currentExecutor;

        public static bool TryGetCurrentExecutor(out IEventExecutor executor)
        {
            executor = currentExecutor;
            return executor != null;
        }

        internal static void SetCurrentExecutor(IEventExecutor executor) => currentExecutor = executor;
    }
}
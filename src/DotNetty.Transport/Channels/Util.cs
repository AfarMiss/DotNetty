using System;
using System.Threading.Tasks;
using DotNetty.Common.Concurrency;
using DotNetty.Common.Internal.Logging;
using TaskCompletionSource = DotNetty.Common.Concurrency.TaskCompletionSource;

namespace DotNetty.Transport.Channels
{
    static class Util
    {
        static readonly IInternalLogger Log = InternalLoggerFactory.GetInstance<IChannel>();

        public static void SafeSetSuccess(TaskCompletionSource promise, IInternalLogger logger)
        {
            if (promise != TaskCompletionSource.Void && !promise.TryComplete())
            {
                logger.Warn($"Failed Promise Success: {promise}");
            }
        }

        public static void SafeSetFailure(TaskCompletionSource promise, Exception cause, IInternalLogger logger)
        {
            if (promise != TaskCompletionSource.Void && !promise.TrySetException(cause))
            {
                logger.Warn($"Failed Promise Failure: {promise}", cause);
            }
        }

        public static void CloseSafe(this IChannel channel)
        {
            CompleteChannelCloseTaskSafely(channel, channel.CloseAsync());
        }

        public static void CloseSafe(this IChannelUnsafe u)
        {
            CompleteChannelCloseTaskSafely(u, u.CloseAsync());
        }

        internal static async void CompleteChannelCloseTaskSafely(object channelObject, Task closeTask)
        {
            try
            {
                await closeTask;
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex) 
            {
                if (Log.DebugEnabled)
                {
                    Log.Debug("Failed to close channel " + channelObject + " cleanly.", ex);
                }
            }
        }
    }
}
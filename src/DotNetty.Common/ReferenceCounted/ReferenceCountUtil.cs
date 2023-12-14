using System;
using DotNetty.Common.Internal.Logging;

namespace DotNetty.Common.Utilities
{
    public static class ReferenceCountUtil
    {
        private static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance(typeof(ReferenceCountUtil));
        
        /// <summary>
        /// Tries to call <see cref="IReferenceCounted.Retain(int)"/> if the specified message implements
        /// <see cref="IReferenceCounted"/>. If the specified message doesn't implement
        /// <see cref="IReferenceCounted"/>, this method does nothing.
        /// </summary>
        public static T Retain<T>(T msg, int increment = 1)
        {
            if (msg is IReferenceCounted counted)
            {
                return (T)counted.Retain(increment);
            }
            return msg;
        }
        
        /// <summary>
        /// Tries to call <see cref="IReferenceCounted.Release(int)" /> if the specified message implements
        /// <see cref="IReferenceCounted"/>. If the specified message doesn't implement
        /// <see cref="IReferenceCounted"/>, this method does nothing.
        /// </summary>
        public static bool Release(object msg, int decrement = 1)
        {
            if (msg is IReferenceCounted counted)
            {
                return counted.Release(decrement);
            }
            return false;
        }
        
        /// <summary>
        /// Tries to call <see cref="IReferenceCounted.Release(int)" /> if the specified message implements
        /// <see cref="IReferenceCounted"/>. If the specified message doesn't implement
        /// <see cref="IReferenceCounted"/>, this method does nothing. Unlike <see cref="Release(object)"/>, this
        /// method catches an exception raised by <see cref="IReferenceCounted.Release(int)" /> and logs it, rather
        /// than rethrowing it to the caller. It is usually recommended to use <see cref="Release(object, int)"/>
        /// instead, unless you absolutely need to swallow an exception.
        /// </summary>
        public static void SafeRelease(object msg, int decrement = 1)
        {
            try
            {
                Release(msg, decrement);
            }
            catch (Exception ex)
            {
                if (Logger.WarnEnabled)
                {
                    Logger.Warn("Failed to release a message: {} (decrement: {})", msg, decrement, ex);
                }
            }
        }
        
        public static void SafeRelease(IReferenceCounted msg, int decrement = 1)
        {
            try
            {
                msg?.Release(decrement);
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to release a message: {} (decrement: {})", msg, decrement, ex);
            }
        }
    }
}
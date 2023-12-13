using System;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DotNetty.Transport.Channels
{
    public static class SkipFlagHelper
    {
        private static readonly ConditionalWeakTable<Type, Tuple<SkipFlags>> SkipTable = new ConditionalWeakTable<Type, Tuple<SkipFlags>>();
        
        internal static SkipFlags GetSkipFlag(IChannelHandler handler)
        {
            var skipDirection = SkipTable.GetValue(handler.GetType(), handlerType => Tuple.Create(GetSkipFlag(handlerType)));
            return skipDirection?.Item1 ?? 0;
        }

        internal static SkipFlags GetSkipFlag(Type handlerType)
        {
            SkipFlags flags = 0;

            // this method should never throw
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.HandlerAdded)))
            {
                flags |= SkipFlags.HandlerAdded;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.HandlerRemoved)))
            {
                flags |= SkipFlags.HandlerRemoved;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ExceptionCaught), typeof(Exception)))
            {
                flags |= SkipFlags.ExceptionCaught;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ChannelRegistered)))
            {
                flags |= SkipFlags.ChannelRegistered;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ChannelUnregistered)))
            {
                flags |= SkipFlags.ChannelUnregistered;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ChannelActive)))
            {
                flags |= SkipFlags.ChannelActive;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ChannelInactive)))
            {
                flags |= SkipFlags.ChannelInactive;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ChannelRead), typeof(object)))
            {
                flags |= SkipFlags.ChannelRead;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ChannelReadComplete)))
            {
                flags |= SkipFlags.ChannelReadComplete;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ChannelWritabilityChanged)))
            {
                flags |= SkipFlags.ChannelWritabilityChanged;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.UserEventTriggered), typeof(object)))
            {
                flags |= SkipFlags.UserEventTriggered;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.BindAsync), typeof(EndPoint)))
            {
                flags |= SkipFlags.Bind;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.ConnectAsync), typeof(EndPoint), typeof(EndPoint)))
            {
                flags |= SkipFlags.Connect;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.DisconnectAsync)))
            {
                flags |= SkipFlags.Disconnect;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.CloseAsync)))
            {
                flags |= SkipFlags.Close;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.DeregisterAsync)))
            {
                flags |= SkipFlags.Deregister;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.Read)))
            {
                flags |= SkipFlags.Read;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.WriteAsync), typeof(object)))
            {
                flags |= SkipFlags.Write;
            }
            if (IsSkipFlagMethod(handlerType, nameof(IChannelHandler.Flush)))
            {
                flags |= SkipFlags.Flush;
            }
            return flags;
        }

        internal static bool IsSkipFlagMethod(Type handlerType, string methodName) => IsSkipFlagMethod(handlerType, methodName, Type.EmptyTypes);

        internal static bool IsSkipFlagMethod(Type handlerType, string methodName, params Type[] paramTypes)
        {
            var newParamTypes = new Type[paramTypes.Length + 1];
            newParamTypes[0] = typeof(IChannelHandlerContext);
            Array.Copy(paramTypes, 0, newParamTypes, 1, paramTypes.Length);
            return handlerType.GetMethod(methodName, newParamTypes)!.GetCustomAttribute<SkipAttribute>(false) != null;
        }
    }
}
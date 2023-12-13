using System;

namespace DotNetty.Transport.Channels
{
    [Flags]
    internal enum SkipFlags
    {
        HandlerAdded = 1,
        HandlerRemoved = 1 << 1,
        ExceptionCaught = 1 << 2,
        ChannelRegistered = 1 << 3,
        ChannelUnregistered = 1 << 4,
        ChannelActive = 1 << 5,
        ChannelInactive = 1 << 6,
        ChannelRead = 1 << 7,
        ChannelReadComplete = 1 << 8,
        ChannelWritabilityChanged = 1 << 9,
        UserEventTriggered = 1 << 10,
        Bind = 1 << 11,
        Connect = 1 << 12,
        Disconnect = 1 << 13,
        Close = 1 << 14,
        Deregister = 1 << 15,
        Read = 1 << 16,
        Write = 1 << 17,
        Flush = 1 << 18,

        Inbound = ExceptionCaught |
                  ChannelRegistered |
                  ChannelUnregistered |
                  ChannelActive |
                  ChannelInactive |
                  ChannelRead |
                  ChannelReadComplete |
                  ChannelWritabilityChanged |
                  UserEventTriggered,

        Outbound = Bind |
                   Connect |
                   Disconnect |
                   Close |
                   Deregister |
                   Read |
                   Write |
                   Flush,
    }
}